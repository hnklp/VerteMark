using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.ObjectClasses;
using VerteMark.MainWindows;

namespace VerteMark
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        Project project;
        public WelcomeWindow()
        {
            InitializeComponent();
            IDTextBox.Focus();
            project = Project.GetInstance();
        }

        //textbox hint
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (string.IsNullOrEmpty(box.Text))
                {
                    box.Background = (ImageBrush)FindResource("watermark");
                    SignInButton.IsEnabled = false;
                }
                else
                {
                    box.Background = null;
                    SignInButton.IsEnabled = true;
                }
                    
            }
        }

        //zmena hintu podle vybraneho RadioButton
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
 

        //tlacitko prihlaseni, vyber rezimu podle RadioButton
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

        private void ReportItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nalezli jste chybu v aplikaci?" + "\n" + "Napište nám prosím mail na vertemark@prf.ujep.cz" + "\n" + "\n" + "Jako předmět uveďte BUG - Krátký popis chyby" + "\n" + "Do zprávy napište podrobný popis chyby a pokud víte, tak postup jak ji můžeme zreplikovat." + "\n" + "\n" + "Děkujeme za spolupráci!", "Nahlásit chybu");
        }

        private void SettingsButtonUI_Kopírovat_Click(object sender, RoutedEventArgs e) {
            new MainWindow(true).Show();
            this.Close();
        }

        // otevření návodu
        private void OpenGuide(object sender, RoutedEventArgs e)
        {
            GuideWindow guideWin = new GuideWindow();
            guideWin.Show();
            // this.Close();
        }

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
