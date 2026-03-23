using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SorobanSecurityPortalApi.Common.Data;
using SorobanSecurityPortalApi.Models.DbModels;
using SorobanSecurityPortalApi.Services.ForumService.Dto;

namespace SorobanSecurityPortalApi.Services.ForumService
{
    public partial class ForumService : IForumService
    {
        private readonly Db _db;
        private readonly ILogger<ForumService> _logger;

        public ForumService(Db db, ILogger<ForumService> logger)
        {
            using var _ = _db = db;
            _logger = logger;
        }

        public async Task<List<ForumCategory>> GetCategoriesAsync()
        {
            var categories = await _db.ForumCategory
                .OrderBy(c => c.SortOrder)
                .Select(c => new ForumCategory
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    ThreadCount = _db.ForumThread.Count(t => t.CategoryId == c.Id)
                })
                .ToListAsync();

            return categories;
        }

        public async Task<PaginatedResult<ForumThreadList>> GetThreadsByCategoryAsync(string categorySlug, int page, int pageSize)
        {
            var query = _db.ForumThread
                .Include(t => t.Author)
                .Where(t => t.Category.Slug == categorySlug);

            var totalItems = await query.CountAsync();

            var threads = await query
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new ForumThreadList
                {
                    Id = t.Id,
                    Title = t.Title,
                    Slug = t.Slug,
                    Author = new Author { Id = t.AuthorId, Name = t.Author.FullName ?? t.Author.Login },
                    ViewCount = t.ViewCount,
                    ReplyCount = _db.ForumPost.Count(p => p.ThreadId == t.Id) - 1, // Subtract first post
                    IsPinned = t.IsPinned,
                    IsLocked = t.IsLocked,
                    CreatedAt = t.CreatedAt,
                    LastPostAt = _db.ForumPost.Where(p => p.ThreadId == t.Id).Max(p => p.CreatedAt)
                })
                .ToListAsync();

            return new PaginatedResult<ForumThreadList>
            {
                Items = threads,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ForumThreadDetail?> GetThreadBySlugAsync(string slug, int page, int pageSize)
        {
            var thread = await _db.ForumThread
                .Include(t => t.Author)
                .FirstOrDefaultAsync(t => t.Slug == slug);

            if (thread == null) return null;

            var postsQuery = _db.ForumPost
                .Include(p => p.Author)
                .Where(p => p.ThreadId == thread.Id);

            var totalPosts = await postsQuery.CountAsync();

            var posts = await postsQuery
                .OrderBy(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ForumPost
                {
                    Id = p.Id,
                    ThreadId = p.ThreadId,
                    Content = p.Content,
                    Author = new Author { Id = p.AuthorId, Name = p.Author.FullName ?? p.Author.Login },
                    Votes = p.Votes,
                    IsFirstPost = p.IsFirstPost,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return new ForumThreadDetail
            {
                Thread = new ForumThreadList
                {
                    Id = thread.Id,
                    Title = thread.Title,
                    Slug = thread.Slug,
                    Author = new Author { Id = thread.AuthorId, Name = thread.Author.FullName ?? thread.Author.Login },
                    ViewCount = thread.ViewCount,
                    ReplyCount = totalPosts - 1,
                    IsPinned = thread.IsPinned,
                    IsLocked = thread.IsLocked,
                    CreatedAt = thread.CreatedAt
                },
                Posts = new PaginatedResult<ForumPost>
                {
                    Items = posts,
                    TotalItems = totalPosts,
                    Page = page,
                    PageSize = pageSize
                }
            };
        }

        public async Task<ForumThreadModel> CreateThreadAsync(int userId, CreateThreadRequest request)
        {
            var slug = GenerateSlug(request.Title);

            // Ensure slug uniqueness
            var existingSlug = await _db.ForumThread.AnyAsync(t => t.Slug == slug);
            if (existingSlug)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var thread = new ForumThreadModel
                {
                    CategoryId = request.CategoryId,
                    AuthorId = userId,
                    Title = request.Title,
                    Slug = slug,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ForumThread.Add(thread);
                await _db.SaveChangesAsync();

                var post = new ForumPostModel
                {
                    ThreadId = thread.Id,
                    AuthorId = userId,
                    Content = request.Content,
                    IsFirstPost = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ForumPost.Add(post);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
                return thread;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ForumPostModel> CreatePostAsync(int userId, int threadId, CreatePostRequest request)
        {
            var post = new ForumPostModel
            {
                ThreadId = threadId,
                AuthorId = userId,
                Content = request.Content,
                IsFirstPost = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.ForumPost.Add(post);
            await _db.SaveChangesAsync();
            return post;
        }

        public async Task<ForumPostModel?> UpdatePostAsync(int userId, int postId, UpdatePostRequest request)
        {
            var post = await _db.ForumPost.FindAsync(postId);
            if (post == null || post.AuthorId != userId) return null;

            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return post;
        }

        public async Task<ForumPostModel?> VotePostAsync(int postId, bool isUpvote)
        {
            var post = await _db.ForumPost.FindAsync(postId);
            if (post == null) return null;

            // Simple increment/decrement implementation
            // In a real scenario, we would track UserVote table to prevent multiple votes
            post.Votes += isUpvote ? 1 : -1;

            await _db.SaveChangesAsync();
            return post;
        }

        public async Task IncrementViewCountAsync(int threadId)
        {
            var thread = await _db.ForumThread.FindAsync(threadId);
            if (thread != null)
            {
                thread.ViewCount++;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsThreadLockedAsync(int threadId)
        {
            var thread = await _db.ForumThread.FindAsync(threadId);
            return thread?.IsLocked ?? false;
        }

        private string GenerateSlug(string title)
        {
            string slug = title.ToLowerInvariant();
            slug = MyRegex().Replace(slug, "");
            slug = MyRegex1().Replace(slug, " ").Trim();
            slug = MyRegex2().Replace(slug, "-");
            return slug;
        }

        [GeneratedRegex(@"[^a-z0-9\s-]")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex1();
        [GeneratedRegex(@"\s")]
        private static partial Regex MyRegex2();
    }
}