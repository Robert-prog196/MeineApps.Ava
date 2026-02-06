using System.Text;
using FinanzRechner.Helpers;
using FinanzRechner.Models;
using MeineApps.Core.Ava.Localization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace FinanzRechner.Services;

/// <summary>
/// Service for exporting transactions in various formats
/// </summary>
public class ExportService : IExportService
{
    private readonly IExpenseService _expenseService;
    private readonly ILocalizationService _localizationService;

    public ExportService(IExpenseService expenseService, ILocalizationService localizationService)
    {
        _expenseService = expenseService;
        _localizationService = localizationService;
    }

    public async Task<string> ExportToCsvAsync(int year, int month, string? targetPath = null)
    {
        var expenses = await _expenseService.GetExpensesByMonthAsync(year, month);
        return await GenerateCsvAsync(expenses, $"transactions_{year}_{month:D2}", targetPath);
    }

    public async Task<string> ExportAllToCsvAsync(string? targetPath = null)
    {
        var expenses = await _expenseService.GetAllExpensesAsync();
        return await GenerateCsvAsync(expenses, "transactions_all", targetPath);
    }

    private async Task<string> GenerateCsvAsync(IEnumerable<Expense> expenses, string fileName, string? targetPath = null)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Date;Type;Category;Description;Amount;Note");

        foreach (var expense in expenses.OrderByDescending(e => e.Date))
        {
            var type = expense.Type == TransactionType.Expense
                ? _localizationService.GetString("Expense") ?? "Expense"
                : _localizationService.GetString("Income") ?? "Income";
            var category = CategoryLocalizationHelper.GetLocalizedName(expense.Category, _localizationService);
            var description = EscapeCsvField(expense.Description);
            var note = EscapeCsvField(expense.Note ?? string.Empty);

            csv.AppendLine($"{expense.Date:yyyy-MM-dd};{type};{category};{description};{expense.Amount:F2};{note}");
        }

        string filePath;
        if (!string.IsNullOrEmpty(targetPath))
        {
            filePath = targetPath;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        else
        {
            var exportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FinanzRechner", "exports");
            Directory.CreateDirectory(exportDir);
            filePath = Path.Combine(exportDir, $"{fileName}.csv");
        }
        await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        return filePath;
    }

    public async Task<string> ExportStatisticsToPdfAsync(string period, string? targetPath = null)
    {
        var allExpenses = await _expenseService.GetAllExpensesAsync();

        var document = new PdfDocument();
        document.Info.Title = $"Financial Statistics - {period}";
        document.Info.Author = "FinanzRechner";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
        var normalFont = new XFont("Arial", 11);
        var smallFont = new XFont("Arial", 9);

        double yPos = 50;
        double leftMargin = 50;
        double pageWidth = page.Width - 100;

        // Title
        gfx.DrawString($"{_localizationService.GetString("Statistics") ?? "Statistics"} - {period}",
            titleFont, XBrushes.Black, new XRect(leftMargin, yPos, pageWidth, 30), XStringFormats.TopLeft);
        yPos += 40;

        gfx.DrawLine(XPens.Gray, leftMargin, yPos, page.Width - leftMargin, yPos);
        yPos += 20;

        // Summary
        var totalExpenses = allExpenses.Where(e => e.Type == TransactionType.Expense).Sum(e => e.Amount);
        var totalIncome = allExpenses.Where(e => e.Type == TransactionType.Income).Sum(e => e.Amount);
        var balance = totalIncome - totalExpenses;

        gfx.DrawString(_localizationService.GetString("Summary") ?? "Summary",
            headerFont, XBrushes.Black, new XRect(leftMargin, yPos, pageWidth, 20), XStringFormats.TopLeft);
        yPos += 25;

        gfx.DrawString($"{_localizationService.GetString("TotalExpenses") ?? "Total Expenses"}:",
            normalFont, XBrushes.Black, leftMargin, yPos);
        gfx.DrawString($"{totalExpenses:N2} \u20ac",
            normalFont, XBrushes.Red, page.Width - leftMargin, yPos, XStringFormats.TopRight);
        yPos += 20;

        gfx.DrawString($"{_localizationService.GetString("TotalIncome") ?? "Total Income"}:",
            normalFont, XBrushes.Black, leftMargin, yPos);
        gfx.DrawString($"{totalIncome:N2} \u20ac",
            normalFont, XBrushes.Green, page.Width - leftMargin, yPos, XStringFormats.TopRight);
        yPos += 20;

        gfx.DrawString($"{_localizationService.GetString("Balance") ?? "Balance"}:",
            headerFont, XBrushes.Black, leftMargin, yPos);
        gfx.DrawString($"{balance:N2} \u20ac",
            headerFont, balance >= 0 ? XBrushes.Green : XBrushes.Red,
            page.Width - leftMargin, yPos, XStringFormats.TopRight);
        yPos += 35;

        // Expenses by category
        var expensesByCategory = allExpenses
            .Where(e => e.Type == TransactionType.Expense)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        if (expensesByCategory.Count > 0)
        {
            gfx.DrawString(_localizationService.GetString("ExpensesByCategory") ?? "Expenses by Category",
                headerFont, XBrushes.Black, new XRect(leftMargin, yPos, pageWidth, 20), XStringFormats.TopLeft);
            yPos += 25;

            foreach (var item in expensesByCategory)
            {
                var categoryName = CategoryLocalizationHelper.GetLocalizedName(item.Category, _localizationService);
                var percentage = totalExpenses > 0 ? (item.Total / totalExpenses * 100) : 0;

                gfx.DrawString($"{categoryName}:", normalFont, XBrushes.Black, leftMargin, yPos);
                gfx.DrawString($"{item.Total:N2} \u20ac ({percentage:F1}%)",
                    normalFont, XBrushes.Black, page.Width - leftMargin, yPos, XStringFormats.TopRight);
                yPos += 18;

                if (yPos > page.Height - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = 50;
                }
            }
        }

        // Footer
        var footerText = $"{_localizationService.GetString("GeneratedBy") ?? "Generated by"} FinanzRechner - {DateTime.Now:dd.MM.yyyy HH:mm}";
        gfx.DrawString(footerText, smallFont, XBrushes.Gray,
            new XRect(0, page.Height - 30, page.Width, 20), XStringFormats.Center);

        string filePath;
        if (!string.IsNullOrEmpty(targetPath))
        {
            filePath = targetPath;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        else
        {
            var exportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FinanzRechner", "exports");
            Directory.CreateDirectory(exportDir);
            filePath = Path.Combine(exportDir, $"statistics_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        document.Save(filePath);
        return filePath;
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }
        return field;
    }
}
