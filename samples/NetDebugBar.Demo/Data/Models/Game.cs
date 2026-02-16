namespace NetDebugBar.Demo.Data.Models
{
    public class Game
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public decimal Price { get; set; }
        public double Rating { get; set; }
        public string? CoverImageUrl { get; set; }

        // Foreign key
        public int? PublisherId { get; set; }

        // Navigation properties
        public Publisher? Publisher { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    }
}
