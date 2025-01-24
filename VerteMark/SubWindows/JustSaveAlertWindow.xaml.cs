using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark.SubWindows {
    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class JustSaveAlertWindow : Window {
        Project project;
        MainWindow mainWindow;
        bool validator;

        public JustSaveAlertWindow(MainWindow mainWindow, bool validator) {
            InitializeComponent();
            project = Project.GetInstance();
            this.mainWindow = mainWindow;
            this.validator = validator;

            mainWindow.IsEnabled = false;
        }

        private void Back_Click(object sender, RoutedEventArgs e) {
            mainWindow.IsEnabled = true;
            this.Close();
        }

        private void SaveAndContinue_Click(object sender, RoutedEventArgs e) {
            if (!validator) { project.SaveProject(1); }
            else { project.SaveProject(2); }
            project.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        private void PreSaveAndContinue_Click(object sender, RoutedEventArgs e) {
            if (!validator) { project.SaveProject(0); }
            else { project.SaveProject(1); }
            project.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.IsEnabled = true;
        }
    }
}
