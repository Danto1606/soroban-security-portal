using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SorobanSecurityPortalApi.Data.Processors;
using SorobanSecurityPortalApi.Services.ForumService;
using SorobanSecurityPortalApi.Services.ForumService.Dto;

namespace SorobanSecurityPortalApi.Controllers
{
    [ApiController]
    [Route("api/v1/forum")]
    public class ForumController : ControllerBase
    {
        private readonly IForumService _forumService;
        private readonly IForumProcessor _forumProcessor;

        public ForumController(IForumService forumService, IForumProcessor forumProcessor)
        {
            _forumService = forumService;
            _forumProcessor = forumProcessor;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _forumService.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("categories/{slug}/threads")]
        public async Task<IActionResult> GetThreads(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _forumService.GetThreadsByCategoryAsync(slug, page, pageSize);
            return Ok(result);
        }

        [HttpGet("threads/{slug}")]
        public async Task<IActionResult> GetThread(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _forumService.GetThreadBySlugAsync(slug, page, pageSize);
            if (result == null)
            {
                return NotFound();
            }

            // Process view count asynchronously
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _ = _forumProcessor.RegisterViewAsync(result.Thread.Id, ip);

            return Ok(result);
        }

        [HttpPost("threads")]
        [Authorize]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            try
            {
                var thread = await _forumService.CreateThreadAsync(userId, request);
                return CreatedAtAction(nameof(GetThread), new { slug = thread.Slug }, new { id = thread.Id, slug = thread.Slug });
            }
            catch (Exception)
            {
                // In production, log error
                return StatusCode(500, "Error creating thread.");
            }
        }

        [HttpPost("threads/{id}/posts")]
        [Authorize]
        public async Task<IActionResult> ReplyToThread(int id, [FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isLocked = await _forumService.IsThreadLockedAsync(id);
            if (isLocked)
            {
                return BadRequest("Thread is locked.");
            }

            var userId = GetCurrentUserId();
            try
            {
                var post = await _forumService.CreatePostAsync(userId, id, request);
                return Ok(new { id = post.Id });
            }
            catch (Exception)
            {
                return NotFound("Thread not found.");
            }
        }

        [HttpPut("posts/{id}")]
        [Authorize]
        public async Task<IActionResult> EditPost(int id, [FromBody] UpdatePostRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var updatedPost = await _forumService.UpdatePostAsync(userId, id, request);

            if (updatedPost == null)
            {
                return NotFound("Post not found or you are not the author.");
            }

            return Ok(new { id = updatedPost.Id });
        }

        [HttpPost("posts/{id}/vote")]
        [Authorize]
        public async Task<IActionResult> VotePost(int id, [FromBody] VotePostRequest request)
        {
            var post = await _forumService.VotePostAsync(id, request.IsUpvote);
            if (post == null)
            {
                return NotFound();
            }

            return Ok(new { votes = post.Votes });
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            // Fallback to NameIdentifier if "id" claim is missing (standard JWT often uses sub/nameid)
            if (string.IsNullOrEmpty(idClaim))
            {
                idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (int.TryParse(idClaim, out int userId))
            {
                return userId;
            }

            // If we are testing with the default admin seed, it might have ID 1
            // But in a real secure context we should throw if claim is missing
            throw new UnauthorizedAccessException("User ID claim not found.");
        }
    }
}