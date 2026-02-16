using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetDebugBar.Demo.Data;
using NetDebugBar.Demo.Data.Models;

namespace NetDebugBar.Demo.Pages.Games
{
    public class IndexModel : PageModel
    {
        private readonly NetDebugBar.Demo.Data.ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(NetDebugBar.Demo.Data.ApplicationDbContext context, IMemoryCache cache, ILogger<IndexModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public IList<Game> Game { get;set; } = default!;

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Loading games list page");

            // Try to get from cache first
            const string cacheKey = "games_list_with_relations";
            if (!_cache.TryGetValue(cacheKey, out IList<Game>? cachedGames))
            {
                // Cache miss - load from database with related entities
                _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

                // Eager load related entities to avoid N+1 queries
                Game = await _context.Games
                    .Include(g => g.Publisher)
                    .Include(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                    .Include(g => g.Reviews)
                    .OrderByDescending(g => g.Rating)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} games from database with related entities", Game.Count);

                // Calculate average rating per game
                foreach (var game in Game)
                {
                    if (game.Reviews.Any())
                    {
                        var avgReviewRating = game.Reviews.Average(r => r.Rating);
                        _logger.LogDebug("Game '{GameName}' has {ReviewCount} reviews with average rating {AvgRating:F2}",
                            game.Name, game.Reviews.Count, avgReviewRating);
                    }
                }

                // Store in cache for 60 seconds
                _cache.Set(cacheKey, Game, TimeSpan.FromSeconds(60));
                _logger.LogDebug("Stored games in cache with 60 second expiration");
            }
            else
            {
                // Cache hit - use cached data
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                Game = cachedGames!;
                _logger.LogInformation("Using cached games list with {Count} items", Game.Count);
            }
        }
    }
}
