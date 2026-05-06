namespace StandNatura.Models
{
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
        public string Status { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
        public string DenialReason { get; set; } = string.Empty;
    }
}