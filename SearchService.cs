using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
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

        // 3. Windows Search Index
        try
        {
            using var connection = new OleDbConnection(
                "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';");

            connection.Open();

            using var command = new OleDbCommand(
                @"SELECT TOP 10 System.ItemNameDisplay, System.ItemPathDisplay
                  FROM SystemIndex
                  WHERE (System.ItemName LIKE ? OR System.ItemNameDisplay LIKE ?)
                  AND System.ItemPathDisplay NOT LIKE '%\Packages\%'
                  AND System.ItemPathDisplay NOT LIKE '%\AppData\Local\Microsoft\%'",
                connection);

            command.Parameters.AddWithValue("@p1", query + "%");
            command.Parameters.AddWithValue("@p2", query + "%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var path = reader[1]?.ToString();
                if (path == null || results.Any(x => x.Path == path)) continue;

                results.Add(new AppItem
                {
                    Name = reader[0]?.ToString(),
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
