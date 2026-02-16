namespace NetDebugBar.Demo.Data.Models
{
    public class Review
    {
        public int Id { get; set; }
        public required string ReviewerName { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }

        // Foreign key
        public int GameId { get; set; }

        // Navigation property
        public Game Game { get; set; } = null!;
    }
}
