using System.Text.Json;
using dengue.watch.api.infrastructure.ml.models;

namespace dengue.watch.api.infrastructure.ml;

public class ModelInfoStore
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public ModelInfoStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "infrastructure", "ml", "models");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "model-info.json");
    }

    public ModelInfo? Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath)) return null;
            try
            {
                var txt = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<ModelInfo>(txt);
            }
            catch
            {
                return null;
            }
        }
    }

    public void Save(ModelInfo info)
    {
        lock (_lock)
        {
            var txt = JsonSerializer.Serialize(info, new JsonSerializerOptions{WriteIndented = true});
            // atomic write
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, txt);
            File.Copy(tmp, _filePath, overwrite: true);
            File.Delete(tmp);
        }
    }

    public ModelInfo SaveNewTrained(string name, string description)
    {
        lock (_lock)
        {
            var current = Load();
            string version = "1.0.0";
            if (current != null && !string.IsNullOrWhiteSpace(current.Version))
            {
                // increment patch version
                var parts = current.Version.Split('.').Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
                if (parts.Length < 3) parts = parts.Concat(Enumerable.Repeat(0, 3 - parts.Length)).ToArray();
                parts[2] = parts[2] + 1;
                version = string.Join('.', parts);
            }

            var info = new ModelInfo(
                Name: name,
                Version: version,
                LastTrained: DateTime.UtcNow,
                IsLoaded: true,
                Description: description
            );

            Save(info);
            return info;
        }
    }
}
