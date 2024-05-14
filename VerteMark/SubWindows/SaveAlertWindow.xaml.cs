using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark.SubWindows
{
    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class SaveAlertWindow : Window {
        Utility utility;
        MainWindow mainWindow;
        bool validator;

        public SaveAlertWindow(MainWindow mainWindow, bool validator) {
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
            else {  utility.SaveProject(2); }
            utility.saved = true;
            Browse();
            mainWindow.IsEnabled = true;
            this.Close();
        }

        private void PreSaveAndContinue_Click(object sender, RoutedEventArgs e) {
            if (!validator) { utility.SaveProject(0); }
            else { utility.SaveProject(1); }
            utility.saved = true;
            Browse();
            mainWindow.IsEnabled = true;
            this.Close();
        }

        public void Browse() {
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(false);

            folderbrowserWindow.oldMainWindow = mainWindow;

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;

            mainWindow.Visibility = Visibility.Hidden;
            folderbrowserWindow.Show();
        }
    }
}
