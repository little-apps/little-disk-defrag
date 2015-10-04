using System;
using System.Threading;
using System.Windows;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag
{
    public class App : Application
    {
        [STAThread]
        static void Main()
        {
            bool bMutexCreated;
            Mutex mutexMain = new Mutex(true, "Little Disk Defrag", out bMutexCreated);

            // If mutex isnt available, show message and exit...
            if (!bMutexCreated)
            {
                MessageBox.Show("Another program seems to be already running...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new App();

            mutexMain.Close();
        }

        public App()
        {
            StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

            // Add resources
            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri("Themes/Generic.xaml", UriKind.Relative) });

            Permissions.SetPrivileges(true);
            Run();
            Permissions.SetPrivileges(false);
        }
    }
}
