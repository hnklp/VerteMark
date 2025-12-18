using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.ObjectClasses;
using VerteMark.MainWindows;

namespace VerteMark
{
    /// <summary>
    /// Úvodní okno aplikace pro přihlášení uživatele a výběr role (anotátor/validátor).
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        Project project;
        /// <summary>
        /// Vytvoří novou instanci WelcomeWindow a inicializuje projekt.
        /// </summary>
        public WelcomeWindow()
        {
            InitializeComponent();
            IDTextBox.Focus();
            project = Project.GetInstance();
        }

        /// <summary>
        /// Obsluha změny textu v textovém poli - povolí/zakáže tlačítko přihlášení.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (string.IsNullOrEmpty(box.Text))
                {
                    SignInButton.IsEnabled = false;
                }
                else
                {
                    SignInButton.IsEnabled = true;
                }
                    
            }
        }

        /// <summary>
        /// Změní nápovědu podle vybraného RadioButton (anotátor/validátor).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void RadioButton_Hint(object sender, RoutedEventArgs e)
        {
            var RadioButton = sender as RadioButton;

            if (HintLabel == null) return; //safeguard

            if (RadioButton != null && RadioButton.IsChecked == true)
            {
                if (RadioButton == AnotatorRadioButton)
                {
                    HintLabel.Content = "Anotátor provádí anotaci.";
                }
                else
                {
                    HintLabel.Content = "Validátor ověřuje správnost anotací.";
                }
            }
        }
 

        /// <summary>
        /// Obsluha kliknutí na tlačítko přihlášení - přihlásí uživatele a otevře SelectWindow.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            // User login
            bool IsValidator = false;
            if (ValidatorRadioButton.IsChecked == true)
            {
                IsValidator = true;
            }
            
            string UserId = IDTextBox.Text;

            project.LoginNewUser(UserId, IsValidator);

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

        /// <summary>
        /// Zobrazí dialog s informacemi o nahlášení chyby.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ReportItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nalezli jste chybu v aplikaci?" + "\n" + "Napište nám prosím mail na software@digitech.ujep.cz" + "\n" + "\n" + "Jako předmět uveďte BUG - VerteMark - Krátký popis chyby" + "\n" + "Do zprávy napište podrobný popis chyby a pokud víte, tak postup jak ji můžeme zreplikovat." + "\n" + "\n" + "Děkujeme za spolupráci!", "Nahlásit chybu");
        }

        /// <summary>
        /// Otevře debug režim aplikace (pro testování).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SettingsButtonUI_Kopírovat_Click(object sender, RoutedEventArgs e) {
            new MainWindow(true).Show();
            this.Close();
        }

        /// <summary>
        /// Otevře okno s uživatelskou příručkou.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void OpenGuide(object sender, RoutedEventArgs e)
        {
            GuideWindow guideWin = new GuideWindow();
            guideWin.Show();
            // this.Close();
        }

        /// <summary>
        /// Otevře okno s informacemi o aplikaci.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow AboutWindow = new AboutWindow();

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            AboutWindow.Left = originalCenterX - AboutWindow.Width / 2;
            AboutWindow.Top = originalCenterY - AboutWindow.Height / 2;

            AboutWindow.Show();
        }
    }
}
