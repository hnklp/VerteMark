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
using System.Windows.Media.Media3D;


namespace VerteMark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public bool IsValidator { get; private set; }
        private string UserId;
        Utility utility;                            //######################
        private BitmapSource? bitmap;           //######################

        public MainWindow(bool IsValidator, string UserId) {
            InitializeComponent();
            this.IsValidator = IsValidator;
            this.UserId = UserId;
            utility = new Utility();                   //######################

            inkCanvas.Width = ImageHolder.Width;     // ######################
            inkCanvas.Height = ImageHolder.Height;    //######################

            List<CheckBox> CheckBoxes = new List<CheckBox>
            {
                CheckBox1, CheckBox2, CheckBox3, CheckBox4,
                CheckBox5, CheckBox6, CheckBox7, CheckBox8
            };


            foreach (var CheckBox in CheckBoxes) {
                CheckBox.IsEnabled = IsValidator;
                CheckBox.IsChecked = IsValidator;
            }


        }

        //dialog otevreni souboru s filtrem
        //TODO odstranit moznost vsechny soubory??
        //TODO pridat otevirani slozek - domluvit se jestli dve funkce nebo jedna
        //TODO dodelat exception

        private void OpenFileItem_Click(object sender, RoutedEventArgs e) {
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

        //kliknuti na o aplikaci
        private void AboutItem_Click(object sender, RoutedEventArgs e) {
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
        private void PropertiesItem_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("Properties clicked");
        }

        //soubor - zavrit
        private void CloseItem_Click(object sender, ExecutedRoutedEventArgs e) {
            Application.Current.Shutdown();
        }





        /*******************/
        //######################
        public BitmapSource ConvertInkCanvasToBitmap(InkCanvas inkCanvas) {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96d, 96d, PixelFormats.Default);
            renderBitmap.Render(inkCanvas);
            BitmapSource bitmapSource = renderBitmap;
            return bitmapSource;
        }

        //######################


        //######################
        private void Button_Click(object sender, RoutedEventArgs e) {
            /* 
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

                bitmap = ConvertInkCanvasToBitmap(inkCanvas);
                utility.SaveBitmapToFile(bitmap, saveFileDialog);
            */

        }

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
                }
            }
        }

        // Když přestaneš držet myš při kreslení tak ulož co jsi nakreslil do anotace
        private void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            utility.UpdateSelectedAnotation(ConvertInkCanvasToBitmap(inkCanvas));
            previewImage.Source = utility.GetActiveAnotaceImage();
            inkCanvas.Strokes.Clear();
        }


        /* Přepínání anotací */
        private void Button_Click_1(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(0);
            textik.Text = "Active anotace is: " + utility.GetActiveAnoticeId();
            previewImage.Source = utility.GetActiveAnotaceImage();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(1);
            textik.Text = "Active anotace is: " + utility.GetActiveAnoticeId();
            previewImage.Source = utility.GetActiveAnotaceImage();

        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(2);

        }

        private void Button_Click_4(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(3);

        }

        private void Button_Click_5(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(4);

        }

        private void Button_Click_6(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(5);

        }

        private void Button_Click_7(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(6);

        }

        private void Button_Click_8(object sender, RoutedEventArgs e) {
            utility.ChangeActiveAnotation(7);

        }
    }
}