namespace ArtReferenceAPI.Models
{
    public class FolderInfo
    {
        public required string Name { get; set; }
        public required string RelativePath { get; set; } // Relative to RootPath
        public List<string> Tags { get; set; } = new List<string>();
        public int ImageCount { get; set; }
    }
}