using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            utility = new Utility();
        }

        //textbox hint
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (string.IsNullOrEmpty(box.Text))
                    box.Background = (ImageBrush)FindResource("watermark");
                else
                    box.Background = null;
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
                    HintLabel.Content = "51 placeholder anapoveda";
                }
                else
                {
                    HintLabel.Content = "55 placeholder vnapoveda";
                }
            }
        }
 

        //tlacitko prihlaseni, vyber rezimu podle RadioButton
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            bool IsValidator = false;
            if (ValidatorRadioButton.IsChecked == true)
            {
                IsValidator = true;
            }
            string UserId = IDTextBox.Text;

            MainWindow MainAnotator = new MainWindow(IsValidator, UserId);

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            MainAnotator.Left = originalCenterX - MainAnotator.Width / 2;
            MainAnotator.Top = originalCenterY - MainAnotator.Height / 2;

            MainAnotator.Show();

            this.Close();
        }
        //data z textboxu (zadani ID) tahejte z IDTextBox.text
    }
}
