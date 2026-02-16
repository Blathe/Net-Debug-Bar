using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetDebugBar.Demo.Data;
using NetDebugBar.Demo.Data.Models;

namespace NetDebugBar.Demo.Pages
{
    public class NPlusOneTestModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NPlusOneTestModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Game> Games { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Simulate a realistic scenario: user has a list of favorite game IDs
            // and we want to display details for each one

            // Step 1: Get game IDs (e.g., from user favorites, search results, etc.)
            var gameIds = await _context.Games
                .OrderBy(g => g.Name)
                .Take(10)
                .Select(g => g.Id)
                .ToListAsync();

            // Step 2: ANTI-PATTERN - Load each game individually in a loop
            // This creates N+1 queries: one query per game!
            Games = new List<Game>();
            foreach (var id in gameIds)
            {
                // Each iteration executes a separate query:
                // SELECT * FROM Games WHERE Id = @p0
                // This is the N+1 problem!
                var game = await _context.Games
                    .Include(g => g.Publisher) // Even with Include, it's still N queries
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (game != null)
                    Games.Add(game);
            }

            // CORRECT APPROACH (commented out to show the problem):
            // var games = await _context.Games
            //     .Where(g => gameIds.Contains(g.Id))
            //     .Include(g => g.Publisher)
            //     .ToListAsync();
            // This would be a SINGLE query with a WHERE IN clause
        }
    }
}
