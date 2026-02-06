using System.Text.Json;
using FinanzRechner.Helpers;
using FinanzRechner.Models;
using MeineApps.Core.Ava.Localization;

namespace FinanzRechner.Services;

/// <summary>
/// Implementation of ExpenseService with local JSON storage
/// </summary>
public class ExpenseService : IExpenseService, IDisposable
{
    private const string ExpensesFile = "expenses.json";
    private const string BudgetsFile = "budgets.json";
    private const string RecurringFile = "recurring_transactions.json";
    private const double MaxAmount = 999_999_999.99;
    private const int MaxDescriptionLength = 200;
    private const int MaxNoteLength = 500;

    private readonly string _expensesFilePath;
    private readonly string _budgetsFilePath;
    private readonly string _recurringFilePath;
    private readonly INotificationService? _notificationService;
    private readonly ILocalizationService? _localizationService;
    private List<Expense> _expenses = [];
    private List<Budget> _budgets = [];
    private List<RecurringTransaction> _recurringTransactions = [];
    private readonly Dictionary<string, DateTime> _sentNotifications = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isInitialized;

    public ExpenseService(INotificationService? notificationService = null,
        ILocalizationService? localizationService = null)
    {
        _notificationService = notificationService;
        _localizationService = localizationService;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FinanzRechner");
        Directory.CreateDirectory(appDataDir);

        _expensesFilePath = Path.Combine(appDataDir, ExpensesFile);
        _budgetsFilePath = Path.Combine(appDataDir, BudgetsFile);
        _recurringFilePath = Path.Combine(appDataDir, RecurringFile);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized) return;
            await LoadExpensesAsync();
            await LoadBudgetsAsync();
            await LoadRecurringTransactionsAsync();
            _isInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Expense>> GetAllExpensesAsync()
    {
        await InitializeAsync();
        return _expenses.OrderByDescending(e => e.Date).ToList();
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesByMonthAsync(int year, int month)
    {
        await InitializeAsync();
        return _expenses
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .OrderByDescending(e => e.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(ExpenseFilter filter)
    {
        await InitializeAsync();
        var query = _expenses.AsEnumerable();

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.Date >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(e => e.Date <= filter.EndDate.Value);
        if (filter.Category.HasValue)
            query = query.Where(e => e.Category == filter.Category.Value);
        if (filter.MinAmount.HasValue)
            query = query.Where(e => e.Amount >= filter.MinAmount.Value);
        if (filter.MaxAmount.HasValue)
            query = query.Where(e => e.Amount <= filter.MaxAmount.Value);

        return query.OrderByDescending(e => e.Date).ToList();
    }

    public async Task<Expense?> GetExpenseAsync(string id)
    {
        await InitializeAsync();
        return _expenses.FirstOrDefault(e => e.Id == id);
    }

    public async Task<Expense> AddExpenseAsync(Expense expense)
    {
        await InitializeAsync();
        ValidateExpense(expense);

        await _semaphore.WaitAsync();
        try
        {
            expense.Id = Guid.NewGuid().ToString();
            _expenses.Add(expense);
            await SaveExpensesAsync();
        }
        finally
        {
            _semaphore.Release();
        }

        if (expense.Type == TransactionType.Expense)
            _ = CheckBudgetWarningAsync(expense.Category, expense.Date);

        return expense;
    }

    public async Task<bool> UpdateExpenseAsync(Expense expense)
    {
        await InitializeAsync();
        ValidateExpense(expense);

        await _semaphore.WaitAsync();
        try
        {
            var existing = _expenses.FirstOrDefault(e => e.Id == expense.Id);
            if (existing == null) return false;

            existing.Date = expense.Date;
            existing.Description = expense.Description;
            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.Note = expense.Note;
            existing.Type = expense.Type;

            await SaveExpensesAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteExpenseAsync(string id)
    {
        await InitializeAsync();

        await _semaphore.WaitAsync();
        try
        {
            var expense = _expenses.FirstOrDefault(e => e.Id == id);
            if (expense == null) return false;

            _expenses.Remove(expense);
            await SaveExpensesAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<MonthSummary> GetMonthSummaryAsync(int year, int month)
    {
        var transactions = await GetExpensesByMonthAsync(year, month);

        var totalExpenses = transactions
            .Where(e => e.Type == TransactionType.Expense).Sum(e => e.Amount);
        var totalIncome = transactions
            .Where(e => e.Type == TransactionType.Income).Sum(e => e.Amount);
        var balance = totalIncome - totalExpenses;
        var byCategory = transactions
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return new MonthSummary(year, month, totalExpenses, totalIncome, balance, byCategory);
    }

    public async Task<double> GetTotalExpensesAsync(DateTime startDate, DateTime endDate)
    {
        var filter = new ExpenseFilter { StartDate = startDate, EndDate = endDate };
        var expenses = await GetExpensesAsync(filter);
        return expenses.Sum(e => e.Amount);
    }

    public async Task ClearAllExpensesAsync()
    {
        _expenses.Clear();
        await SaveExpensesAsync();
    }

    #region Budget Management

    public async Task<Budget> SetBudgetAsync(Budget budget)
    {
        await InitializeAsync();
        ValidateBudget(budget);

        await _semaphore.WaitAsync();
        try
        {
            var existing = _budgets.FirstOrDefault(b => b.Category == budget.Category);
            if (existing != null)
            {
                existing.MonthlyLimit = budget.MonthlyLimit;
                existing.IsEnabled = budget.IsEnabled;
                existing.WarningThreshold = budget.WarningThreshold;
            }
            else
            {
                budget.Id = Guid.NewGuid().ToString();
                _budgets.Add(budget);
            }

            await SaveBudgetsAsync();
            return budget;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Budget?> GetBudgetAsync(ExpenseCategory category)
    {
        await InitializeAsync();
        return _budgets.FirstOrDefault(b => b.Category == category);
    }

    public async Task<IReadOnlyList<Budget>> GetAllBudgetsAsync()
    {
        await InitializeAsync();
        return _budgets.Where(b => b.IsEnabled).ToList();
    }

    public async Task<bool> DeleteBudgetAsync(ExpenseCategory category)
    {
        await InitializeAsync();
        await _semaphore.WaitAsync();
        try
        {
            var budget = _budgets.FirstOrDefault(b => b.Category == category);
            if (budget == null) return false;
            _budgets.Remove(budget);
            await SaveBudgetsAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<BudgetStatus?> GetBudgetStatusAsync(ExpenseCategory category)
    {
        var budget = await GetBudgetAsync(category);
        if (budget == null || !budget.IsEnabled) return null;

        var now = DateTime.Now;
        var expenses = await GetExpensesByMonthAsync(now.Year, now.Month);
        var spent = expenses
            .Where(e => e.Category == category && e.Type == TransactionType.Expense)
            .Sum(e => e.Amount);

        var remaining = budget.MonthlyLimit - spent;
        var percentageUsed = budget.MonthlyLimit > 0 ? (spent / budget.MonthlyLimit) * 100 : 0;
        var alertLevel = percentageUsed >= 100 ? BudgetAlertLevel.Exceeded :
                        percentageUsed >= budget.WarningThreshold ? BudgetAlertLevel.Warning :
                        BudgetAlertLevel.Safe;

        return new BudgetStatus(category, budget.MonthlyLimit, spent, remaining, percentageUsed, alertLevel);
    }

    public async Task<IReadOnlyList<BudgetStatus>> GetAllBudgetStatusAsync()
    {
        await InitializeAsync();
        var statusList = new List<BudgetStatus>();
        foreach (var budget in _budgets.Where(b => b.IsEnabled))
        {
            var status = await GetBudgetStatusAsync(budget.Category);
            if (status != null) statusList.Add(status);
        }
        return statusList;
    }

    #endregion

    #region Recurring Transactions

    public async Task<RecurringTransaction> CreateRecurringTransactionAsync(RecurringTransaction transaction)
    {
        ValidateRecurringTransaction(transaction);
        await _semaphore.WaitAsync();
        try
        {
            if (transaction.Id == Guid.Empty) transaction.Id = Guid.NewGuid();
            _recurringTransactions.Add(transaction);
            await SaveRecurringTransactionsAsync();
            return transaction;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> UpdateRecurringTransactionAsync(RecurringTransaction transaction)
    {
        ValidateRecurringTransaction(transaction);
        await _semaphore.WaitAsync();
        try
        {
            var existing = _recurringTransactions.FirstOrDefault(r => r.Id == transaction.Id);
            if (existing == null) return false;
            var index = _recurringTransactions.IndexOf(existing);
            _recurringTransactions[index] = transaction;
            await SaveRecurringTransactionsAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteRecurringTransactionAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var transaction = _recurringTransactions.FirstOrDefault(r => r.Id == id);
            if (transaction == null) return false;
            _recurringTransactions.Remove(transaction);
            await SaveRecurringTransactionsAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<RecurringTransaction?> GetRecurringTransactionAsync(Guid id) =>
        Task.FromResult(_recurringTransactions.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<RecurringTransaction>> GetAllRecurringTransactionsAsync() =>
        Task.FromResult<IReadOnlyList<RecurringTransaction>>(
            _recurringTransactions.OrderBy(r => r.Description).ToList());

    public async Task<int> ProcessDueRecurringTransactionsAsync()
    {
        var today = DateTime.Today;
        var count = 0;
        foreach (var recurring in _recurringTransactions.Where(r => r.IsActive))
        {
            if (!recurring.IsDue(today)) continue;
            var expense = recurring.CreateExpense(today);
            await AddExpenseAsync(expense);
            recurring.LastExecuted = today;
            count++;
        }
        if (count > 0) await SaveRecurringTransactionsAsync();
        return count;
    }

    #endregion

    #region Backup & Restore

    private record BackupData(
        string Version, DateTime CreatedAt,
        List<Expense> Expenses, List<Budget> Budgets,
        List<RecurringTransaction> RecurringTransactions);

    public async Task<string> ExportToJsonAsync()
    {
        await InitializeAsync();
        await _semaphore.WaitAsync();
        try
        {
            var backup = new BackupData("1.0", DateTime.Now,
                _expenses.ToList(), _budgets.ToList(), _recurringTransactions.ToList());
            return JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> ImportFromJsonAsync(string json, bool mergeData = false)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be empty.", nameof(json));

        await InitializeAsync();
        await _semaphore.WaitAsync();
        try
        {
            var backup = JsonSerializer.Deserialize<BackupData>(json);
            if (backup == null) throw new InvalidOperationException("Invalid backup data format.");

            int importedCount = 0;
            if (!mergeData)
            {
                _expenses.Clear();
                _budgets.Clear();
                _recurringTransactions.Clear();
            }

            if (backup.Expenses != null)
            {
                foreach (var expense in backup.Expenses)
                {
                    if (mergeData && _expenses.Any(e => e.Id == expense.Id)) continue;
                    _expenses.Add(expense);
                    importedCount++;
                }
            }

            if (backup.Budgets != null)
            {
                foreach (var budget in backup.Budgets)
                {
                    if (mergeData)
                    {
                        var existing = _budgets.FirstOrDefault(b => b.Category == budget.Category);
                        if (existing != null) _budgets.Remove(existing);
                    }
                    _budgets.Add(budget);
                }
            }

            if (backup.RecurringTransactions != null)
            {
                foreach (var recurring in backup.RecurringTransactions)
                {
                    if (mergeData && _recurringTransactions.Any(r => r.Id == recurring.Id)) continue;
                    _recurringTransactions.Add(recurring);
                }
            }

            await SaveExpensesAsync();
            await SaveBudgetsAsync();
            await SaveRecurringTransactionsAsync();

            return importedCount;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Private Helpers

    private async Task LoadExpensesAsync()
    {
        try
        {
            if (File.Exists(_expensesFilePath))
            {
                var json = await File.ReadAllTextAsync(_expensesFilePath);
                _expenses = JsonSerializer.Deserialize<List<Expense>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            // Silently handle load error - will use empty collection
            _expenses = [];
        }
    }

    private async Task LoadBudgetsAsync()
    {
        try
        {
            if (File.Exists(_budgetsFilePath))
            {
                var json = await File.ReadAllTextAsync(_budgetsFilePath);
                _budgets = JsonSerializer.Deserialize<List<Budget>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            // Silently handle load error - will use empty collection
            _budgets = [];
        }
    }

    private async Task LoadRecurringTransactionsAsync()
    {
        try
        {
            if (File.Exists(_recurringFilePath))
            {
                var json = await File.ReadAllTextAsync(_recurringFilePath);
                _recurringTransactions = JsonSerializer.Deserialize<List<RecurringTransaction>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            // Silently handle load error - will use empty collection
            _recurringTransactions = [];
        }
    }

    private async Task SaveExpensesAsync()
    {
        var json = JsonSerializer.Serialize(_expenses, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_expensesFilePath, json);
    }

    private async Task SaveBudgetsAsync()
    {
        var json = JsonSerializer.Serialize(_budgets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_budgetsFilePath, json);
    }

    private async Task SaveRecurringTransactionsAsync()
    {
        var json = JsonSerializer.Serialize(_recurringTransactions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_recurringFilePath, json);
    }

    private static void ValidateExpense(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        if (string.IsNullOrWhiteSpace(expense.Description))
            throw new ArgumentException("Description cannot be empty.");
        if (expense.Description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.");
        if (expense.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (expense.Amount > MaxAmount)
            throw new ArgumentException($"Amount cannot exceed {MaxAmount:N2}.");
        if (!string.IsNullOrEmpty(expense.Note) && expense.Note.Length > MaxNoteLength)
            throw new ArgumentException($"Note cannot exceed {MaxNoteLength} characters.");
    }

    private static void ValidateBudget(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (budget.MonthlyLimit <= 0)
            throw new ArgumentException("Monthly limit must be greater than zero.");
        if (budget.MonthlyLimit > MaxAmount)
            throw new ArgumentException($"Monthly limit cannot exceed {MaxAmount:N2}.");
        if (budget.WarningThreshold is < 0 or > 100)
            throw new ArgumentException("Warning threshold must be between 0 and 100.");
    }

    private static void ValidateRecurringTransaction(RecurringTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (string.IsNullOrWhiteSpace(transaction.Description))
            throw new ArgumentException("Description cannot be empty.");
        if (transaction.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (transaction.EndDate.HasValue && transaction.EndDate.Value <= transaction.StartDate)
            throw new ArgumentException("End date must be after start date.");
    }

    private async Task CheckBudgetWarningAsync(ExpenseCategory category, DateTime date)
    {
        if (_notificationService == null) return;
        try
        {
            var budget = _budgets.FirstOrDefault(b => b.Category == category && b.IsEnabled);
            if (budget == null) return;

            var startOfMonth = new DateTime(date.Year, date.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var spent = _expenses
                .Where(e => e.Category == category && e.Type == TransactionType.Expense
                    && e.Date >= startOfMonth && e.Date <= endOfMonth)
                .Sum(e => e.Amount);
            var percentageUsed = budget.MonthlyLimit > 0 ? (spent / budget.MonthlyLimit * 100) : 0;
            var notificationKey = $"{category}_{date:yyyy-MM}";

            if (percentageUsed >= 80 && !_sentNotifications.ContainsKey($"{notificationKey}_80"))
            {
                var categoryName = CategoryLocalizationHelper.GetLocalizedName(category, _localizationService);
                await _notificationService.SendBudgetAlertAsync(categoryName, percentageUsed, spent, budget.MonthlyLimit);
                _sentNotifications[$"{notificationKey}_80"] = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            // Silently handle budget warning check failure
        }
    }

    #endregion

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
