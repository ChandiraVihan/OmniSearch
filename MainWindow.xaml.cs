using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OmniSearchApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
            SearchInput.Focus();
            Keyboard.Focus(SearchInput);
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Search logic will go here
        }

        // This handles keys for the whole window
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // ESC closes the app
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }

        // Optional: If you want clicking the dark background to close the app
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            // If the user clicked outside the search box (on the window background)
            if (e.OriginalSource == this.Content || e.OriginalSource is Grid)
            {
                Application.Current.Shutdown();
            }
        }
    }
}