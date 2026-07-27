using Microsoft.Extensions.Configuration;

namespace Digi.Shared.Helper;

public static class CentralConfigurationExtensions
{
    public static ConfigurationManager AddCentralConfiguration(this ConfigurationManager configuration)
    {
        var configuredPath = configuration["CentralConfig:Path"]
            ?? Environment.GetEnvironmentVariable("DIGIERP_CENTRAL_CONFIG_PATH");

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            candidates.Add(configuredPath);
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.central.json"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "appsettings.central.json"));

        foreach (var basePath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(basePath);
            var depth = 0;
            while (dir != null && depth < 8)
            {
                candidates.Add(Path.Combine(dir.FullName, "appsettings.central.json"));
                dir = dir.Parent;
                depth++;
            }
        }

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            configuration.AddJsonFile(path, optional: false, reloadOnChange: true);
            break;
        }

        return configuration;
    }
}
