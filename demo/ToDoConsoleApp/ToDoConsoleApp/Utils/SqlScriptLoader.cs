using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ToDoConsoleApp.Utils;

/// <summary>
/// Advanced SQL script loader supporting both embedded resources and file-based loading.
/// Provides automatic caching and SQL normalization.
/// </summary>
public class SqlScriptLoader
{
    private static readonly ConcurrentDictionary<string, string> ScriptCache = new();
    private readonly string _sqlScriptBasePath;
    private readonly bool _preferEmbedded;
    private readonly ILogger<SqlScriptLoader>? _logger;

    public SqlScriptLoader(
        string sqlScriptBasePath = "Infrastructure/Sql",
        bool preferEmbedded = true,
        ILogger<SqlScriptLoader>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(sqlScriptBasePath))
            throw new ArgumentException("SQL script base path cannot be empty", nameof(sqlScriptBasePath));

        _sqlScriptBasePath = sqlScriptBasePath;
        _preferEmbedded = preferEmbedded;
        _logger = logger;
    }

    /// <summary>
    /// Loads a SQL script from embedded resources or file system with caching.
    /// Automatically normalizes and cleans the SQL.
    /// </summary>
    public string LoadScript(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new ArgumentException("Script path cannot be empty", nameof(scriptPath));

        if (ScriptCache.TryGetValue(scriptPath, out var cachedScript))
        {
            _logger?.LogDebug("Retrieved cached script: {ScriptPath}", scriptPath);
            return cachedScript;
        }

        string? script = _preferEmbedded
            ? TryLoadEmbedded(scriptPath) ?? TryLoadFromFile(scriptPath)
            : TryLoadFromFile(scriptPath) ?? TryLoadEmbedded(scriptPath);

        if (script == null)
        {
            _logger?.LogError("SQL script not found: {ScriptPath}", scriptPath);
            throw new FileNotFoundException($"SQL script not found: {scriptPath} (embedded or file-based)");
        }

        // Normalize and cache
        script = NormalizeSql(script);
        ScriptCache[scriptPath] = script;

        _logger?.LogDebug("Loaded and cached script: {ScriptPath}", scriptPath);
        return script;
    }

    /// <summary>
    /// Attempts to load script from embedded resources.
    /// </summary>
    private string? TryLoadEmbedded(string resourcePath)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            var resourceName = $"{assemblyName}.{_sqlScriptBasePath.Replace("/", ".").Replace("\\", ".")}.{resourcePath.Replace("/", ".").Replace("\\", ".")}";

            _logger?.LogDebug("Attempting to load embedded resource: {ResourceName}", resourceName);

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    _logger?.LogDebug("Embedded resource not found: {ResourceName}", resourceName);
                    return null;
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var content = reader.ReadToEnd();
                    _logger?.LogDebug("Loaded embedded resource: {ResourceName}", resourceName);
                    return content;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error loading embedded resource: {ResourcePath}", resourcePath);
            return null;
        }
    }

    /// <summary>
    /// Attempts to load script from file system.
    /// </summary>
    private string? TryLoadFromFile(string scriptPath)
    {
        try
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, _sqlScriptBasePath, scriptPath);

            _logger?.LogDebug("Attempting to load script from file: {FilePath}", fullPath);

            if (!File.Exists(fullPath))
            {
                _logger?.LogDebug("Script file not found: {FilePath}", fullPath);
                return null;
            }

            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            _logger?.LogDebug("Loaded script from file: {FilePath}", fullPath);
            return content;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error loading script from file: {ScriptPath}", scriptPath);
            return null;
        }
    }

    /// <summary>
    /// Normalizes SQL by removing comments and excess whitespace.
    /// </summary>
    private static string NormalizeSql(string sql)
    {
        // Remove line comments (-- comment)
        sql = Regex.Replace(sql, @"--[^\r\n]*", "");

        // Remove block comments (/* comment */)
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Replace multiple spaces/tabs/newlines with single space
        sql = Regex.Replace(sql, @"\s+", " ");

        return sql.Trim();
    }

    /// <summary>
    /// Clears the entire script cache.
    /// Useful for testing or forcing reload.
    /// </summary>
    public void ClearCache()
    {
        ScriptCache.Clear();
        _logger?.LogInformation("Script cache cleared");
    }

    /// <summary>
    /// Gets cache statistics for diagnostics.
    /// </summary>
    public (int CachedScripts, long ApproximateMemoryBytes) GetCacheStats()
    {
        var memoryBytes = ScriptCache.Values.Sum(s => s.Length * sizeof(char));
        return (ScriptCache.Count, memoryBytes);
    }
}