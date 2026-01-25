using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;       
using System.Drawing;               
using System.Data.OleDb;
using Microsoft.Win32;

namespace OmniSearchApp
{

    
    public class AppItem
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Type { get; set; }
        public ImageSource? Icon { get; set; } 
    }

    public partial class MainWindow : Window
    {
        private List<AppItem> startMenuApps = new List<AppItem>();
        private System.Windows.Threading.DispatcherTimer _searchTimer;

        public MainWindow()
        {
            // InitializeComponent();
            // LoadStartMenuApps();
            InitializeComponent();
             LoadStartMenuApps();

    // Initialize the timer
    _searchTimer = new System.Windows.Threading.DispatcherTimer();
    _searchTimer.Interval = TimeSpan.FromMilliseconds(300); // Wait 300ms
    _searchTimer.Tick += SearchTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowBlur.EnableBlur(this);
            this.Activate();
            SearchInput.Focus();
        }

        private void LoadStartMenuApps()
        {
            string[] locations = {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
            };

            foreach (var path in locations)
            {
                if (Directory.Exists(path))
                {
                    try {
                        var files = Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories);
                        foreach (var file in files) {
                            startMenuApps.Add(new AppItem { 
                                Name = Path.GetFileNameWithoutExtension(file), 
                                Path = file,
                                Type = "Start Menu",
                                Icon = GetFileIcon(file) // Load icons for Start Menu
                            });
                        }
                    } catch { }
                }
            }
        }

        private string? GetPathFromRegistry(string command)
        {
            string[] keys = {
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{command}.exe",
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{command}"
            };

            foreach (var keyPath in keys)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath) ?? Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(""); 
                        if (value != null) return value.ToString();
                    }
                }
            }
            return null;
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
{

    // Every time the user types, stop the timer and start it again
    _searchTimer.Stop();
    _searchTimer.Start();

    string query = SearchInput.Text.Trim();
    if (string.IsNullOrWhiteSpace(query)) {
        ResultsBorder.Visibility = Visibility.Collapsed;
        return;
    }

    List<AppItem> searchResults = new List<AppItem>();

    // 1. REGISTRY CHECK (The "Win+R" Logic)
    string? regPath = GetPathFromRegistry(query.ToLower());
    if (regPath != null) {
        searchResults.Add(new AppItem { 
            Name = query.ToUpper(), 
            Path = regPath, 
            Type = "System Command",
            Icon = GetFileIcon(regPath) 
        });
    }

    // 2. MANUAL START MENU SCAN (Safety Net for Spotify)
    // Sometimes the indexer ignores .lnk files for UWP apps
    var menuMatches = startMenuApps
        .Where(a => a.Name!.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Take(5);
    searchResults.AddRange(menuMatches);

    // 3. WINDOWS INDEXER (Deep Search)
    try {
        string connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";
        using (OleDbConnection connection = new OleDbConnection(connectionString)) {
            connection.Open();
            // Search for ItemNameDisplay which is usually "Spotify" instead of the long package name
            string sql = $@"SELECT TOP 10 System.ItemNameDisplay, System.ItemPathDisplay 
                           FROM SystemIndex 
                           WHERE (System.ItemName LIKE '{query}%' OR System.ItemNameDisplay LIKE '{query}%')
                           AND System.ItemPathDisplay NOT LIKE '%\Packages\%' 
                           AND System.ItemPathDisplay NOT LIKE '%\AppData\Local\Microsoft\%'";

            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    string? name = reader[0]?.ToString();
                    string? path = reader[1]?.ToString();
                    
                    // Only add if it's not already there and NOT a messy package folder
                    if (path != null && !searchResults.Any(x => x.Path == path)) {
                        searchResults.Add(new AppItem { 
                            Name = name, 
                            Path = path,
                            Type = "File",
                            Icon = GetFileIcon(path)
                        });
                    }
                }
            }
        }
    } catch { }

    // 4. THE "IF ALL ELSE FAILS" SPOTIFY FIX
    if (query.ToLower().Contains("spot") && !searchResults.Any(x => x.Name!.ToLower().Contains("spotify"))) {
        searchResults.Insert(0, new AppItem {
            Name = "Spotify",
            Path = "spotify", 
            Type = "App",
            Icon = GetFileIcon("spotify")
        });
    }

    // UPDATE UI
    if (searchResults.Any()) {
        // Ensure the list is unique and refreshed
        ResultsList.ItemsSource = searchResults.ToList();
        ResultsBorder.Visibility = Visibility.Visible;
    } else {
        ResultsBorder.Visibility = Visibility.Collapsed;
    }
}

        private ImageSource? GetFileIcon(string path)
        {
            try {
                if (!File.Exists(path) && !Directory.Exists(path)) return null;

                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path)!) {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            } catch { return null; }
        }

        private void LaunchSelectedApp()
        {
            try {
                string? target = (ResultsList.SelectedItem is AppItem selected) ? selected.Path : SearchInput.Text.Trim();
                if (string.IsNullOrEmpty(target)) return;

                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                Application.Current.Shutdown();
            } catch (Exception ex) { MessageBox.Show($"Launch Error: {ex.Message}"); }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { e.Handled = true; LaunchSelectedApp(); }
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
{
    _searchTimer.Stop(); // Stop so it doesn't loop
    
    
}

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => LaunchSelectedApp();
        private void BackgroundGrid_MouseDown(object sender, MouseButtonEventArgs e) { if (e.OriginalSource is Grid) Application.Current.Shutdown(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.Key == Key.Escape) Application.Current.Shutdown(); }
    }
}