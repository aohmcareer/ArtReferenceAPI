using ArtReferenceAPI.Models;
using ArtReferenceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArtReferenceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly ImageService _imageService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(ImageService imageService, ILogger<ImagesController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        [HttpGet("random")]
        public ActionResult<ImageInfo> GetRandomImage([FromQuery] string? folder = null, [FromQuery] string? tags = null)
        {
            var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var image = _imageService.GetRandomImage(folder, tagList);
            if (image == null)
            {
                _logger.LogWarning("No random image found for folder: {Folder}, tags: {Tags}", folder, tags);
                return NotFound(new { message = "No matching image found for the selected criteria." });
            }
            return Ok(image);
        }

        [HttpGet("gallery")]
        public ActionResult<PaginatedResult<ImageInfo>> GetGalleryImages(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? folder = null, 
            [FromQuery] string? tags = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100; 

            var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = _imageService.GetImages(page, pageSize, folder, tagList);
            return Ok(result);
        }

        [HttpGet("folders")]
        public ActionResult<IEnumerable<FolderInfo>> GetFolders([FromQuery] string? tags = null)
        {
            var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Ok(_imageService.GetFolders(tagList));
        }

        [HttpGet("tags")]
        public ActionResult<IEnumerable<string>> GetAllTags()
        {
            return Ok(_imageService.GetAllUniqueTags());
        }
    }
}