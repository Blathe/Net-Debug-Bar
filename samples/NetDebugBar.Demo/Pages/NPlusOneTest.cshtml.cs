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
            // Deliberately load games WITHOUT .Include() to cause N+1 queries
            // When we access game.Publisher in the view, EF will execute a separate query for each game
            Games = await _context.Games
                .OrderBy(g => g.Name)
                .Take(10) // Limit to 10 to make it obvious
                .ToListAsync();

            // Force lazy loading of publishers by accessing them here
            // This will trigger N separate queries (one per game)
            foreach (var game in Games)
            {
                _ = game.Publisher?.Name; // Access Publisher to trigger lazy load
            }
        }
    }
}
