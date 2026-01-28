using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class SearchService
{
    private readonly List<AppItem> _startMenuApps;

    public SearchService()
    {
        _startMenuApps = LoadStartMenuApps();
    }

    private List<AppItem> LoadStartMenuApps()
    {
        var results = new List<AppItem>();

        string[] locations =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var path in locations)
        {
            try
            {
                if (!Directory.Exists(path)) continue;

                foreach (var file in Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories))
                {
                    results.Add(new AppItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Type = "Start Menu",
                        Icon = IconService.GetIcon(file)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        return results;
    }

    public List<AppItem> Search(string query)
    {
        var results = new List<AppItem>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        query = query.Trim();

        // 1. Registry lookup
        var regPath = GetPathFromRegistry(query);
        if (regPath != null)
        {
            results.Add(new AppItem
            {
                Name = query.ToUpper(),
                Path = regPath,
                Type = "System Command",
                Icon = IconService.GetIcon(regPath)
            });
        }

        // 2. Start menu
        results.AddRange(
            _startMenuApps
                .Where(a => a.Name!.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
        );

        // 3. Search the index
        results.AddRange(SearchFromIndex(query, results));


        return results;
    }

    private IEnumerable<AppItem> SearchFromIndex(string query, List<AppItem> existingResults)
    {
        var results = new List<AppItem>();
        try
        {
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Name, Path, Type
                FROM Files
                WHERE Name LIKE @Query OR Path LIKE @Query
                LIMIT 20";
            command.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var path = reader["Path"]?.ToString();
                if (path == null || existingResults.Any(x => x.Path == path)) continue;

                results.Add(new AppItem
                {
                    Name = reader["Name"]?.ToString(),
                    Path = path,
                    Type = "File",
                    Icon = IconService.GetIcon(path)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return results;
    }

    private string? GetPathFromRegistry(string command)
    {
        string[] keys =
        {
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{command}.exe",
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{command}"
        };

        foreach (var keyPath in keys)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath)
                         ?? Registry.CurrentUser.OpenSubKey(keyPath);

            var value = key?.GetValue("");
            if (value != null) return value.ToString();
        }

        return null;
    }
}
