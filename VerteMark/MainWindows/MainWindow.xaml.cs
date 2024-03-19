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
using System.Windows.Shell;
using System;
using System.IO;
using Microsoft.Win32;
using VerteMark.ObjectClasses;
using FellowOakDicom;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;


namespace VerteMark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    /// TODO: Pridat nazev otevreneho souboru a rezimu anotator/validator do titulku aplikace
    /// TODO: Rozdelit status bar oddelovaci a text s ID dat doprava
    public partial class MainWindow : Window
    {
        Utility utility;                           
        private BitmapSource? bitmap;           

        public MainWindow()
        {
            InitializeComponent();
            utility = new Utility(); 
            User? loggedInUser = utility.GetLoggedInUser();

            inkCanvas.Width = ImageHolder.Width;
            inkCanvas.Height = ImageHolder.Height;
            inkCanvas.Margin = new Thickness(0);

            previewImage.Width = ImageHolder.Width;
            previewImage.Height = ImageHolder.Height;
            previewImage.Margin = new Thickness(0);

            // Assuming you have a Grid as the common container
            Grid.SetColumn(inkCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(inkCanvas, Grid.GetRow(ImageHolder));

            Grid.SetColumn(previewImage, Grid.GetColumn(ImageHolder));
            Grid.SetRow(previewImage, Grid.GetRow(ImageHolder));

            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();

            if (loggedInUser.Validator)
            {
                RoleStatus.Text = "v_status_str";
            }

            else
            {
                RoleStatus.Text = "a_status_str";
            }
            
            List<CheckBox> CheckBoxes = new List<CheckBox>
            {
                CheckBox1, CheckBox2, CheckBox3, CheckBox4,
                CheckBox5, CheckBox6, CheckBox7, CheckBox8
            };

            foreach (var CheckBox in CheckBoxes)
            {
                bool isValidator = loggedInUser.Validator;

                CheckBox.IsEnabled = isValidator;
                CheckBox.IsChecked = isValidator;
            }


        }

        //dialog otevreni souboru s filtrem
        //TODO odstranit moznost vsechny soubory??
        //TODO pridat otevirani slozek - domluvit se jestli dve funkce nebo jedna
        //TODO dodelat exception

        private void OpenFileItem_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "png_files_opend_str (*.png)|*.png|DICOM (*.dcm)|*.dcm|all_files_opend_str (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "open_dialog_title_str";

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
        //    Application.Current.Shutdown();   // <- začalo to házet error tak jsem to zakomentil
        }




        /*******************/
        //######################
        public WriteableBitmap ConvertInkCanvasToWriteableBitmap(InkCanvas inkCanvas) {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96d, 96d, PixelFormats.Default);
            
            renderBitmap.Render(inkCanvas);

            WriteableBitmap writeableBitmap = new WriteableBitmap(renderBitmap.PixelWidth, renderBitmap.PixelHeight, renderBitmap.DpiX, renderBitmap.DpiY, renderBitmap.Format, renderBitmap.Palette);
            renderBitmap.CopyPixels(new Int32Rect(0, 0, renderBitmap.PixelWidth, renderBitmap.PixelHeight), writeableBitmap.BackBuffer, writeableBitmap.BackBufferStride * writeableBitmap.PixelHeight, writeableBitmap.BackBufferStride);

            writeableBitmap.Lock();
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();

            return writeableBitmap;
        }

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
            utility.UpdateSelectedAnotation(ConvertInkCanvasToWriteableBitmap(inkCanvas));
            previewImage.Source = utility.GetActiveAnotaceImage();
            inkCanvas.Strokes.Clear();
        }
        //Smaže obsah vybrané anotace
        private void Smazat_butt(object sender, RoutedEventArgs e) {
            utility.ClearActiveAnotace();
            previewImage.Source = utility.GetActiveAnotaceImage();
        }

        /* Přepínání anotací */
        void SwitchActiveAnot(int id) {
            utility.ChangeActiveAnotation(id);
            previewImage.Source = utility.GetActiveAnotaceImage();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(0);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(1);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(2);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(3);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(4);

        }

        private void Button_Click_6(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(5);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(6);
        }

        private void Button_Click_8(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(7);
        }
    }
}
