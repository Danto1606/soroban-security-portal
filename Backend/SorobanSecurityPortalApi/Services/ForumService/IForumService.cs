using SorobanSecurityPortalApi.Models.DbModels;
using SorobanSecurityPortalApi.Services.ForumService.Dto;

namespace SorobanSecurityPortalApi.Services.ForumService
{
    public interface IForumService
    {
        Task<List<ForumCategory>> GetCategoriesAsync();
        Task<PaginatedResult<ForumThreadList>> GetThreadsByCategoryAsync(string categorySlug, int page, int pageSize);
        Task<ForumThreadDetail?> GetThreadBySlugAsync(string slug, int page, int pageSize);
        Task<ForumThreadModel> CreateThreadAsync(int userId, CreateThreadRequest request);
        Task<ForumPostModel> CreatePostAsync(int userId, int threadId, CreatePostRequest request);
        Task<ForumPostModel?> UpdatePostAsync(int userId, int postId, UpdatePostRequest request);
        Task<ForumPostModel?> VotePostAsync(int postId, bool isUpvote);
        Task IncrementViewCountAsync(int threadId);
        Task<bool> IsThreadLockedAsync(int threadId);
    }
}