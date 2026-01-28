using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace OmniSearchApp
{
    public partial class SettingsWindow : Window
    {
        private readonly string _configFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OmniSearch",
            "settings.json"
        );

        private List<string> _indexedFolders = new List<string>();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = File.ReadAllText(_configFile);
                    _indexedFolders = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                else
                {
                    _indexedFolders = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");
                _indexedFolders = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            }
            
            UpdateListBox();
        }

        private void SaveSettings()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_indexedFolders);
                File.WriteAllText(_configFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private void UpdateListBox()
        {
            FoldersListBox.ItemsSource = null;
            FoldersListBox.ItemsSource = _indexedFolders;
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                var folder = dialog.FolderName;
                if (!_indexedFolders.Contains(folder))
                {
                    _indexedFolders.Add(folder);
                    SaveSettings();
                    UpdateListBox();
                }
            }
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersListBox.SelectedItem is string selectedFolder)
            {
                _indexedFolders.Remove(selectedFolder);
                SaveSettings();
                UpdateListBox();
            }
        }
    }
}
