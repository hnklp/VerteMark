using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using VerteMark.ObjectClasses;
using System.Windows.Ink;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Diagnostics;


namespace VerteMark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    /// TODO: Pridat nazev otevreneho souboru a rezimu anotator/validator do titulku aplikace
    public partial class MainWindow : Window
    {
        Utility utility;
        User? loggedInUser;
        List<CheckBox> checkBoxes;
        ToggleButton activeAnotButton;
        ToggleButton activeToolbarButton;

        StateManager stateManager;

        // Toolbar drag and drop

        bool isDragging = false;
        Point offset;
        Thumb grip;
        Point? cropStartPoint = null;

        // Connecting the line
        int minConnectDistance;
        int maxConnectDistance;

        public MainWindow() {
           
            InitializeComponent();
            utility = new Utility();
            checkBoxes = new List<CheckBox>
            {
                CheckBox1, CheckBox2, CheckBox3, CheckBox4,
                CheckBox5, CheckBox6, CheckBox7, CheckBox8
            };
            loggedInUser = utility.GetLoggedInUser();
            InitializeCheckboxes();
            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "v_status_str" : "a_status_str";
            ImageHolder.Source = utility.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            SwitchActiveAnot(0);
            SetCanvasAttributes();

            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;

            Loaded += delegate
            {
                SetCanvasComponentsSize();
            };
        }
        public MainWindow(bool debugging) {
            InitializeComponent();
            Debug.WriteLine($"Správný konstruktor zavolán.");
            utility = new Utility();
            utility.LoginUser("Debugger", true);
            utility.DebugProjectStart();
            checkBoxes = new List<CheckBox>
            {
                CheckBox1, CheckBox2, CheckBox3, CheckBox4,
                CheckBox5, CheckBox6, CheckBox7, CheckBox8
            };
            loggedInUser = utility.GetLoggedInUser();
            InitializeCheckboxes();
            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "v_status_str" : "a_status_str";
            ImageHolder.Source = utility.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            SwitchActiveAnot(0);
            Loaded += delegate {
                SetCanvasComponentsSize();
            };

            // Přidání CommandBinding pro Open
            CommandBinding openCommandBinding = new CommandBinding(
                ApplicationCommands.Open,
                OpenFileItem_Click
            );

            this.CommandBindings.Add(openCommandBinding);

            // Přidání CommandBinding pro Save
            CommandBinding saveCommandBinding = new CommandBinding(
                ApplicationCommands.Save,
                Save_Click);  // Již existující metoda

            this.CommandBindings.Add(saveCommandBinding);
        }

        //dialog otevreni souboru s filtrem
        //TODO odstranit moznost vsechny soubory??
        //TODO pridat otevirani slozek - domluvit se jestli dve funkce nebo jedna
        //TODO dodelat exception pri spatnem vyberu souboru (eg. .zip)

        void InitializeCheckboxes()
        {
            foreach (var CheckBox in checkBoxes)
            {
                bool isValidator = loggedInUser.Validator;
                CheckBox.IsEnabled = isValidator;
                CheckBox.IsChecked = isValidator;
            }
        }

        // Podle velikosti ImageHolder nastaví plátno
        void SetCanvasComponentsSize() {
            inkCanvas.Width = utility.GetOriginalPicture().Width;
            inkCanvas.Height = utility.GetOriginalPicture().Height;
            inkCanvas.Margin = new Thickness(0);
            PreviewImage.Width = utility.GetOriginalPicture().Width;
            PreviewImage.Height = utility.GetOriginalPicture().Height;
            PreviewImage.Margin = new Thickness(0);
            Grid.SetColumn(inkCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(inkCanvas, Grid.GetRow(ImageHolder));
            Grid.SetColumn(PreviewImage, Grid.GetColumn(ImageHolder));
            Grid.SetRow(PreviewImage, Grid.GetRow(ImageHolder));
            
        }
        // šířka pera, vzdálenosti pro doplnění
        void SetCanvasAttributes() {
            inkCanvas.DefaultDrawingAttributes.Width = 1;
            inkCanvas.DefaultDrawingAttributes.Height = 1;
            minConnectDistance = 2;
            maxConnectDistance = 10;
        }

        private void OpenFileItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "png_files_opend_str (*.png)|*.png|DICOM (*.dcm)|*.dcm|all_files_opend_str (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "open_dialog_title_str";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                bool success = utility.ChooseProjectFolder(selectedFilePath);
                if (success)
                {
                    //Pokud se vybrala dobrá složka/soubor tak pokračuj
                    BitmapImage bitmapImage = utility.GetOriginalPicture();
                    ImageHolder.Source = bitmapImage;
                }
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false; // Allow selecting only one file
            openFileDialog.Title = "Select a PNG File";
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                bool success = utility.ChooseProjectFolder(selectedFilePath);
                if (success)
                {
                    //Pokud se vybrala dobrá složka/soubor tak pokračuj
                    BitmapImage bitmapImage = utility.GetOriginalPicture();
                    ImageHolder.Source = bitmapImage;
                }
            }
        }

        /*
         * ============
         *  App states
         * ============
         */

        private void HandleStateChanged(object sender, AppState newState)
        {

            switch (newState)
            {
                case AppState.Drawing:
                case AppState.Erasing:

                    SetInkCanvasMode(newState);
                    CropCanvas.Visibility = Visibility.Hidden;

                    if (CroppedImage.Source != null)
                    {
                        ImageHolder.Visibility = Visibility.Hidden;
                        PreviewImage.Visibility = Visibility.Hidden;

                        CroppedImage.Visibility = Visibility.Visible;
                        CroppedPreviewImage.Visibility = Visibility.Visible;
                    }
                    break;

                case AppState.Cropping:

                    SetInkCanvasMode(newState);
                    CropCanvas.Visibility = Visibility.Visible;

                    if (CroppedImage.Source != null)
                    {
                        ImageHolder.Visibility = Visibility.Visible;
                        PreviewImage.Visibility = Visibility.Visible;

                        CroppedImage.Visibility = Visibility.Hidden;
                        CroppedPreviewImage.Visibility = Visibility.Hidden;
                    }

                    SetCanvasComponentsSize();
                    break;
                default:
                    break;
            }

        }

        private void SetInkCanvasMode(AppState state)
        {
            inkCanvas.EditingMode = state == AppState.Drawing ? InkCanvasEditingMode.Ink :
                                    state == AppState.Erasing ? InkCanvasEditingMode.EraseByPoint :
                                    InkCanvasEditingMode.None;
        }

        private void CropTButton_Click(object sender, RoutedEventArgs e)
        {
            stateManager.CurrentState = AppState.Cropping;
            SwitchActiveToolbarButton(sender as ToggleButton);
        }

        private void DrawTButton_Click(object sender, RoutedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                CropImage();
            }
            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(sender as ToggleButton);
        }

        private void EraseTButton_Click(object sender, RoutedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                CropImage();
            }
            stateManager.CurrentState = AppState.Erasing;
            SwitchActiveToolbarButton(sender as ToggleButton);
        }

        void SwitchActiveToolbarButton(ToggleButton pressedButton)
        {
            if (activeToolbarButton != null)
            {
                activeToolbarButton.IsChecked = false;
            }
            activeToolbarButton = pressedButton;
        }

        /*
         * ======
         *  Menu
         * ======
         */

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
            System.Windows.Application.Current.Shutdown();  
        }

    
        private void Save_Click(object sender, RoutedEventArgs e) {
            /* 
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

                bitmap = ConvertInkCanvasToBitmap(inkCanvas);
                utility.SaveBitmapToFile(bitmap, saveFileDialog);
            */
            Debug.WriteLine("VOLAM SAVE PROJECT");
            utility.SaveProject();
        }

        /*
         * =========
         *  Drawing
         * =========
         */

        //Spojování linky
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Drawing)
            {
                // Get the first and last points of the stroke
                StylusPoint firstPoint = e.Stroke.StylusPoints.First();
                StylusPoint lastPoint = e.Stroke.StylusPoints.Last();

                // Calculate the distance between the first and last points
                double distance = Math.Sqrt(Math.Pow(lastPoint.X - firstPoint.X, 2) + Math.Pow(lastPoint.Y - firstPoint.Y, 2));

                // If the distance is less than 5, connect the points with a line
                if (distance < 100)
                {
                    // Create a new stroke for the connecting line
                    StylusPointCollection points = new StylusPointCollection();
                    points.Add(firstPoint);
                    points.Add(lastPoint);
                    Stroke lineStroke = new Stroke(points);

                    // Set the color and thickness of the connecting line
                    lineStroke.DrawingAttributes.Color = utility.GetActiveAnotaceColor();
                    lineStroke.DrawingAttributes.Width = 1;

                    // Add the connecting line stroke to the InkCanvas
                    inkCanvas.Strokes.Add(lineStroke);
                }
            }
        }

        WriteableBitmap ConvertInkCanvasToWriteableBitmap(InkCanvas inkCanvas)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96d, 96d, PixelFormats.Default);
            renderBitmap.Render(inkCanvas);
            WriteableBitmap writeableBitmap = new WriteableBitmap(renderBitmap.PixelWidth, renderBitmap.PixelHeight, renderBitmap.DpiX, renderBitmap.DpiY, renderBitmap.Format, renderBitmap.Palette);
            renderBitmap.CopyPixels(new Int32Rect(0, 0, renderBitmap.PixelWidth, renderBitmap.PixelHeight), writeableBitmap.BackBuffer, writeableBitmap.BackBufferStride * writeableBitmap.PixelHeight, writeableBitmap.BackBufferStride);
            writeableBitmap.Lock();
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
            return writeableBitmap;
        }

        // Když přestaneš držet myš při kreslení tak ulož co jsi nakreslil do anotace
        void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Drawing)
            {
                utility.UpdateSelectedAnotation(ConvertInkCanvasToWriteableBitmap(inkCanvas));

                if (CroppedImage.Source == null)
                    PreviewImage.Source = utility.GetActiveAnotaceImage();
                else
                    CroppedPreviewImage.Source = utility.GetActiveAnotaceImage();
            }
        }

        //Smaže obsah vybrané anotace
        void Smazat_butt(object sender, RoutedEventArgs e)
        {
            utility.ClearActiveAnotace();
            inkCanvas.Strokes.Clear();
            PreviewImage.Source = utility.GetActiveAnotaceImage();
        }

        /* Přepínání anotací */
        void SwitchActiveAnot(int id)
        {
            utility.ChangeActiveAnotation(id);
            //   previewImage.Source = utility.GetActiveAnotaceImage();
            inkCanvas.DefaultDrawingAttributes.Color = utility.GetActiveAnotaceColor();
            inkCanvas.Strokes.Clear();
            PreviewImage.Source = utility.GetActiveAnotaceImage();
        }

        void SwitchActiveAnotButton(ToggleButton pressedButton)
        {
            if (activeAnotButton != null)
            {
                activeAnotButton.IsChecked = false;
            }
            activeAnotButton = pressedButton;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(0);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(1);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(2);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(3);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(4);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(5);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(6);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            SwitchActiveAnot(7);
            SwitchActiveAnotButton(sender as ToggleButton);
        }

        /*
         * =======================
         *  Toolbar drop and drag
         * =======================
         */

        private T GetParent<T>(DependencyObject d) where T : class
        {
            while (d != null && !(d is T))
            {
                d = VisualTreeHelper.GetParent(d);
            }
            return d as T;

        }

        private void Grip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(ToolBarTray);
            IInputElement ie = ToolBarTray.InputHitTest(p);
            grip = GetParent<Thumb>(ie as DependencyObject);
            if (grip != null)
            {
                isDragging = true;
                offset = e.GetPosition(ToolBarTray);
                grip.CaptureMouse();
            }
        }

        private void Grip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPoint = e.GetPosition(ToolBarTray);

                if (grip != null && grip.IsMouseCaptured)
                {
                    Point newPosition = Mouse.GetPosition(this);
                    int toolbarOffset = 18;
                    double newX = newPosition.X - offset.X;
                    double newY = newPosition.Y - offset.Y - toolbarOffset;

                    // Ensure the ToolBarTray stays within the bounds of the window
                    newX = Math.Max(0, Math.Min(newX, Grid.ColumnDefinitions[0].ActualWidth - ToolBarTray.ActualWidth));
                    newY = Math.Max(0, Math.Min(newY, Grid.ActualHeight - ToolBarTray.ActualHeight));

                    ToolBarTray.Margin = new Thickness(newX, newY, 0, 0);
                }
            }
        }

        private void Grip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (grip != null)
            {
                isDragging = false;
                grip.ReleaseMouseCapture();
            }
        }

        /*
         * ======
         *  Crop
         * ======
         */

        private void CropCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                cropStartPoint = e.GetPosition(CropCanvas);
                CropRectangle.Width = 0;
                CropRectangle.Height = 0;
            }
        }

        private void CropCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                if (cropStartPoint.HasValue)
                {
                    cropStartPoint = null;
                }
            }
        }

        private void CropCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                if (cropStartPoint.HasValue)
                {
                    var pos = e.GetPosition(CropCanvas);

                    var x = Math.Min(pos.X, cropStartPoint.Value.X);
                    var y = Math.Min(pos.Y, cropStartPoint.Value.Y);

                    var width = Math.Max(pos.X, cropStartPoint.Value.X) - x;
                    var height = Math.Max(pos.Y, cropStartPoint.Value.Y) - y;

                    Canvas.SetLeft(CropRectangle, x);
                    Canvas.SetTop(CropRectangle, y);

                    CropRectangle.Width = width;
                    CropRectangle.Height = height;
                }
            }
        }

        private void CropImage()
        {
            Int32Rect rect = new Int32Rect((int)Canvas.GetLeft(CropRectangle),
                                      (int)Canvas.GetTop(CropRectangle),
                                      (int)CropRectangle.Width,
                                      (int)CropRectangle.Height);

            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                return;
          
            BitmapSource croppedImage = new CroppedBitmap(ImageHolder.Source as BitmapSource, rect);
            CroppedImage.Source = croppedImage;

            if (PreviewImage.Source !=  null)
            {
                BitmapSource croppedPreviewImage = new CroppedBitmap(PreviewImage.Source as BitmapSource, rect);
                CroppedPreviewImage.Source = croppedPreviewImage;
            }

            inkCanvas.Width = CropRectangle.Width;
            inkCanvas.Height = CropRectangle.Height;
            PreviewImage.Width = CropRectangle.Width;
            PreviewImage.Height = CropRectangle.Height;
            CropCanvas.Width = CropRectangle.Width;
            CropCanvas.Height = CropRectangle.Height;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else if (e.Delta < 0)
                {
                    ZoomOut();
                }

                e.Handled = true;
            }
        }

        private void ZoomIn()
        {
            if (ZoomSlider.Value < ZoomSlider.Maximum)
            {
                ZoomSlider.Value += 10;
            }
        }

        private void ZoomOut()
        {
            if (ZoomSlider.Value > ZoomSlider.Minimum)
            {
                ZoomSlider.Value -= 10;
            }
        }

        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ImageHolder != null)
            {
                double zoomFactor = ZoomSlider.Value / 100;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);
            }
        }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is double))
                return null;

            double numericValue = (double)value;
            int intValue = (int)Math.Round(numericValue);
            return string.Format("{0}%", intValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        
    }

}