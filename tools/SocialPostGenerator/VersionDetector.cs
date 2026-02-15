using System.Xml.Linq;

namespace SocialPostGenerator;

/// <summary>
/// Liest App-Versionen aus den Android .csproj Dateien
/// </summary>
static class VersionDetector
{
    /// <summary>
    /// Liest ApplicationDisplayVersion aus der Android-csproj einer App
    /// </summary>
    public static string GetVersion(string solutionRoot, string appName)
    {
        var csprojPath = Path.Combine(solutionRoot, "src", "Apps", appName,
            $"{appName}.Android", $"{appName}.Android.csproj");

        if (!File.Exists(csprojPath))
            return "?.?.?";

        try
        {
            var doc = XDocument.Load(csprojPath);
            return doc.Descendants("ApplicationDisplayVersion").FirstOrDefault()?.Value ?? "?.?.?";
        }
        catch
        {
            return "?.?.?";
        }
    }

    /// <summary>
    /// Liest Versionen aller Apps
    /// </summary>
    public static Dictionary<string, string> GetAllVersions(string solutionRoot, string[] appNames)
    {
        var result = new Dictionary<string, string>();
        foreach (var name in appNames)
            result[name] = GetVersion(solutionRoot, name);
        return result;
    }
}
