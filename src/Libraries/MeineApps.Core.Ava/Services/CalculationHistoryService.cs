using System.Text.Json;

namespace MeineApps.Core.Ava.Services;

/// <summary>
/// JSON-file-based calculation history service.
/// Stores the last 10 calculations per calculator type.
/// </summary>
public class CalculationHistoryService : ICalculationHistoryService
{
    private const string HistoryFolder = "calculation_history";
    private const int MaxItemsPerCalculator = 10;

    private readonly string _historyPath;

    public CalculationHistoryService()
    {
        _historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeineApps", HistoryFolder);
        Directory.CreateDirectory(_historyPath);
    }

    public async Task AddCalculationAsync(string calculatorId, string title, Dictionary<string, object> data)
    {
        try
        {
            var item = new CalculationHistoryItem
            {
                Id = Guid.NewGuid().ToString(),
                CalculatorId = calculatorId,
                Title = title,
                Data = data,
                CreatedAt = DateTime.Now
            };

            var history = await GetHistoryAsync(calculatorId, 100);
            history.Insert(0, item);

            if (history.Count > MaxItemsPerCalculator)
                history = history.Take(MaxItemsPerCalculator).ToList();

            await SaveHistoryAsync(calculatorId, history);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: Add error: {ex.Message}");
        }
    }

    public async Task<List<CalculationHistoryItem>> GetHistoryAsync(string calculatorId, int maxItems = 10)
    {
        try
        {
            var filePath = GetHistoryFilePath(calculatorId);
            if (!File.Exists(filePath))
                return [];

            var json = await File.ReadAllTextAsync(filePath);
            var history = JsonSerializer.Deserialize<List<CalculationHistoryItem>>(json) ?? [];
            return history.Take(maxItems).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: Load error: {ex.Message}");
            return [];
        }
    }

    public async Task<CalculationHistoryItem?> GetCalculationAsync(string id)
    {
        try
        {
            var files = Directory.GetFiles(_historyPath, "*.json");
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var history = JsonSerializer.Deserialize<List<CalculationHistoryItem>>(json);
                var item = history?.FirstOrDefault(h => h.Id == id);
                if (item != null) return item;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: GetCalculation error: {ex.Message}");
            return null;
        }
    }

    public async Task DeleteCalculationAsync(string id)
    {
        try
        {
            var files = Directory.GetFiles(_historyPath, "*.json");
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var history = JsonSerializer.Deserialize<List<CalculationHistoryItem>>(json);
                if (history == null) continue;

                var item = history.FirstOrDefault(h => h.Id == id);
                if (item != null)
                {
                    history.Remove(item);
                    var updatedJson = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(file, updatedJson);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: Delete error: {ex.Message}");
        }
    }

    public Task ClearHistoryAsync(string calculatorId)
    {
        try
        {
            var filePath = GetHistoryFilePath(calculatorId);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: Clear error: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public async Task CleanupOldEntriesAsync(int olderThanDays = 90)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
            var files = Directory.GetFiles(_historyPath, "*.json");

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var history = JsonSerializer.Deserialize<List<CalculationHistoryItem>>(json);
                if (history == null) continue;

                var filtered = history.Where(h => h.CreatedAt > cutoffDate).ToList();
                if (filtered.Count != history.Count)
                {
                    var updatedJson = JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(file, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CalculationHistoryService: Cleanup error: {ex.Message}");
        }
    }

    private async Task SaveHistoryAsync(string calculatorId, List<CalculationHistoryItem> history)
    {
        var filePath = GetHistoryFilePath(calculatorId);
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private string GetHistoryFilePath(string calculatorId)
        => Path.Combine(_historyPath, $"{calculatorId}.json");
}
