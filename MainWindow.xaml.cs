using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OmniSearchApp
{
    public partial class MainWindow : Window
    {
        private readonly SearchService _searchService;
        private readonly IndexService _indexService;
        private readonly DispatcherTimer _searchTimer;

        public MainWindow()
        {
            InitializeComponent();

            _searchService = new SearchService();
            _indexService = new IndexService();

            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowBlur.EnableBlur(this);
            SearchInput.Focus();
            Activate();

            await Task.Run(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Indexing...");
                    await _indexService.BuildIndex();
                    Dispatcher.Invoke(() => StatusText.Text = "Indexing complete.");
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => StatusText.Text = $"Indexing error: {ex.Message}");
                }
            });
        }
        
        private void BackgroundGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid)
                Application.Current.Shutdown();
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer.Stop();

            string query = SearchInput.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                ResultsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var results = await Task.Run(() => _searchService.Search(query));

            Dispatcher.Invoke(() =>
            {
                if (results.Any())
                {
                    ResultsList.ItemsSource = results;
                    ResultsBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    ResultsBorder.Visibility = Visibility.Collapsed;
                }
            });
        }

        

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LaunchSelected();
            }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LaunchSelected();
        }

        private void LaunchSelected()
        {
            if (ResultsList.SelectedItem is not AppItem item)
                return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = item.Path,
                        UseShellExecute = true
                    });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Launch Error");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            this.Hide();
            settingsWindow.ShowDialog();
            this.Show();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

    }
}
