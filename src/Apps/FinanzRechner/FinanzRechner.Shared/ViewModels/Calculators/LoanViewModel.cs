using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanzRechner.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MeineApps.Core.Ava.Localization;
using SkiaSharp;

namespace FinanzRechner.ViewModels.Calculators;

public partial class LoanViewModel : ObservableObject
{
    private readonly FinanceEngine _financeEngine;
    private readonly ILocalizationService _localizationService;

    public LoanViewModel(FinanceEngine financeEngine, ILocalizationService localizationService)
    {
        _financeEngine = financeEngine;
        _localizationService = localizationService;
    }

    public Action? GoBackAction { get; set; }

    #region Localized Text Properties

    public string TitleText => _localizationService.GetString("CalcLoan") ?? "Loan";
    public string LoanAmountText => _localizationService.GetString("LoanAmount") ?? "Loan Amount (EUR)";
    public string AnnualRateText => _localizationService.GetString("AnnualRate") ?? "Annual Rate (%)";
    public string YearsText => _localizationService.GetString("Years") ?? "Years";
    public string ResultText => _localizationService.GetString("Result") ?? "Result";
    public string MonthlyPaymentText => _localizationService.GetString("MonthlyPayment") ?? "Monthly Payment";
    public string TotalPaymentText => _localizationService.GetString("TotalPayment") ?? "Total Payment";
    public string TotalInterestText => _localizationService.GetString("TotalInterest") ?? "Total Interest";
    public string CostBreakdownText => _localizationService.GetString("CostBreakdown") ?? "Cost Breakdown";
    public string PrincipalPortionText => _localizationService.GetString("PrincipalPortion") ?? "Principal";
    public string InterestPortionText => _localizationService.GetString("InterestPortion") ?? "Interest";
    public string ResetText => _localizationService.GetString("Reset") ?? "Reset";
    public string CalculateText => _localizationService.GetString("Calculate") ?? "Calculate";

    #endregion

    #region Input Properties

    [ObservableProperty]
    private double _loanAmount = 100000;

    [ObservableProperty]
    private double _annualRate = 4;

    [ObservableProperty]
    private int _years = 20;

    #endregion

    #region Result Properties

    [ObservableProperty]
    private LoanResult? _result;

    [ObservableProperty]
    private bool _hasResult;

    public string MonthlyPaymentDisplay => Result != null ? $"{Result.MonthlyPayment:N2} \u20ac" : "";
    public string TotalPaymentDisplay => Result != null ? $"{Result.TotalPayment:N2} \u20ac" : "";
    public string TotalInterestDisplay => Result != null ? $"{Result.TotalInterest:N2} \u20ac" : "";

    partial void OnResultChanged(LoanResult? value)
    {
        OnPropertyChanged(nameof(MonthlyPaymentDisplay));
        OnPropertyChanged(nameof(TotalPaymentDisplay));
        OnPropertyChanged(nameof(TotalInterestDisplay));
        UpdateChartData();
    }

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    private void UpdateChartData()
    {
        if (Result == null)
        {
            ChartSeries = Array.Empty<ISeries>();
            return;
        }

        ChartSeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Values = new[] { Result.LoanAmount },
                Name = _localizationService.GetString("ChartRepayment") ?? "Repayment",
                Fill = new SolidColorPaint(SKColor.Parse("#4CAF50")),
                Pushout = 5
            },
            new PieSeries<double>
            {
                Values = new[] { Result.TotalInterest },
                Name = _localizationService.GetString("ChartInterest") ?? "Interest",
                Fill = new SolidColorPaint(SKColor.Parse("#FF9800"))
            }
        };
    }

    #endregion

    [RelayCommand]
    private void Calculate()
    {
        if (LoanAmount <= 0 || Years <= 0)
        {
            HasResult = false;
            return;
        }

        Result = _financeEngine.CalculateLoan(LoanAmount, AnnualRate, Years);
        HasResult = true;
    }

    [RelayCommand]
    private void Reset()
    {
        LoanAmount = 100000;
        AnnualRate = 4;
        Years = 20;
        Result = null;
        HasResult = false;
        ChartSeries = Array.Empty<ISeries>();
    }

    [RelayCommand]
    private void GoBack() => GoBackAction?.Invoke();
}
