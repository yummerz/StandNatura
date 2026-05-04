namespace StandNatura.Models
{
    // This model is used for the Sighting Feed
    // It includes the Username from the Users table
    // instead of just the UserId
    public class SightingDisplay
    {
        public int SightingId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DatePosted { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
    }
}