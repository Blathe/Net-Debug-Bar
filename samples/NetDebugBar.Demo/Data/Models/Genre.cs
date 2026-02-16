namespace NetDebugBar.Demo.Data.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // Navigation property
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    }
}
