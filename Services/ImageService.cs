using ArtReferenceAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ArtReferenceAPI.Services
{
    // ImageSettings class remains the same
    public class ImageSettings
    {
        public required string RootPath { get; set; }
        public required string BaseServePath { get; set; }
    }

    public class ImageService
    {
        private readonly IMemoryCache _cache;
        private readonly ImageSettings _settings;
        private readonly ILogger<ImageService> _logger;
        private const string AllImagesCacheKey = "AllImages";
        private const string AllFoldersCacheKey = "AllFolders";
        private static readonly Random _random = new();

        public ImageService(IMemoryCache cache, IOptions<ImageSettings> settings, ILogger<ImageService> logger)
        {
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
            InitializeCache();
        }

        private void InitializeCache()
        {
            _logger.LogInformation("Initializing image cache from path: {RootPath}", _settings.RootPath);
            if (string.IsNullOrEmpty(_settings.RootPath) || !Directory.Exists(_settings.RootPath))
            {
                _logger.LogError("Image root path '{RootPath}' is null, empty, or does not exist.", _settings.RootPath);
                _cache.Set(AllImagesCacheKey, new List<ImageInfo>());
                _cache.Set(AllFoldersCacheKey, new List<FolderInfo>());
                return;
            }

            var allImages = new List<ImageInfo>();
            var allFolders = new List<FolderInfo>();
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            try
            {
                // Get directories directly under RootPath. These are now our "Image Set Folders".
                var imageSetFolders = Directory.GetDirectories(_settings.RootPath);
                _logger.LogInformation("Found {Count} potential image set folders directly under RootPath.", imageSetFolders.Length);


                foreach (var folderPath in imageSetFolders) // folderPath is now like "Z:\SynologyDrive\References\ReferenceFolder"
                {
                    var folderName = Path.GetFileName(folderPath); // e.g., "ReferenceFolder"
                    _logger.LogDebug("Processing image set folder: {FolderName} at path {FolderPath}", folderName, folderPath);

                    var metadataFile = Directory.EnumerateFiles(folderPath, "*-metadata.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    List<string> tags = new List<string>();

                    if (metadataFile != null)
                    {
                        try
                        {
                            var metadataJson = File.ReadAllText(metadataFile);
                            tags = JsonSerializer.Deserialize<List<string>>(metadataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
                            _logger.LogDebug("Successfully parsed metadata for {FolderName} from {MetadataFile} with {TagCount} tags.", folderName, metadataFile, tags.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse metadata for folder {FolderName} from {MetadataFile}", folderName, metadataFile);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Metadata file ending with '-metadata.json' not found for folder {FolderName} in {FolderPath}", folderName, folderPath);
                    }

                    var imagesInFolder = Directory.GetFiles(folderPath)
                       .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
                       .Select(filePath =>
                       {
                           var fileName = Path.GetFileName(filePath);
                           // RelativePath should be relative to _settings.RootPath
                           // e.g., "ReferenceFolder/image001.jpg"
                           var relativeFilePath = Path.GetRelativePath(_settings.RootPath, filePath);
                           var urlPath = Path.Combine(_settings.BaseServePath, relativeFilePath).Replace("\\", "/");
                           _logger.LogTrace("Found image: {FileName} in folder {FolderName}, relative path: {RelativePath}, URL path: {UrlPath}", fileName, folderName, relativeFilePath, urlPath);
                           return new ImageInfo
                           {
                               FileName = fileName,
                               RelativePath = relativeFilePath,
                               Url = urlPath,
                               FolderName = folderName, // This is the "Image Set Folder" name
                               Tags = new List<string>(tags)
                           };
                       }).ToList();

                    allImages.AddRange(imagesInFolder);
                    if (imagesInFolder.Any())
                    {
                        allFolders.Add(new FolderInfo
                        {
                            Name = folderName, // This is the "Image Set Folder" name
                            RelativePath = Path.GetRelativePath(_settings.RootPath, folderPath), // e.g., "ReferenceFolder"
                            Tags = tags,
                            ImageCount = imagesInFolder.Count
                        });
                        _logger.LogDebug("Added folder {FolderName} with {ImageCount} images to cache.", folderName, imagesInFolder.Count);
                    }
                    else
                    {
                        _logger.LogDebug("No images found in folder {FolderName}.", folderName);
                    }
                }
                _cache.Set(AllImagesCacheKey, allImages, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
                _cache.Set(AllFoldersCacheKey, allFolders, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
                _logger.LogInformation("FINISHED CACHING: Cached {ImageCount} images from {FolderCount} folders.", allImages.Count, allFolders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during image cache initialization.");
                _cache.Set(AllImagesCacheKey, new List<ImageInfo>());
                _cache.Set(AllFoldersCacheKey, new List<FolderInfo>());
            }
        }

        // The rest of the ImageService methods (GetAllImagesFromCache, GetRandomImage, GetImages, GetFolders, GetAllUniqueTags) remain the same
        // They operate on the cached lists, which are now populated
        public List<ImageInfo> GetAllImagesFromCache() => _cache.Get<List<ImageInfo>>(AllImagesCacheKey) ?? new List<ImageInfo>();
        public List<FolderInfo> GetAllFoldersFromCache() => _cache.Get<List<FolderInfo>>(AllFoldersCacheKey) ?? new List<FolderInfo>();

        public ImageInfo? GetRandomImage(string? folderName = null, IEnumerable<string>? tags = null)
        {
            var allImages = GetAllImagesFromCache();
            if (!allImages.Any()) return null;

            IEnumerable<ImageInfo> filteredImages = allImages;

            if (!string.IsNullOrEmpty(folderName))
            {
                // folderName here will be "ImageFolder"
                filteredImages = filteredImages.Where(img =>
                    img.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase));
            }

            if (tags != null && tags.Any())
            {
                var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                filteredImages = filteredImages.Where(img => img.Tags.Any(t => tagSet.Contains(t)));
            }

            var eligibleImages = filteredImages.ToList();
            if (!eligibleImages.Any())
            {
                _logger.LogWarning("GetRandomImage: No eligible images found after filtering. Folder: {Folder}, Tags: {Tags}", folderName, string.Join(",", tags ?? Enumerable.Empty<string>()));
                return null;
            }

            var selectedImage = eligibleImages[_random.Next(eligibleImages.Count)];
            _logger.LogDebug("GetRandomImage: Selected random image {FileName} from folder {ImageFolder}", selectedImage.FileName, selectedImage.FolderName);
            return selectedImage;
        }

        public PaginatedResult<ImageInfo> GetImages(int page, int pageSize, string? folderName = null, IEnumerable<string>? tags = null)
        {
            var allImages = GetAllImagesFromCache();
            IEnumerable<ImageInfo> query = allImages;

            if (!string.IsNullOrEmpty(folderName))
            {
                query = query.Where(img => img.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase));
            }

            if (tags != null && tags.Any())
            {
                var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                query = query.Where(img => img.Tags.Any(t => tagSet.Contains(t)));
            }

            var totalCount = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            _logger.LogDebug("GetImages: Page {Page}, PageSize {PageSize}, Folder: {Folder}, Tags: {Tags} - Found {Count} items, Total {Total}",
               page, pageSize, folderName, string.Join(",", tags ?? Enumerable.Empty<string>()), items.Count, totalCount);

            return new PaginatedResult<ImageInfo>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public IEnumerable<FolderInfo> GetFolders(IEnumerable<string>? tags = null)
        {
            var allFolders = GetAllFoldersFromCache();
            IEnumerable<FolderInfo> result;
            if (tags == null || !tags.Any())
            {
                result = allFolders;
            }
            else
            {
                var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
                result = allFolders.Where(f => f.Tags.Any(t => tagSet.Contains(t)));
            }
            _logger.LogDebug("GetFolders: Tags: {Tags} - Returning {Count} folders", string.Join(",", tags ?? Enumerable.Empty<string>()), result.Count());
            return result;
        }

        public IEnumerable<string> GetAllUniqueTags()
        {
            var uniqueTags = GetAllFoldersFromCache()
                .SelectMany(f => f.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList(); // ToList to log count easily
            _logger.LogDebug("GetAllUniqueTags: Found {Count} unique tags.", uniqueTags.Count);
            return uniqueTags;
        }
    }
}
