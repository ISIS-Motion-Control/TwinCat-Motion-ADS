using System.Windows;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            TwinCat_Motion_ADS.Properties.Settings.Default.Save();
        }
    }
}
