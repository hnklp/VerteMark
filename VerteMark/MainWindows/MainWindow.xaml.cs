using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.IO;
using Microsoft.Win32;

namespace VerteMark
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsValidator;
        private string UserId;

        public MainWindow(bool IsValidator, string UserId)
        {
            InitializeComponent();
            this.IsValidator = IsValidator;
            this.UserId = UserId;
        }

        //dialog otevreni souboru s filtrem
        //TODO odstranit moznost vsechny soubory??
        //TODO pridat otevirani slozek - domluvit se jestli dve funkce nebo jedna
        private void OpenFileItem_Click(object sender, RoutedEventArgs e)
        {
            //toto mozna presunout jinam, at jsou tady jenom funkce pro tlacitka?
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DICOM (*.dcm)|*.dcm|Všechny soubory (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedFileName = openFileDialog.FileName;
                    //sem pridat co se ma se souborem udelat
                }
                catch (Exception ex) //safeguard pokud se nepovede soubor otevrit
                {
                    MessageBox.Show($"zde vlozit string (CZ: Chyba EN: Error {ex.Message}", "Sem to stejne", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //kliknuti na o aplikaci
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

        //kliknuti na nastaveni aplikace
        private void PropertiesItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Properties clicked");
        }

        //soubor - zavrit
        private void CloseItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}