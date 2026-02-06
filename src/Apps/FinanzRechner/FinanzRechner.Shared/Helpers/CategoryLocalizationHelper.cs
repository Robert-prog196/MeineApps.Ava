using FinanzRechner.Models;
using MeineApps.Core.Ava.Localization;

namespace FinanzRechner.Helpers;

/// <summary>
/// Centralized helper for localized category names
/// </summary>
public static class CategoryLocalizationHelper
{
    public static string GetCategoryKey(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Food => "CategoryFood",
        ExpenseCategory.Transport => "CategoryTransport",
        ExpenseCategory.Housing => "CategoryHousing",
        ExpenseCategory.Entertainment => "CategoryEntertainment",
        ExpenseCategory.Shopping => "CategoryShopping",
        ExpenseCategory.Health => "CategoryHealth",
        ExpenseCategory.Education => "CategoryEducation",
        ExpenseCategory.Bills => "CategoryBills",
        ExpenseCategory.Other => "CategoryOther",
        ExpenseCategory.Salary => "CategorySalary",
        ExpenseCategory.Freelance => "CategoryFreelance",
        ExpenseCategory.Investment => "CategoryInvestment",
        ExpenseCategory.Gift => "CategoryGift",
        ExpenseCategory.OtherIncome => "CategoryOtherIncome",
        _ => "CategoryOther"
    };

    public static string GetLocalizedName(ExpenseCategory category, ILocalizationService? localizationService)
    {
        if (localizationService == null)
            return GetFallbackName(category);

        var key = GetCategoryKey(category);
        return localizationService.GetString(key) ?? GetFallbackName(category);
    }

    private static string GetFallbackName(ExpenseCategory category)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        return category switch
        {
            ExpenseCategory.Food => culture switch
            {
                "de" => "Lebensmittel", "es" => "Comida", "fr" => "Nourriture",
                "it" => "Cibo", "pt" => "Comida", _ => "Food"
            },
            ExpenseCategory.Transport => culture switch
            {
                "de" => "Transport", "es" => "Transporte", "fr" => "Transport",
                "it" => "Trasporto", "pt" => "Transporte", _ => "Transport"
            },
            ExpenseCategory.Housing => culture switch
            {
                "de" => "Wohnen", "es" => "Vivienda", "fr" => "Logement",
                "it" => "Casa", "pt" => "Moradia", _ => "Housing"
            },
            ExpenseCategory.Entertainment => culture switch
            {
                "de" => "Unterhaltung", "es" => "Entretenimiento", "fr" => "Divertissement",
                "it" => "Intrattenimento", "pt" => "Entretenimento", _ => "Entertainment"
            },
            ExpenseCategory.Shopping => culture switch
            {
                "de" => "Einkaufen", "es" => "Compras", "fr" => "Achats",
                "it" => "Acquisti", "pt" => "Compras", _ => "Shopping"
            },
            ExpenseCategory.Health => culture switch
            {
                "de" => "Gesundheit", "es" => "Salud", "fr" => "Sant\u00e9",
                "it" => "Salute", "pt" => "Sa\u00fade", _ => "Health"
            },
            ExpenseCategory.Education => culture switch
            {
                "de" => "Bildung", "es" => "Educaci\u00f3n", "fr" => "\u00c9ducation",
                "it" => "Educazione", "pt" => "Educa\u00e7\u00e3o", _ => "Education"
            },
            ExpenseCategory.Bills => culture switch
            {
                "de" => "Rechnungen", "es" => "Facturas", "fr" => "Factures",
                "it" => "Bollette", "pt" => "Contas", _ => "Bills"
            },
            ExpenseCategory.Other => culture switch
            {
                "de" => "Sonstiges", "es" => "Otros", "fr" => "Autres",
                "it" => "Altro", "pt" => "Outros", _ => "Other"
            },
            ExpenseCategory.Salary => culture switch
            {
                "de" => "Gehalt", "es" => "Salario", "fr" => "Salaire",
                "it" => "Stipendio", "pt" => "Sal\u00e1rio", _ => "Salary"
            },
            ExpenseCategory.Freelance => culture switch
            {
                "de" => "Freiberuflich", "es" => "Aut\u00f3nomo", "fr" => "Freelance",
                "it" => "Freelance", "pt" => "Freelancer", _ => "Freelance"
            },
            ExpenseCategory.Investment => culture switch
            {
                "de" => "Kapitalertr\u00e4ge", "es" => "Inversiones", "fr" => "Investissement",
                "it" => "Investimento", "pt" => "Investimento", _ => "Investment"
            },
            ExpenseCategory.Gift => culture switch
            {
                "de" => "Geschenk", "es" => "Regalo", "fr" => "Cadeau",
                "it" => "Regalo", "pt" => "Presente", _ => "Gift"
            },
            ExpenseCategory.OtherIncome => culture switch
            {
                "de" => "Sonstiges Einkommen", "es" => "Otros Ingresos", "fr" => "Autres Revenus",
                "it" => "Altre Entrate", "pt" => "Outras Receitas", _ => "Other Income"
            },
            _ => category.ToString()
        };
    }
}
