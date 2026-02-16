namespace NetDebugBar.Demo.Data.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }
        public DateTime? FoundedDate { get; set; }

        // Navigation property
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}
