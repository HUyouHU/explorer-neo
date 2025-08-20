using System;

namespace CustomExplorer
{
    public class FileSystemItem
    {
        public string Name { get; set; }
        public string DateModified { get; set; }
        public string Type { get; set; }
        public string FullPath { get; set; }
        public string Color { get; set; } // Hex string for the color
    }
}
