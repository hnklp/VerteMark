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

namespace VerteMark
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {

        public WelcomeWindow()
        {
            InitializeComponent();
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
            var radioButton = sender as RadioButton;

            if (HintLabel == null) return; //safeguard

            if (radioButton != null && radioButton.IsChecked == true)
            {
                if (radioButton == AnotatorRadioButton)
                {
                    HintLabel.Content = "51 placeholder anapoveda";
                }
                else
                {
                    HintLabel.Content = "55 placeholder vnapoveda";
                }
            }
        }
 

        //talcitko prihlaseni, vyber rezimu podle RadioButton
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton? selectedRadioButton = null;
            if (AnotatorRadioButton.IsChecked == true)
            {
                selectedRadioButton = AnotatorRadioButton;
            }
            else if (ValidatorRadioButton.IsChecked == true)
            {
                selectedRadioButton = ValidatorRadioButton;
            }

            if (selectedRadioButton != null)
            {
                if (selectedRadioButton.Content.ToString() == "Anotátor")
                {
                    MainWindow windowAnotator = new MainWindow();
                    windowAnotator.Show();
                }
                else if (selectedRadioButton.Content.ToString() == "Validátor")
                {
                    MainWindowValidator windowValidator = new MainWindowValidator();
                    windowValidator.Show();
                }

                this.Close();
            }
        }
        //data z textboxu (zadani ID) tahejte z IDTextBox.text
    }
}
