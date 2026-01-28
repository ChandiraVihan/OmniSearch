using System.Configuration;
using System.Data;
using System.Windows;

namespace OmniSearchApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var indexService = new IndexService();
        _ = indexService.BuildIndex(); // Fire and forget
    }
}
