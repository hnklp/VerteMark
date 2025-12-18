using System.IO;
using System.Windows;
using System.Windows.Controls;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;
using VerteMark.ObjectClasses.FolderClasses;

namespace VerteMark.SubWindows
{
    /// <summary>
    /// Okno pro uložení projektu s možností výběru cílové složky (to_validate, validated, invalid).
    /// </summary>
    public partial class JustSaveAlertWindow : Window
    {
        Project project;
        MainWindow mainWindow;
        bool validator;
        /// <summary>True, pokud bylo okno otevřeno z tlačítka Save, false pokud z Open</summary>
        bool saveButton; // true = save button v mainwindow, false = open button v mainwindow
        private string _sourceButtonName;

        /// <summary>
        /// Vytvoří novou instanci JustSaveAlertWindow.
        /// </summary>
        /// <param name="mainWindow">Reference na hlavní okno</param>
        /// <param name="validator">True, pokud je uživatel validátor</param>
        /// <param name="saveButton">True, pokud bylo okno otevřeno z tlačítka Save</param>
        /// <param name="sourceButtonName">Název tlačítka, které okno otevřelo</param>
        public JustSaveAlertWindow(MainWindow mainWindow, bool validator, bool saveButton, string sourceButtonName)
        {
            InitializeComponent();
            project = Project.GetInstance();
            this.mainWindow = mainWindow;
            this.validator = validator;
            this.saveButton = saveButton;
            mainWindow.IsEnabled = false;
            this._sourceButtonName = sourceButtonName;

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

        /// <summary>
        /// Vrátí se zpět do hlavního okna bez uložení.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.IsEnabled = true;
            this.Close();
        }

        /// <summary>
        /// Uloží projekt do složky to_validate (anotátor) nebo validated (validátor).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SendForValidation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (validator) { SendForValidationButton.IsEnabled = false; }
            else { SendForValidationButton.IsEnabled = true; }
            
            if (!validator) { project.SaveProject(1, button.Name); }
            else { project.SaveProject(2, button.Name); }
            project.saved = true;
            Browse(false);
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        /// <summary>
        /// Uloží projekt do složky validated.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (validator) { ValidateButton.IsEnabled = true; }
            else { ValidateButton.IsEnabled = false; }
            if (!validator) { project.SaveProject(1, button.Name); }
            else { project.SaveProject(2, button.Name); }
            project.saved = true;
            Browse(false);
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        /// <summary>
        /// Zruší všechny změny a vrátí projekt do složky dicoms (pouze pro anotátory).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SaveToDICOMButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (MessageBox.Show("Tato operace nenávratně smaže VEŠKERÉ ÚPRAVY provedené na souboru. Opravdu přesunout?",
                    "Varování",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {

                project.folderUtilityManager.Discard(button.Name);
                //project.SaveProject(4);

                Browse(true);
                mainWindow.IsEnabled = true;
                this.Close();
            }
        }

        /// <summary>
        /// Uloží projekt do složky to_anotate (rozpracované).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SaveWIPButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            project.SaveProject(0, button.Name);
            project.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();
            if (_sourceButtonName == "OpenProject")
            {
                this.Browse(false);
            }
        }

        /// <summary>
        /// Zahodí všechny neuložené změny po potvrzení uživatele.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Tato operace smaže veškeré neuložené změny. Opravdu pokračovat?",
                    "Varování",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                if (_sourceButtonName == "SaveButtonUI")
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

                else
                {
                    if (project.projectType == "dicoms" &&
                        project.folderUtilityManager?.fileManager?.jsonPath != null)
                    {
                        Directory.Delete(project.folderUtilityManager.fileManager.outputPath, true);
                    }

                    this.Browse(false);
                }
            }
        }

        /// <summary>
        /// Otevře okno FolderbrowserWindow pro výběr projektu.
        /// </summary>
        /// <param name="select">True pro výběr nového .vmk souboru, false pro návrat do okna se seznamem DICOMů</param>
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

        /// <summary>
        /// Obsluha zavírání okna - povolí hlavní okno.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.IsEnabled = true;
        }
    }
}