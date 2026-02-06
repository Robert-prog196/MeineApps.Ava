using System.Text.Json;

namespace MeineApps.Core.Ava.Services;

/// <summary>
/// JSON file-based preferences service for cross-platform support
/// </summary>
public class PreferencesService : IPreferencesService
{
    private readonly string _filePath;
    private Dictionary<string, JsonElement> _preferences = new();
    private readonly object _lock = new();

    public PreferencesService(string? appName = null)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, appName ?? "MeineApps");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "preferences.json");
        Load();
    }

    public T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (!_preferences.TryGetValue(key, out var element))
                return defaultValue;

            try
            {
                return element.Deserialize<T>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            var json = JsonSerializer.SerializeToElement(value);
            _preferences[key] = json;
            Save();
        }
    }

    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _preferences.ContainsKey(key);
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_preferences.Remove(key))
                Save();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _preferences.Clear();
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _preferences = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
            }
        }
        catch
        {
            _preferences = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_preferences, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
