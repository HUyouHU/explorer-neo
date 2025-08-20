using System.Collections.Generic;

namespace CustomExplorer
{
    public class FolderMetadata
    {
        public string Path { get; set; }
        public string Color { get; set; }
        public string Tags { get; set; } // Stored as a single comma-separated string
        public string Comment { get; set; }
    }
}
