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
using VerteMark.ObjectClasses;
using FellowOakDicom;


namespace VerteMark
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        // Vlastnosti
        Utility utility;
        private BitmapSource? bitmap;

        public MainWindow() {
            InitializeComponent();

            // Tady začíná kód
            utility = new Utility();

            inkCanvas.Width = ImageHolder.Width;
            inkCanvas.Height = ImageHolder.Height;
        }

        /*** LOGIN ***/
        private void LoginButt_Click(object sender, RoutedEventArgs e) {
            bool isValidator = IsValidatorSelected("validator", loginRadioContainer);
            // Přihlaš usera
            utility.LoginUser(loginTextBox.Text, isValidator);
            // Vypiš data o userovi
            User? user = utility.GetLoggedInUser();
            IDtext.Text = user.Id;
            VALIDATORtext.Text = user.IsValidator.ToString();
        }

        private bool IsValidatorSelected(string groupName, Grid container) {
            foreach (RadioButton radioButton in container.Children.OfType<RadioButton>()) {
                if (radioButton.GroupName == groupName && radioButton.IsChecked == true) {
                    return radioButton.Content.ToString() == "Validátor";
                }
            }
            return false; // Return false if no radio button is checked or if the checked button is not "Validátor"
        }

        /*******************/

        /*** PROJECT CREATION ***/


        private void OpenProject_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "Select a PNG File";
            if (openFileDialog.ShowDialog() == true) {
                string selectedFilePath = openFileDialog.FileName;
                bool success = utility.ChooseProjectFolder(selectedFilePath);
                if (success) {
                    //Pokud se vybrala dobrá složka/soubor tak pokračuj
                    BitmapImage bitmapImage = utility.GetOriginalPicture(); 


                    ImageHolder.Source = bitmapImage;
                    /*
                    inkCanvas.Width = bitmapImage.PixelWidth;
                    inkCanvas.Height = bitmapImage.PixelHeight;
                    */
                }
            }
        }



        /*******************/

        public BitmapSource ConvertInkCanvasToBitmap(InkCanvas inkCanvas) {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)inkCanvas.ActualWidth,(int)inkCanvas.ActualHeight,96d, 96d,PixelFormats.Default);
            renderBitmap.Render(inkCanvas);
            BitmapSource bitmapSource = renderBitmap;
            return bitmapSource;
        }

        public void SaveBitmapToFile(BitmapSource bitmap) {
            // Create a SaveFileDialog to prompt the user for file save location
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";
            // Show the dialog and get the result
            if (saveFileDialog.ShowDialog() == true) {
                // Create a BitmapEncoder based on the selected file format
                BitmapEncoder encoder = null;
                switch (System.IO.Path.GetExtension(saveFileDialog.FileName).ToUpper()) {
                    case ".PNG":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".JPG":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".BMP":
                        encoder = new BmpBitmapEncoder();
                        break;
                    default:
                        // Unsupported file format
                        return;
                }

                // Encode and save the bitmap to the selected file path
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create)) {
                    encoder.Save(stream);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            bitmap = ConvertInkCanvasToBitmap(inkCanvas);
            SaveBitmapToFile(bitmap);
        }

    }
}