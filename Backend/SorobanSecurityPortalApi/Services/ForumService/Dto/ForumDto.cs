using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SorobanSecurityPortalApi.Services.ForumService.Dto
{
    public class ForumCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("ThreadCount")]
        public int ThreadCount { get; set; }
        [JsonPropertyName("sort_order")]
        public int SortOrder { get; set; }
    }

    public class ForumThreadList
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
        [JsonPropertyName("author")]
        public Author Author { get; set; } = new();
        [JsonPropertyName("view_count")]
        public int ViewCount { get; set; }
        [JsonPropertyName("reply_count")]
        public int ReplyCount { get; set; }
        [JsonPropertyName("is_pinned")]
        public bool IsPinned { get; set; }
        [JsonPropertyName("is_locked")]
        public bool IsLocked { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("last_post_at")]
        public DateTime? LastPostAt { get; set; }
    }

    public class ForumThreadDetail
    {
        [JsonPropertyName("thread")]
        public ForumThreadList Thread { get; set; } = new();
        [JsonPropertyName("posts")]
        public PaginatedResult<ForumPost> Posts { get; set; } = new();
    }

    public class ForumPost
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("thread_id")]
        public int ThreadId { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("author")]
        public Author Author { get; set; } = new();
        [JsonPropertyName("votes")]
        public int Votes { get; set; }
        [JsonPropertyName("is_first_post")]
        public bool IsFirstPost { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        // Add avatar URL if available in Login/Profile models
    }

    public class CreateThreadRequest
    {
        [Required]
        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }
        [Required]
        [MaxLength(200)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class CreatePostRequest
    {
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class UpdatePostRequest
    {
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class VotePostRequest
    {
        [JsonPropertyName("is_upvote")]
        public bool IsUpvote { get; set; }
    }

    public class PaginatedResult<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = [];
        [JsonPropertyName("total_items")]
        public int TotalItems { get; set; }
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }
    }
}