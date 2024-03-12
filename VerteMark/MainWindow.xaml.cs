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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFileItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DICOM (*.dcm)|*.dcm|JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|Všechny soubory (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Zde můžete provést další akce s vybraným souborem
                    string selectedFileName = openFileDialog.FileName;
                    // Například můžete načíst obsah souboru a zpracovat ho:
                    string fileContent = File.ReadAllText(selectedFileName);
                    // Zde můžete provést další zpracování obsahu souboru
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při otevírání souboru: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("VerteMark\nVerze {version}\nAutoři: Alex Schönfelder\nHynek Půta\nJakub Kepič\nJosef Bér\nMatěj Ježek\nSabina Ajksnerová\n2024 PřF UJEP");
        }

        private void PropertiesItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Properties clicked");
        }

        private void CloseItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}