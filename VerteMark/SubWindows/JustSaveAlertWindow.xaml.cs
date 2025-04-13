using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;
using VerteMark.ObjectClasses.FolderClasses;

namespace VerteMark.SubWindows
{
    /// <summary>
    /// Interakční logika pro JustSaveAlertWindow.xaml
    /// </summary>
    public partial class JustSaveAlertWindow : Window
    {
        Project project;
        MainWindow mainWindow;
        bool validator;
        bool saveButton; // true = save button v mainwindow, false = open button v mainwindow

        public JustSaveAlertWindow(MainWindow mainWindow, bool validator, bool saveButton)
        {
            InitializeComponent();
            project = Project.GetInstance();
            this.mainWindow = mainWindow;
            this.validator = validator;
            this.saveButton = saveButton;
            mainWindow.IsEnabled = false;

            if (validator)
            {
                SendForValidationButton.IsEnabled = false;
                ValidateButton.IsEnabled = true;
            }

            else
            {
                SendForValidationButton.IsEnabled = true;
                ValidateButton.IsEnabled = false;
            }
        }

        // Zpět do hlavního okna
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.IsEnabled = true;
            this.Close();
        }

        // Uložit do k validaci
        private void SendForValidation_Click(object sender, RoutedEventArgs e)
        {
            if (validator) { SendForValidationButton.IsEnabled = false; }
            else { SendForValidationButton.IsEnabled = true; }
            
            if (!validator) { project.SaveProject(1); }
            else { project.SaveProject(2); }
            project.saved = true;
            Browse(false);
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        // Uložit do validované
        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            if (validator) { ValidateButton.IsEnabled = true; }
            else { ValidateButton.IsEnabled = false; }
            if (!validator) { project.SaveProject(1); }
            else { project.SaveProject(2); }
            project.saved = true;
            Browse(false);
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        // Uložit do DICOMs
        private void SaveToDICOMButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Tato operace nenávratně smaže VEŠKERÉ ÚPRAVY provedené na souboru. Opravdu přesunout?",
                    "Varování",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {

                project.folderUtilityManager.Discard();

                Browse(true);
                mainWindow.IsEnabled = true;
                this.Close();
            }
        }

        // Uložit do rozpracované
        private void SaveWIPButton_Click(object sender, RoutedEventArgs e)
        {
            if (!validator) { project.SaveProject(0); }
            else { project.SaveProject(0); }
            project.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

        }

        // Zahodit změny
        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Tato operace smaže veškeré neuložené změny. Opravdu pokračovat?",
                    "Varování",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                project.Choose(project.fileName, project.projectType);
                //mainWindow.IsEnabled = false;

                this.mainWindow.Close();
                MainWindow mainWindow = new MainWindow();

                // Získání středu původního okna
                double originalCenterX = Left + Width / 2;
                double originalCenterY = Top + Height / 2;

                // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
                mainWindow.Left = originalCenterX - mainWindow.Width / 2;
                mainWindow.Top = originalCenterY - mainWindow.Height / 2;

                mainWindow.Show();

                this.Close();
            }
        }

        public void Browse(bool select)
        {
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(select);

            folderbrowserWindow.oldMainWindow = mainWindow;

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;

            mainWindow.Visibility = Visibility.Hidden;
            folderbrowserWindow.Show();
            
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.IsEnabled = true;
        }
    }
}