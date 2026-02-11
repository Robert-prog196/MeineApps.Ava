namespace FinanzRechner.Services;

/// <summary>
/// Service for exporting transactions in various formats
/// </summary>
public interface IExportService
{
    Task<string> ExportToCsvAsync(int year, int month, string? targetPath = null);
    Task<string> ExportToCsvAsync(DateTime startDate, DateTime endDate, string? targetPath = null);
    Task<string> ExportAllToCsvAsync(string? targetPath = null);
    Task<string> ExportStatisticsToPdfAsync(string period, string? targetPath = null);
    Task<string> ExportStatisticsToPdfAsync(string period, DateTime startDate, DateTime endDate, string? targetPath = null);
}
