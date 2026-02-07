namespace MeineApps.Core.Ava.Services;

/// <summary>
/// Service zum Teilen/Oeffnen von Dateien (plattformuebergreifend).
/// Desktop: Datei mit Standard-App oeffnen.
/// Android: Share-Intent mit FileProvider.
/// </summary>
public interface IFileShareService
{
    /// <summary>
    /// Oeffnet den Share-/Open-Dialog fuer eine Datei.
    /// </summary>
    Task<bool> ShareFileAsync(string filePath, string title, string mimeType);

    /// <summary>
    /// Gibt den plattformspezifischen Export-Ordner zurueck.
    /// Desktop: Dokumente/{appName}
    /// Android: App-spezifisches externes Verzeichnis
    /// </summary>
    string GetExportDirectory(string appName);
}
