namespace FinanzRechner.Services;

public interface IFileDialogService
{
    Task<string?> SaveFileAsync(string suggestedFileName, string title, string filterName, params string[] extensions);
}
