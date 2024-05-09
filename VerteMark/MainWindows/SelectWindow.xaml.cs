using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;


namespace VerteMark
{
    /// <summary>
    /// Interakční logika pro SelectWindow.xaml
    /// </summary>
    public partial class SelectWindow : Window
    {
        Utility utility;

        public SelectWindow()
        {
            InitializeComponent();
            utility = new Utility();
        }

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

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "all_files_opend_str (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "open_dialog_title_str";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                utility.ChooseProjectFolder(selectedFilePath);
                this.HintLabel.Content = openFileDialog.FileName;
            }

            ContinueButton.IsEnabled = true;
        }

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