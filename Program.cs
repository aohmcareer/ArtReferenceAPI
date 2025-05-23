using ArtReferenceAPI.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options; // Required for IOptionsMonitor

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection("ImageSettings"));
builder.Services.AddSingleton<ImageService>(); 
builder.Services.AddMemoryCache();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") 
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

// Serve static image files
// Use IOptionsMonitor to get settings if they might change or for cleaner access
var imageSettingsSnapshot = app.Services.GetRequiredService<IOptionsMonitor<ImageSettings>>().CurrentValue;

if (imageSettingsSnapshot != null && 
    !string.IsNullOrEmpty(imageSettingsSnapshot.RootPath) && 
    Directory.Exists(imageSettingsSnapshot.RootPath) && 
    !string.IsNullOrEmpty(imageSettingsSnapshot.BaseServePath))
{
    app.Logger.LogInformation("Serving static files from: {PhysicalPath} at URL base: {RequestPath}", 
        imageSettingsSnapshot.RootPath, imageSettingsSnapshot.BaseServePath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageSettingsSnapshot.RootPath),
        RequestPath = imageSettingsSnapshot.BaseServePath 
    });
}
else
{
    app.Logger.LogWarning("ImageSettings.RootPath is not configured correctly, directory does not exist, or BaseServePath is missing. Static images will not be served.");
    app.Logger.LogWarning("Current settings - RootPath: '{RootPath}', BaseServePath: '{BaseServePath}', Directory Exists: {DirExists}", 
        imageSettingsSnapshot?.RootPath, 
        imageSettingsSnapshot?.BaseServePath, 
        (imageSettingsSnapshot != null && !string.IsNullOrEmpty(imageSettingsSnapshot.RootPath) ? Directory.Exists(imageSettingsSnapshot.RootPath).ToString() : "N/A (RootPath null/empty)"));
}

app.MapControllers();

app.Run();