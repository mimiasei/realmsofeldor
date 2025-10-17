using System;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Metadata about a saved map for display in map selection UI.
    /// </summary>
    [Serializable]
    public class MapMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int PlayerCount { get; set; }
        public string ThumbnailPath { get; set; } // Optional thumbnail image path
        public bool IsGenerated { get; set; } // True if RMG, false if manually created

        public MapMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
        }

        public MapMetadata(string name, int width, int height, int playerCount, bool isGenerated = true)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Width = width;
            Height = height;
            PlayerCount = playerCount;
            IsGenerated = isGenerated;
            CreatedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
        }

        public string GetDisplayName()
        {
            return $"{Name} ({Width}x{Height}, {PlayerCount} players)";
        }
    }
}
