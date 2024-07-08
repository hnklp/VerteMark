using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        Utility utility;
        public WelcomeWindow()
        {
            InitializeComponent();
            IDTextBox.Focus();
            utility = new Utility();
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

            utility.LoginUser(UserId, IsValidator);

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

        private void SettingsButtonUI_Kopírovat_Click(object sender, RoutedEventArgs e) {
            new MainWindow(true).Show();
            this.Close();
        }

        // otevření návodu
        private void OpenGuide(object sender, RoutedEventArgs e)
        { 
            Window1 window1 = new Window1();
            window1.Show();
            this.Close();
        }
    }
}
