using System;

namespace StandNatura.Models
{
    public class Sighting
    {
        public int SightingId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; }
        public string Photo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DenialReason { get; set; } = string.Empty;
        public string ArchiveReason { get; set; } = string.Empty;
    }
}