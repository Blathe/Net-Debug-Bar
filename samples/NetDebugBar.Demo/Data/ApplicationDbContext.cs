using NetDebugBar.Demo.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NetDebugBar.Demo.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between Game and Genre
            modelBuilder.Entity<GameGenre>()
                .HasKey(gg => new { gg.GameId, gg.GenreId });

            modelBuilder.Entity<GameGenre>()
                .HasOne(gg => gg.Game)
                .WithMany(g => g.GameGenres)
                .HasForeignKey(gg => gg.GameId);

            modelBuilder.Entity<GameGenre>()
                .HasOne(gg => gg.Genre)
                .WithMany(g => g.GameGenres)
                .HasForeignKey(gg => gg.GenreId);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Publishers
            modelBuilder.Entity<Publisher>().HasData(
                new Publisher { Id = 1, Name = "Electronic Arts", Country = "USA", Website = "https://ea.com", FoundedDate = new DateTime(1982, 5, 28) },
                new Publisher { Id = 2, Name = "Ubisoft", Country = "France", Website = "https://ubisoft.com", FoundedDate = new DateTime(1986, 3, 12) },
                new Publisher { Id = 3, Name = "Nintendo", Country = "Japan", Website = "https://nintendo.com", FoundedDate = new DateTime(1889, 9, 23) },
                new Publisher { Id = 4, Name = "Valve Corporation", Country = "USA", Website = "https://valvesoftware.com", FoundedDate = new DateTime(1996, 8, 24) },
                new Publisher { Id = 5, Name = "CD Projekt", Country = "Poland", Website = "https://cdprojekt.com", FoundedDate = new DateTime(1994, 5, 1) }
            );

            // Seed Genres
            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Action", Description = "Fast-paced games focused on physical challenges" },
                new Genre { Id = 2, Name = "RPG", Description = "Role-playing games with character development" },
                new Genre { Id = 3, Name = "Strategy", Description = "Games requiring careful planning and tactics" },
                new Genre { Id = 4, Name = "Adventure", Description = "Story-driven exploration games" },
                new Genre { Id = 5, Name = "Puzzle", Description = "Games focused on problem-solving" },
                new Genre { Id = 6, Name = "Sports", Description = "Athletic competition simulations" },
                new Genre { Id = 7, Name = "Shooter", Description = "Combat games with ranged weapons" }
            );

            // Seed Games
            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Name = "Portal 2",
                    Description = "A mind-bending puzzle platformer with portal mechanics",
                    ReleaseDate = new DateTime(2011, 4, 19),
                    Price = 19.99m,
                    Rating = 4.9,
                    PublisherId = 4,
                    CoverImageUrl = "https://placeholder.com/portal2.jpg"
                },
                new Game
                {
                    Id = 2,
                    Name = "The Witcher 3",
                    Description = "Epic open-world RPG following Geralt of Rivia",
                    ReleaseDate = new DateTime(2015, 5, 19),
                    Price = 39.99m,
                    Rating = 4.8,
                    PublisherId = 5,
                    CoverImageUrl = "https://placeholder.com/witcher3.jpg"
                },
                new Game
                {
                    Id = 3,
                    Name = "Zelda: Breath of the Wild",
                    Description = "Revolutionary open-world adventure in Hyrule",
                    ReleaseDate = new DateTime(2017, 3, 3),
                    Price = 59.99m,
                    Rating = 4.9,
                    PublisherId = 3,
                    CoverImageUrl = "https://placeholder.com/zelda.jpg"
                },
                new Game
                {
                    Id = 4,
                    Name = "Assassin's Creed Valhalla",
                    Description = "Viking saga in medieval England",
                    ReleaseDate = new DateTime(2020, 11, 10),
                    Price = 59.99m,
                    Rating = 4.2,
                    PublisherId = 2,
                    CoverImageUrl = "https://placeholder.com/acvalhalla.jpg"
                },
                new Game
                {
                    Id = 5,
                    Name = "FIFA 24",
                    Description = "The ultimate football simulation",
                    ReleaseDate = new DateTime(2023, 9, 29),
                    Price = 69.99m,
                    Rating = 3.8,
                    PublisherId = 1,
                    CoverImageUrl = "https://placeholder.com/fifa24.jpg"
                }
            );

            // Seed GameGenres (many-to-many)
            modelBuilder.Entity<GameGenre>().HasData(
                // Portal 2: Puzzle, Action
                new GameGenre { GameId = 1, GenreId = 5 },
                new GameGenre { GameId = 1, GenreId = 1 },
                // Witcher 3: RPG, Action, Adventure
                new GameGenre { GameId = 2, GenreId = 2 },
                new GameGenre { GameId = 2, GenreId = 1 },
                new GameGenre { GameId = 2, GenreId = 4 },
                // Zelda: Adventure, Action, RPG
                new GameGenre { GameId = 3, GenreId = 4 },
                new GameGenre { GameId = 3, GenreId = 1 },
                new GameGenre { GameId = 3, GenreId = 2 },
                // AC Valhalla: Action, Adventure, RPG
                new GameGenre { GameId = 4, GenreId = 1 },
                new GameGenre { GameId = 4, GenreId = 4 },
                new GameGenre { GameId = 4, GenreId = 2 },
                // FIFA: Sports
                new GameGenre { GameId = 5, GenreId = 6 }
            );

            // Seed Reviews
            modelBuilder.Entity<Review>().HasData(
                new Review { Id = 1, GameId = 1, ReviewerName = "John Doe", Rating = 5, Comment = "Best puzzle game ever!", ReviewDate = new DateTime(2024, 1, 15) },
                new Review { Id = 2, GameId = 1, ReviewerName = "Jane Smith", Rating = 5, Comment = "Absolutely brilliant mechanics", ReviewDate = new DateTime(2024, 2, 3) },
                new Review { Id = 3, GameId = 2, ReviewerName = "Alex Johnson", Rating = 5, Comment = "Masterpiece of storytelling", ReviewDate = new DateTime(2024, 1, 20) },
                new Review { Id = 4, GameId = 2, ReviewerName = "Sarah Williams", Rating = 4, Comment = "Great game, minor bugs", ReviewDate = new DateTime(2024, 2, 10) },
                new Review { Id = 5, GameId = 3, ReviewerName = "Mike Brown", Rating = 5, Comment = "Changed open-world gaming forever", ReviewDate = new DateTime(2024, 1, 25) },
                new Review { Id = 6, GameId = 3, ReviewerName = "Emily Davis", Rating = 5, Comment = "Perfect adventure", ReviewDate = new DateTime(2024, 2, 14) },
                new Review { Id = 7, GameId = 4, ReviewerName = "Chris Wilson", Rating = 4, Comment = "Good Viking experience", ReviewDate = new DateTime(2024, 1, 30) },
                new Review { Id = 8, GameId = 5, ReviewerName = "Pat Martinez", Rating = 3, Comment = "Same as last year", ReviewDate = new DateTime(2024, 2, 5) }
            );
        }
    }
}
