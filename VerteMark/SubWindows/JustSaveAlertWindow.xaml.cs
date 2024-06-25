using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark.SubWindows {
    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class JustSaveAlertWindow : Window {
        Utility utility;
        MainWindow mainWindow;
        bool validator;

        public JustSaveAlertWindow(MainWindow mainWindow, bool validator) {
            InitializeComponent();
            utility = new Utility();
            this.mainWindow = mainWindow;
            this.validator = validator;

            mainWindow.IsEnabled = false;
        }

        private void Back_Click(object sender, RoutedEventArgs e) {
            mainWindow.IsEnabled = true;
            this.Close();
        }

        private void SaveAndContinue_Click(object sender, RoutedEventArgs e) {
            if (!validator) { utility.SaveProject(1); }
            else { utility.SaveProject(2); }
            utility.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

            if (!utility.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        private void PreSaveAndContinue_Click(object sender, RoutedEventArgs e) {
            if (!validator) { utility.SaveProject(0); }
            else { utility.SaveProject(1); }
            utility.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.IsEnabled = true;
        }
    }
}
