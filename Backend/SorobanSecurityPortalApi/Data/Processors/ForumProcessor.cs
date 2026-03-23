using Microsoft.Extensions.Caching.Memory;
using SorobanSecurityPortalApi.Services.ForumService;

namespace SorobanSecurityPortalApi.Data.Processors
{
    public class ForumProcessor : IForumProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;

        public ForumProcessor(IServiceProvider serviceProvider, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        public async Task RegisterViewAsync(int threadId, string ipAddress)
        {
            string cacheKey = $"view_thread_{threadId}_ip_{ipAddress}";

            // Rate limiting: Check if this IP viewed this thread recently
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var forumService = scope.ServiceProvider.GetRequiredService<IForumService>();
                    await forumService.IncrementViewCountAsync(threadId);
                }

                // Set cache to prevent re-counting for 1 hour
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(cacheKey, true, cacheEntryOptions);
            }
        }
    }


    public interface IForumProcessor
    {
        Task RegisterViewAsync(int threadId, string ipAddress);
    }
}