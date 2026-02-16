using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetDebugBar.Demo.Data;
using NetDebugBar.Demo.Data.Models;

namespace NetDebugBar.Demo.Pages.Games
{
    public class DetailsModel : PageModel
    {
        private readonly NetDebugBar.Demo.Data.ApplicationDbContext _context;

        public DetailsModel(NetDebugBar.Demo.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Game Game { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Publisher)
                .Include(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(g => g.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (game is not null)
            {
                Game = game;

                return Page();
            }

            return NotFound();
        }
    }
}
