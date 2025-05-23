namespace ArtReferenceAPI.Models
{
    public class ImageInfo
    {
        public required string FileName { get; set; }
        public required string RelativePath { get; set; } // Relative to RootPath
        public required string Url { get; set; } // Full URL path part to access the image
        public required string FolderName { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}