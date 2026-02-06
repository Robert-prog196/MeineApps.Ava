using System.Diagnostics;
using System.Globalization;
using System.Text;
using WorkTimePro.Models;
using WorkTimePro.Resources.Strings;

namespace WorkTimePro.Services;

/// <summary>
/// Export service for PDF, Excel and CSV
/// PDF uses PdfSharpCore (Avalonia), Excel uses ClosedXML
/// </summary>
public class ExportService : IExportService
{
    private readonly IDatabaseService _database;
    private readonly ICalculationService _calculation;

    private static string CacheDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WorkTimePro", "Cache");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public ExportService(IDatabaseService database, ICalculationService calculation)
    {
        _database = database;
        _calculation = calculation;
    }

    #region PDF Export

    public async Task<string> ExportMonthToPdfAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await ExportRangeToPdfAsync(start, end);
    }

    public async Task<string> ExportRangeToPdfAsync(DateTime start, DateTime end)
    {
        // TODO: Implement with PdfSharpCore
        // The MAUI version uses Syncfusion.Pdf which is not available in Avalonia.
        // Replace with PdfSharpCore equivalent:
        //
        // using PdfSharpCore.Pdf;
        // using PdfSharpCore.Drawing;
        //
        // var document = new PdfDocument();
        // var page = document.AddPage();
        // var gfx = XGraphics.FromPdfPage(page);
        // var titleFont = new XFont("Helvetica", 18, XFontStyleEx.Bold);
        // gfx.DrawString("Arbeitszeitnachweis", titleFont, XBrushes.DarkBlue, new XPoint(20, 40));
        //
        // For now, export as CSV as a fallback and return that path.

        var workDays = await _database.GetWorkDaysAsync(start, end);
        var fileName = $"Arbeitszeit_{start:yyyy-MM-dd}_bis_{end:yyyy-MM-dd}.pdf";
        var filePath = Path.Combine(CacheDirectory, fileName);

        // TODO: Full PdfSharpCore implementation
        // Stub: Write a simple text file with .pdf extension as placeholder
        var sb = new StringBuilder();
        sb.AppendLine($"Arbeitszeitnachweis - {start:dd.MM.yyyy} bis {end:dd.MM.yyyy}");
        sb.AppendLine($"Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Datum;Status;Arbeitszeit;Pause;Soll;Saldo");

        int totalWork = 0, totalPause = 0, totalTarget = 0, totalBalance = 0;

        foreach (var day in workDays.OrderBy(d => d.Date))
        {
            sb.AppendLine($"{day.Date:ddd dd.MM.};{GetStatusText(day.Status)};{FormatMinutes(day.ActualWorkMinutes)};{FormatMinutes(day.ManualPauseMinutes + day.AutoPauseMinutes)};{FormatMinutes(day.TargetWorkMinutes)};{FormatBalance(day.BalanceMinutes)}");
            totalWork += day.ActualWorkMinutes;
            totalPause += day.ManualPauseMinutes + day.AutoPauseMinutes;
            totalTarget += day.TargetWorkMinutes;
            totalBalance += day.BalanceMinutes;
        }

        sb.AppendLine($"GESAMT;;{FormatMinutes(totalWork)};{FormatMinutes(totalPause)};{FormatMinutes(totalTarget)};{FormatBalance(totalBalance)}");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

        return filePath;
    }

    public async Task<string> ExportYearToPdfAsync(int year)
    {
        // TODO: Implement with PdfSharpCore (see ExportRangeToPdfAsync for pattern)
        var fileName = $"Jahresuebersicht_{year}.pdf";
        var filePath = Path.Combine(CacheDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine($"Jahresuebersicht {year}");
        sb.AppendLine();
        sb.AppendLine("Monat;Arbeitstage;Ist;Soll;Saldo");

        int yearTotalWork = 0, yearTotalTarget = 0, yearTotalBalance = 0, yearWorkDays = 0;

        for (int month = 1; month <= 12; month++)
        {
            var monthData = await _calculation.CalculateMonthAsync(year, month);
            sb.AppendLine($"{new DateTime(year, month, 1):MMMM};{monthData.WorkedDays};{FormatMinutes(monthData.ActualWorkMinutes)};{FormatMinutes(monthData.TargetWorkMinutes)};{FormatBalance(monthData.BalanceMinutes)}");
            yearTotalWork += monthData.ActualWorkMinutes;
            yearTotalTarget += monthData.TargetWorkMinutes;
            yearTotalBalance += monthData.BalanceMinutes;
            yearWorkDays += monthData.WorkedDays;
        }

        sb.AppendLine($"GESAMT;{yearWorkDays};{FormatMinutes(yearTotalWork)};{FormatMinutes(yearTotalTarget)};{FormatBalance(yearTotalBalance)}");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

        return filePath;
    }

    #endregion

    #region Excel Export

    public async Task<string> ExportMonthToExcelAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await ExportRangeToExcelAsync(start, end);
    }

    public async Task<string> ExportRangeToExcelAsync(DateTime start, DateTime end)
    {
        // TODO: Add ClosedXML NuGet package to WorkTimePro.Shared.csproj
        // The implementation below is ready to use once ClosedXML is added.
        //
        // For now, fall back to CSV format with .xlsx extension as a stub.

        var workDays = await _database.GetWorkDaysAsync(start, end);
        var fileName = $"Arbeitszeit_{start:yyyy-MM-dd}_bis_{end:yyyy-MM-dd}.xlsx";
        var filePath = Path.Combine(CacheDirectory, fileName);

        // TODO: Uncomment when ClosedXML is added to project
        /*
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Arbeitszeiten");

        // Header
        worksheet.Cell(1, 1).Value = "Datum";
        worksheet.Cell(1, 2).Value = "Status";
        worksheet.Cell(1, 3).Value = "Check-In";
        worksheet.Cell(1, 4).Value = "Check-Out";
        worksheet.Cell(1, 5).Value = "Arbeitszeit";
        worksheet.Cell(1, 6).Value = "Manuelle Pause";
        worksheet.Cell(1, 7).Value = "Auto-Pause";
        worksheet.Cell(1, 8).Value = "Soll";
        worksheet.Cell(1, 9).Value = "Saldo";

        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1565C0");
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        headerRange.Style.Font.Bold = true;

        int row = 2;
        int totalWork = 0, totalPause = 0, totalTarget = 0, totalBalance = 0;

        foreach (var day in workDays.OrderBy(d => d.Date))
        {
            var timeEntries = await _database.GetTimeEntriesAsync(day.Id);
            var firstCheckIn = timeEntries.Where(e => e.Type == EntryType.CheckIn).OrderBy(e => e.Timestamp).FirstOrDefault();
            var lastCheckOut = timeEntries.Where(e => e.Type == EntryType.CheckOut).OrderByDescending(e => e.Timestamp).FirstOrDefault();

            worksheet.Cell(row, 1).Value = day.Date.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
            worksheet.Cell(row, 2).Value = GetStatusText(day.Status);
            worksheet.Cell(row, 3).Value = firstCheckIn?.Timestamp.ToString("HH:mm") ?? "-";
            worksheet.Cell(row, 4).Value = lastCheckOut?.Timestamp.ToString("HH:mm") ?? "-";
            worksheet.Cell(row, 5).Value = FormatMinutes(day.ActualWorkMinutes);
            worksheet.Cell(row, 6).Value = FormatMinutes(day.ManualPauseMinutes);
            worksheet.Cell(row, 7).Value = day.AutoPauseMinutes > 0 ? $"{FormatMinutes(day.AutoPauseMinutes)} (auto)" : "-";
            worksheet.Cell(row, 8).Value = FormatMinutes(day.TargetWorkMinutes);
            worksheet.Cell(row, 9).Value = FormatBalance(day.BalanceMinutes);

            if (day.BalanceMinutes < 0)
                worksheet.Cell(row, 9).Style.Font.FontColor = ClosedXML.Excel.XLColor.Red;
            else if (day.BalanceMinutes > 0)
                worksheet.Cell(row, 9).Style.Font.FontColor = ClosedXML.Excel.XLColor.Green;

            totalWork += day.ActualWorkMinutes;
            totalPause += (day.ManualPauseMinutes + day.AutoPauseMinutes);
            totalTarget += day.TargetWorkMinutes;
            totalBalance += day.BalanceMinutes;
            row++;
        }

        row++;
        worksheet.Cell(row, 1).Value = "GESAMT";
        worksheet.Cell(row, 5).Value = FormatMinutes(totalWork);
        worksheet.Cell(row, 6).Value = FormatMinutes(totalPause);
        worksheet.Cell(row, 8).Value = FormatMinutes(totalTarget);
        worksheet.Cell(row, 9).Value = FormatBalance(totalBalance);

        var sumRange = worksheet.Range(row, 1, row, 9);
        sumRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        sumRange.Style.Font.Bold = true;

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        */

        // Stub: Write CSV as fallback
        return await ExportRangeToCsvAsync(start, end);
    }

    #endregion

    #region CSV Export

    public async Task<string> ExportMonthToCsvAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await ExportRangeToCsvAsync(start, end);
    }

    public async Task<string> ExportRangeToCsvAsync(DateTime start, DateTime end)
    {
        var workDays = await _database.GetWorkDaysAsync(start, end);
        var fileName = $"Arbeitszeit_{start:yyyy-MM-dd}_bis_{end:yyyy-MM-dd}.csv";
        var filePath = Path.Combine(CacheDirectory, fileName);

        var sb = new StringBuilder();

        // Header (semicolon-separated for Excel compatibility)
        sb.AppendLine("Datum;Status;Check-In;Check-Out;Arbeitszeit (min);Pause (min);Auto-Pause (min);Soll (min);Saldo (min)");

        foreach (var day in workDays.OrderBy(d => d.Date))
        {
            var timeEntries = await _database.GetTimeEntriesAsync(day.Id);
            var firstCheckIn = timeEntries.Where(e => e.Type == EntryType.CheckIn).OrderBy(e => e.Timestamp).FirstOrDefault();
            var lastCheckOut = timeEntries.Where(e => e.Type == EntryType.CheckOut).OrderByDescending(e => e.Timestamp).FirstOrDefault();

            sb.AppendLine(string.Join(";",
                day.Date.ToString("yyyy-MM-dd"),
                GetStatusText(day.Status),
                firstCheckIn?.Timestamp.ToString("HH:mm") ?? "",
                lastCheckOut?.Timestamp.ToString("HH:mm") ?? "",
                day.ActualWorkMinutes,
                day.ManualPauseMinutes,
                day.AutoPauseMinutes,
                day.TargetWorkMinutes,
                day.BalanceMinutes
            ));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    #endregion

    #region Share

    public async Task ShareFileAsync(string filePath)
    {
        // TODO: Implement platform-specific sharing for Avalonia
        // On Desktop: Could open file explorer / default application
        // On Android: Use platform-specific share intent

        // For desktop, try to open the file with the default application
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception)
        {
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var mins = Math.Abs(minutes % 60);
        return $"{hours}:{mins:D2}";
    }

    private static string FormatBalance(int minutes)
    {
        var sign = minutes >= 0 ? "+" : "";
        return sign + FormatMinutes(minutes);
    }

    private static string GetStatusText(DayStatus status)
    {
        return status switch
        {
            DayStatus.WorkDay or DayStatus.Work => AppStrings.DayStatus_WorkDay,
            DayStatus.Weekend => AppStrings.DayStatus_Weekend,
            DayStatus.Holiday => AppStrings.DayStatus_Holiday,
            DayStatus.Vacation => AppStrings.DayStatus_Vacation,
            DayStatus.Sick => AppStrings.DayStatus_Sick,
            DayStatus.HomeOffice => AppStrings.DayStatus_HomeOffice,
            DayStatus.BusinessTrip => AppStrings.DayStatus_BusinessTrip,
            DayStatus.OvertimeCompensation => AppStrings.OvertimeCompensation,
            DayStatus.SpecialLeave => AppStrings.SpecialLeave,
            DayStatus.Training => AppStrings.DayStatus_Training,
            DayStatus.CompensatoryTime => AppStrings.DayStatus_CompensatoryTime,
            DayStatus.UnpaidLeave => AppStrings.UnpaidLeave,
            _ => "-"
        };
    }

    #endregion
}
