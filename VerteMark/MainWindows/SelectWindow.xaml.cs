using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;


namespace VerteMark
{
    /// <summary>
    /// Okno pro výběr akce - vytvoření nového projektu nebo pokračování v práci.
    /// </summary>
    public partial class SelectWindow : Window
    {
        Project project;
        
        /// <summary>
        /// Vytvoří novou instanci SelectWindow a inicializuje projekt.
        /// </summary>
        public SelectWindow()
        {
            InitializeComponent();
            project = Project.GetInstance();
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko pokračování - otevře FolderbrowserWindow.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(true);

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;


            folderbrowserWindow.Show();

            this.Close();
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko výběru - otevře dialog pro výběr .vmk souboru.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Soubory VerteMark (.vmk)|*.vmk";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "Otevřít soubor";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                project.ChooseProjectFolder(selectedFilePath);
                HintText.Text = selectedFilePath;
            }

            if (openFileDialog.FileName != "" && openFileDialog.FileName != null)
            {
                ContinueButton.IsEnabled = true;
            }

        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko zpět - vrátí se na WelcomeWindow.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WelcomeWindow welcomeWindow = new WelcomeWindow();

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            welcomeWindow.Left = originalCenterX - welcomeWindow.Width / 2;
            welcomeWindow.Top = originalCenterY - welcomeWindow.Height / 2;


            welcomeWindow.Show();

            this.Close();
        }

       
        }

    }