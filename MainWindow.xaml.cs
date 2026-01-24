using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;

namespace OmniSearchApp
{
    // 1. Data model for apps
    public class AppItem
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
    }

    public partial class MainWindow : Window
    {
        private List<AppItem> allApps = new List<AppItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadInstalledApps(); // Scan the PC when the app starts
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowBlur.EnableBlur(this);
            this.Activate();
            SearchInput.Focus();
            Keyboard.Focus(SearchInput);
        }

        private void LoadInstalledApps()
        {
            string[] locations = {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
            };

            foreach (var path in locations)
            {
                if (Directory.Exists(path))
                {
                    try 
                    {
                        var files = Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            allApps.Add(new AppItem { 
                                Name = Path.GetFileNameWithoutExtension(file), 
                                Path = file 
                            });
                        }
                    }
                    catch { /* Skip folders connot access */ }
                }
            }
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchInput.Text.ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                ResultsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var filtered = allApps.Where(a => a.Name.ToLower().Contains(query)).Take(10).ToList();

            if (filtered.Any())
            {
                ResultsList.ItemsSource = filtered;
                ResultsBorder.Visibility = Visibility.Visible;
            }
            else
            {
                ResultsBorder.Visibility = Visibility.Collapsed;
            }
        }

        // Logic to launch the app
        private void LaunchSelectedApp()
        {
            if (ResultsList.SelectedItem is AppItem selectedApp)
            {
                try 
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedApp.Path,
                        UseShellExecute = true
                    });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LaunchSelectedApp();
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LaunchSelectedApp();
        }

        // protected override void OnKeyDown(KeyEventArgs e)
        // {
        //     base.OnKeyDown(e);
        //     if (e.Key == Key.Escape) Application.Current.Shutdown();
        // }

        // // Fixed clicking the dark background to close
        // protected override void OnMouseDown(MouseButtonEventArgs e)
        // {
        //     base.OnMouseDown(e);
        //     // If we clicked the Window itself (the blurred area) and not the search bar
        //     if (e.OriginalSource == this || e.OriginalSource is Grid)
        //     {
        //         Application.Current.Shutdown();
        //     }
        // }

        private void BackgroundGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Check if the background grid clicked specifically
        if (e.OriginalSource is Grid)
        {
            Application.Current.Shutdown();
        }
    }

    // Escape key to exit 
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) Application.Current.Shutdown();
    }
    }
}