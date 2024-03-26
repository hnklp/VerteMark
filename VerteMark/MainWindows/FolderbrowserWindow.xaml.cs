using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.ObjectClasses;

namespace VerteMark.MainWindows
{
    public partial class FolderbrowserWindow : Window
    {
        Utility utility;

        public FolderbrowserWindow()
        {
            InitializeComponent();
            utility = new Utility();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Aktualizace ListBoxu na základě vybraného typu projektu
            UpdateFileList();
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            // Získání názvu vybraného souboru nebo složky
            string filename = (sender as Button).Content.ToString();

            // Zavolání funkce Choose s názvem vybraného souboru nebo složky
            utility.Choose(filename);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch to SelectWindow
            SelectWindow selectWindow = new SelectWindow();

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            selectWindow.Left = originalCenterX - selectWindow.Width / 2;
            selectWindow.Top = originalCenterY - selectWindow.Height / 2;

            selectWindow.Show();

            this.Close();
        }

        private void UpdateFileList()
        {
            // Získání vybraného RadioButtonu
            RadioButton selectedRadioButton = FindVisualChildren<RadioButton>(this)
                .FirstOrDefault(radioButton => radioButton.IsChecked == true);

            if (selectedRadioButton != null)
            {
                // Vyčištění obsahu ListBoxu
                FileListBox.Items.Clear();

                Debug.WriteLine("--------FILENAME-------------");
                Debug.WriteLine(selectedRadioButton.Name);
                // Zobrazení seznamu souborů nebo složek jako tlačítek
                switch (selectedRadioButton.Name)
                {
                    case "DicomRadioButton":
                        List<String> filenames = utility.ChooseNewProject();
                        Debug.WriteLine(filenames.Count());
                        foreach (string filename in filenames)
                        {
                            Button button = new Button();
                            Debug.WriteLine(filename);
                            Debug.WriteLine("--------FILENAME-------------");
                            button.Content = filename;
                            button.Click += (sender, e) => { utility.Choose(filename); };
                            FileListBox.Items.Add(button);
                        }
                        break;
                    case "InProgressRadioButton":
                        List<String> filenamesA = utility.ChooseContinueAnotation();
                        Debug.WriteLine(filenamesA.Count());
                        foreach (string filename in filenamesA)
                        {
                            Debug.WriteLine(filename);
                            Debug.WriteLine("--------FILENAME---ANOTATE----------");
                            Button button = new Button();
                            button.Content = filename;
                            button.Click += (sender, e) => { utility.Choose(filename); };
                            FileListBox.Items.Add(button);
                        }
                        break;
                    case "ValidationRadioButton":
                        List<String> filenamesV = utility.ChooseValidation();
                        Debug.WriteLine(filenamesV.Count());
                        foreach (string filename in filenamesV)
                        {
                            Debug.WriteLine(filename);
                            Debug.WriteLine("--------FILENAME---VALIDATE----------");
                            Button button = new Button();
                            button.Content = filename;
                            button.Click += (sender, e) => { utility.Choose(filename); };
                            FileListBox.Items.Add(button);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // Pomocná metoda pro nalezení potomků daného typu
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
