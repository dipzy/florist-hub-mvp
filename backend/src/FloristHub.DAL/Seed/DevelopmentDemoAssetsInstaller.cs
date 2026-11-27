using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FloristHub.DAL.Seed;

/// <summary>Copia imagens versionadas para o storage local em desenvolvimento.</summary>
public static class DevelopmentDemoAssetsInstaller
{
    public static void EnsureInstalled(IHostEnvironment env, IConfiguration configuration, ILogger log)
    {
        if (!env.IsDevelopment())
            return;

        var uploadsRoot = configuration["Storage:Local:RootPath"] ?? "uploads";
        if (!Path.IsPathRooted(uploadsRoot))
            uploadsRoot = Path.Combine(env.ContentRootPath, uploadsRoot);

        var demoDest = Path.Combine(uploadsRoot, "demo");
        var demoSource = Path.Combine(env.ContentRootPath, "demo-assets");
        Directory.CreateDirectory(demoDest);

        if (!Directory.Exists(demoSource))
        {
            log.LogWarning("Pasta demo-assets em falta: {Path}", demoSource);
            return;
        }

        foreach (var source in Directory.EnumerateFiles(demoSource, "*.jpg", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(source);
            var dest = Path.Combine(demoDest, fileName);
            File.Copy(source, dest, overwrite: true);
            log.LogDebug("Imagem demo sincronizada: {Dest}", dest);
        }
    }
}
