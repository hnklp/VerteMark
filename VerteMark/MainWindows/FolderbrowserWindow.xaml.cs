using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.ObjectClasses;

namespace VerteMark.MainWindows
{
    public partial class FolderbrowserWindow : Window
    {
        Utility utility;
        string projectType; // create new project or load existing
        bool loadFromSelect;
        public MainWindow? oldMainWindow;

        public FolderbrowserWindow(bool fromSelect)
        {
            InitializeComponent();
            utility = new Utility();
            projectType = "";
            loadFromSelect = fromSelect;
            oldMainWindow = null;
            

            // Přidání obslužné metody pro událost SelectionChanged pro FileListBox
            FileListBox.SelectionChanged += FileListBox_SelectionChanged;
            LoadforRole();
            Closing += FolderbrowserWindow_Closing;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Aktualizace ListBoxu na základě vybraného typu projektu
            UpdateFileList();
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && SelectedRadioButtonTextBlock != null)
            {
                SelectedRadioButtonTextBlock.Text = radioButton.Content.ToString();
                // Aktualizujte ListBox na základě vybraného typu projektu
                UpdateFileList();
            }
        }


        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {

            string selectedFile = FileListBox.SelectedItem as string;

            // Zavolání metody Choose s názvem vybraného souboru
            utility.Choose(selectedFile, projectType);

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

        private void FolderbrowserWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            // Kontrola, zda oldMainWindow není null a zda je viditelné
            if (oldMainWindow != null && oldMainWindow.Visibility == Visibility.Hidden) {
                // Zavření oldMainWindow
                oldMainWindow.Close();
            }
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Zkontrolujte, zda je vybrán nějaký soubor
            if (FileListBox.SelectedItem != null)
            {
                // Pokud je vybrán soubor, povolte tlačítko ContinueButton
                ContinueButton.IsEnabled = true;
            }
            else
            {
                ContinueButton.IsEnabled = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (loadFromSelect) {
                // Switch to SelectWindow
                SelectWindow selectWindow = new SelectWindow();

                // Získání středu původního okna
                double originalCenterX = Left + Width / 2;
                double originalCenterY = Top + Height / 2;

                // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
                selectWindow.Left = originalCenterX - selectWindow.Width / 2;
                selectWindow.Top = originalCenterY - selectWindow.Height / 2;

                selectWindow.Show();

                if (oldMainWindow != null)
                {
                    oldMainWindow.Close();
                }
                
                this.Close();
            }
            else {
                oldMainWindow.Visibility = Visibility.Visible;
                this.Close();
            }
        }

        private void UpdateFileList()
        {
            // Získání vybraného RadioButtonu
            RadioButton selectedRadioButton = FindVisualChildren<RadioButton>(this)
                .FirstOrDefault(radioButton => radioButton.IsChecked == true);

            if (selectedRadioButton != null)
            {
                // Aktualizace ListBoxu na základě vybraného typu projektu
                switch (selectedRadioButton.Name)
                {
                    case "DicomRadioButton":
                        FileListBox.ItemsSource = utility.ChooseNewProject();
                        projectType = "dicoms";
                        break;
                    case "InProgressRadioButton":
                        FileListBox.ItemsSource = utility.ChooseContinueAnotation();
                        projectType = "to_anotate";
                        break;
                    case "ValidationRadioButton":
                        FileListBox.ItemsSource = utility.ChooseValidation();
                        projectType = "to_validate";
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


        void LoadforRole()
       {        
            if (utility.GetLoggedInUser().Validator){
                    FileListBox.ItemsSource = utility.ChooseValidation();
                    projectType = "to_validate";
                    UpdateFileList();
                    SelectedRadioButtonTextBlock.Text = "K validaci";
                    ValidationRadioButton.IsChecked = true;
            }
            else
            {
                FileListBox.ItemsSource = utility.ChooseNewProject();
                projectType = "dicoms";
                UpdateFileList();
                DicomRadioButton.IsChecked = true;

            }
            }
        }
    }

