// using System.Text;
// using System.Windows;
// using System.Windows.Controls;
// using System.Windows.Data;
// using System.Windows.Documents;
// using System.Windows.Input;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
// using System.Windows.Navigation;
// using System.Windows.Shapes;

// namespace OmniSearchApp;

// /// <summary>
// /// Interaction logic for MainWindow.xaml
// /// </summary>
// public partial class MainWindow : Window
// {
//     public MainWindow()
//     {
//         InitializeComponent();
//     }
// }

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
            // This allows to drag the search bar around with your mouse
            this.MouseDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchInput.Text;
            // TODO: This trigger the Search Engine later
        }

        private void SearchInput_GotFocus(object sender, RoutedEventArgs e)
        {
            // Logic for when the search bar is clicked
        }

     protected override void OnKeyDown(KeyEventArgs e)
{
    base.OnKeyDown(e);
    // Use Key.Escape to close when esc pressed
    if (e.Key == Key.Escape)
    {
        Application.Current.Shutdown();
    }
}
    }
}