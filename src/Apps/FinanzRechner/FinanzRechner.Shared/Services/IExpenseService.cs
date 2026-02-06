using FinanzRechner.Models;

namespace FinanzRechner.Services;

/// <summary>
/// Service for expense management (CRUD + Statistics)
/// </summary>
public interface IExpenseService
{
    Task InitializeAsync();
    Task<IReadOnlyList<Expense>> GetAllExpensesAsync();
    Task<IReadOnlyList<Expense>> GetExpensesByMonthAsync(int year, int month);
    Task<IReadOnlyList<Expense>> GetExpensesAsync(ExpenseFilter filter);
    Task<Expense?> GetExpenseAsync(string id);
    Task<Expense> AddExpenseAsync(Expense expense);
    Task<bool> UpdateExpenseAsync(Expense expense);
    Task<bool> DeleteExpenseAsync(string id);
    Task<MonthSummary> GetMonthSummaryAsync(int year, int month);
    Task<double> GetTotalExpensesAsync(DateTime startDate, DateTime endDate);
    Task ClearAllExpensesAsync();

    // Budget Management
    Task<Budget> SetBudgetAsync(Budget budget);
    Task<Budget?> GetBudgetAsync(ExpenseCategory category);
    Task<IReadOnlyList<Budget>> GetAllBudgetsAsync();
    Task<bool> DeleteBudgetAsync(ExpenseCategory category);
    Task<BudgetStatus?> GetBudgetStatusAsync(ExpenseCategory category);
    Task<IReadOnlyList<BudgetStatus>> GetAllBudgetStatusAsync();

    // Recurring Transactions
    Task<RecurringTransaction> CreateRecurringTransactionAsync(RecurringTransaction transaction);
    Task<bool> UpdateRecurringTransactionAsync(RecurringTransaction transaction);
    Task<bool> DeleteRecurringTransactionAsync(Guid id);
    Task<RecurringTransaction?> GetRecurringTransactionAsync(Guid id);
    Task<IReadOnlyList<RecurringTransaction>> GetAllRecurringTransactionsAsync();
    Task<int> ProcessDueRecurringTransactionsAsync();

    // Backup & Restore
    Task<string> ExportToJsonAsync();
    Task<int> ImportFromJsonAsync(string json, bool mergeData = false);
}
