using FitnessRechner.Models;

namespace FitnessRechner.Services;

/// <summary>
/// Service for barcode lookup via Open Food Facts API
/// </summary>
public interface IBarcodeLookupService
{
    /// <summary>
    /// Looks up a food item by its barcode in the Open Food Facts database.
    /// </summary>
    /// <param name="barcode">The scanned barcode (EAN, UPC, etc.)</param>
    /// <returns>FoodItem if found, null otherwise</returns>
    Task<FoodItem?> LookupByBarcodeAsync(string barcode);

    /// <summary>
    /// Returns the recently scanned barcodes, sorted by last scan.
    /// </summary>
    /// <param name="limit">Maximum number of entries</param>
    Task<IReadOnlyList<CachedBarcodeEntry>> GetScanHistoryAsync(int limit = 10);

    /// <summary>
    /// Clears the entire scan history.
    /// </summary>
    Task ClearScanHistoryAsync();
}
