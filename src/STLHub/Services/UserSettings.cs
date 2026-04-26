using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STLHub.Services;

/// <summary>
/// Persisted user settings stored as JSON in the application data folder.
/// Includes window state, theme, view preferences, and recent repositories.
/// </summary>
public class UserSettings
{
    public double WindowX { get; set; } = double.NaN;
    public double WindowY { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public bool IsMaximized { get; set; }
    public double SidebarWidth { get; set; } = 250;
    public string ViewSize { get; set; } = "Medium";
    public string SortOrder { get; set; } = "DateDesc";
    public string Theme { get; set; } = "Dark";
    public string LastRepositoryPath { get; set; } = string.Empty;
    public List<string> RecentRepositories { get; set; } = new();

    public void AddRecentRepository(string path)
    {
        RecentRepositories.Remove(path);
        RecentRepositories.Insert(0, path);
        if (RecentRepositories.Count > 10)
            RecentRepositories = RecentRepositories.Take(10).ToList();
        LastRepositoryPath = path;
    }

    private static string GetSettingsPath()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "STLHub");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "settings.json");
    }

    public static UserSettings Load()
    {
        try
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch { }
        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            string path = GetSettingsPath();
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(this, options));
        }
        catch { }
    }
}
