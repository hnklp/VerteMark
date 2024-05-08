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
    public partial class MainWindow : Window {
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

        // Canvas Drag Move View
        private bool _isDragging = false;
        private Point _startDragPoint;

        // Image crop
        Point? cropStartPoint = null;

        // Dont worry about it (canvas)
        private StylusPoint? firstPoint = null;
        private StylusPoint? lastPoint = null;

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
            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;

            activeAnotButton = Button1;
            activeToolbarButton = DrawTButton;
          
            Loaded += delegate
            {
                SetCanvasComponentsSize();
                SwitchActiveAnot(0);

                // start at 25% zoom
                double zoomFactor = 0.25;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);
            };
            
        }

        //dialog otevreni souboru s filtrem
        //TODO odstranit moznost vsechny soubory??
        //TODO pridat otevirani slozek - domluvit se jestli dve funkce nebo jedna
        //TODO dodelat exception pri spatnem vyberu souboru (eg. .zip)

        void InitializeCheckboxes() {
            foreach (var CheckBox in checkBoxes) {
                bool isValidator = loggedInUser.Validator;
                CheckBox.IsEnabled = isValidator;
                CheckBox.IsChecked = isValidator;
            }
        }

        // Podle velikosti ImageHolder nastaví plátno
        void SetCanvasComponentsSize() {
            InkCanvas.Width = utility.GetOriginalPicture().PixelWidth;
            InkCanvas.Height = utility.GetOriginalPicture().PixelHeight;
            InkCanvas.Margin = new Thickness(0);
            PreviewImage.Width = utility.GetOriginalPicture().PixelWidth;
            PreviewImage.Height = utility.GetOriginalPicture().PixelHeight;
            PreviewImage.Margin = new Thickness(0);
            CropCanvas.Width = utility.GetOriginalPicture().PixelWidth;
            CropCanvas.Height = utility.GetOriginalPicture().PixelHeight;
            CropCanvas.Margin = new Thickness(0);
            Grid.SetColumn(InkCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(InkCanvas, Grid.GetRow(ImageHolder));
            Grid.SetColumn(PreviewImage, Grid.GetColumn(ImageHolder));
            Grid.SetRow(PreviewImage, Grid.GetRow(ImageHolder));
            Grid.SetColumn(CropCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(CropCanvas, Grid.GetRow(ImageHolder));
        }

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
                }
            }
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

        /*
         * ============
         *  App states
         * ============
         */

        private void HandleStateChanged(object sender, AppState newState) {

            switch (newState) {
                case AppState.Drawing:

                    SetInkCanvasMode(newState);
                    CropCanvas.Visibility = Visibility.Hidden;
                    CropLabel.Visibility = Visibility.Collapsed;
                    CropConfirmButton.Visibility = Visibility.Collapsed;
                    CropCancelButton.Visibility = Visibility.Collapsed;

                    if (CroppedImage.Source != null) {
                        ImageHolder.Visibility = Visibility.Hidden;
                        PreviewImage.Visibility = Visibility.Hidden;

                        CroppedImage.Visibility = Visibility.Visible;
                    }
                    break;

                case AppState.Cropping:

                    SetInkCanvasMode(newState);
                    CropCanvas.Visibility = Visibility.Visible;
                    CropLabel.Visibility = Visibility.Visible;
                    CropConfirmButton.Visibility = Visibility.Visible;
                    CropCancelButton.Visibility = Visibility.Visible;

                    if (CroppedImage.Source != null) {
                        ImageHolder.Visibility = Visibility.Visible;
                        PreviewImage.Visibility = Visibility.Visible;

                        CroppedImage.Visibility = Visibility.Hidden;
                    }

                    SetCanvasComponentsSize();
                    break;
                default:
                    break;
            }

        }

        private void SetInkCanvasMode(AppState state) {
            InkCanvas.EditingMode = state == AppState.Drawing ? InkCanvasEditingMode.Ink :
                                    InkCanvasEditingMode.None;
        }

        private void CropTButton_Click(object sender, RoutedEventArgs e) {
            stateManager.CurrentState = AppState.Cropping;
            SwitchActiveToolbarButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void DrawTButton_Click(object sender, RoutedEventArgs e) {
            if (stateManager.CurrentState == AppState.Cropping) {
                CropImage();
            }
            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(sender as ToggleButton);
            e.Handled = true;
        }

        void SwitchActiveToolbarButton(ToggleButton pressedButton) {
            if (activeToolbarButton != null) {
                activeToolbarButton.IsChecked = false;
                pressedButton.IsChecked = true;
            }
            activeToolbarButton = pressedButton;
        }

        /*
         * ======
         *  Menu
         * ======
         */

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
            System.Windows.Application.Current.Shutdown();
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            utility.SaveProject();
        }

        /*
         * =========
         *  Drawing
         * =========
         */

        //Spojování linky
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e) {
            if (firstPoint == null) {
                firstPoint = e.Stroke.StylusPoints.First();
            }
            lastPoint = e.Stroke.StylusPoints.Last();
        }

        void ConnectStrokeAnotace() {
            if (stateManager != null && stateManager.CurrentState == AppState.Drawing && firstPoint != null && lastPoint != null) {
                // Calculate the distance between the first and last points
                double distance = Math.Sqrt(Math.Pow(lastPoint.Value.X - firstPoint.Value.X, 2) + Math.Pow(lastPoint.Value.Y - firstPoint.Value.Y, 2));

                // If the distance is less than 100, connect the points with a line
                if (distance < 100) {
                    // Create a new stroke for the connecting line
                    StylusPointCollection points = new StylusPointCollection();
                    points.Add(firstPoint.Value);
                    points.Add(lastPoint.Value);
                    Stroke lineStroke = new Stroke(points);

                    // Set the color and thickness of the connecting line
                    lineStroke.DrawingAttributes.Color = utility.GetActiveAnotaceColor();
                    lineStroke.DrawingAttributes.Width = InkCanvas.DefaultDrawingAttributes.Width;
                    lineStroke.DrawingAttributes.Height = InkCanvas.DefaultDrawingAttributes.Height;

                    // Add the connecting line stroke to the InkCanvas
                    InkCanvas.Strokes.Add(lineStroke);
                    firstPoint = null;
                }
            }
        }
        void UpdateElementsWithAnotace() {
            InkCanvas.Strokes.Clear();

            WriteableBitmap activeAnotaceImage = utility.GetActiveAnotaceImage();

            PreviewImage.Source = activeAnotaceImage;
            InkCanvas.Background = new ImageBrush(activeAnotaceImage);
        }

        void SaveCanvasIntoAnot() {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(InkCanvas.ActualWidth), (int)(InkCanvas.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(InkCanvas);
            utility.UpdateSelectedAnotation(new WriteableBitmap(rtb));
        }

        // Když přestaneš držet myš při kreslení tak ulož co jsi nakreslil do anotace
        void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (stateManager.CurrentState == AppState.Drawing) {
                /*
                utility.UpdateSelectedAnotation(ConvertInkCanvasToWriteableBitmap(InkCanvas));
                if(CroppedImage.Source == null){
                    PreviewImage.Source = utility.GetActiveAnotaceImage();
                }else{
                    CroppedPreviewImage.Source = utility.GetActiveAnotaceImage();
                }
                */

                // width a height a dpi by mohli dělat bordel při ukládání
                SaveCanvasIntoAnot();
                UpdateElementsWithAnotace();

                utility.SetActiveAnotaceIsAnotated(true);
                CropTButton.IsEnabled = !utility.GetIsAnotated();
            }
        }

        //Smaže obsah vybrané anotace
        void Smazat_butt(object sender, RoutedEventArgs e) {
            utility.ClearActiveAnotace();
            UpdateElementsWithAnotace();

            utility.SetActiveAnotaceIsAnotated(false);
            CropTButton.IsEnabled = !utility.GetIsAnotated();
        }

        /* Přepínání anotací */
        void SwitchActiveAnot(int id) {
            // Stroking the connection (spojení od začátku ke konci)
            ConnectStrokeAnotace();
            SaveCanvasIntoAnot();
            // The rest
            UpdateElementsWithAnotace();
            utility.ChangeActiveAnotation(id);
            //   previewImage.Source = utility.GetActiveAnotaceImage();
            InkCanvas.DefaultDrawingAttributes.Color = utility.GetActiveAnotaceColor();
            //  InkCanvas.Strokes.Clear();
            UpdateElementsWithAnotace();
        }

        void SwitchActiveAnotButton(ToggleButton pressedButton) { 

            if(activeAnotButton != null) {

                activeAnotButton.IsChecked = false;
                pressedButton.IsChecked = true;
            }
            activeAnotButton = pressedButton;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(0);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(1);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(2);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(3);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(4);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(5);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(6);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void Button_Click_8(object sender, RoutedEventArgs e) {
            SwitchActiveAnot(7);
            SwitchActiveAnotButton(sender as ToggleButton);
            e.Handled = true;
        }

        /*
         * =======================
         *  Toolbar drop and drag
         * =======================
         */

        private T GetParent<T>(DependencyObject d) where T : class {
            while (d != null && !(d is T)) {
                d = VisualTreeHelper.GetParent(d);
            }
            return d as T;

        }

        private void Grip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Point p = e.GetPosition(ToolBarTray);
            IInputElement ie = ToolBarTray.InputHitTest(p);
            grip = GetParent<Thumb>(ie as DependencyObject);
            if (grip != null) {
                isDragging = true;
                offset = e.GetPosition(ToolBarTray);
                grip.CaptureMouse();
            }
        }

        private void Grip_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (isDragging) {
                Point currentPoint = e.GetPosition(ToolBarTray);

                if (grip != null && grip.IsMouseCaptured) {
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

        private void Grip_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (grip != null) {
                isDragging = false;
                grip.ReleaseMouseCapture();
            }
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        /*
         * ======
         *  Crop
         * ======
         */

        private void CropCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (stateManager.CurrentState == AppState.Cropping) {
                cropStartPoint = e.GetPosition(CropCanvas);
                CropRectangle.Width = 0;
                CropRectangle.Height = 0;
            }
        }

        private void CropCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (stateManager.CurrentState == AppState.Cropping) {
                if (cropStartPoint.HasValue) {
                    cropStartPoint = null;
                }
            }
        }

        private void CropCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (stateManager.CurrentState == AppState.Cropping) {
                if (cropStartPoint.HasValue) {
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

        private void CropImage() {
            Int32Rect rect = new Int32Rect((int)Canvas.GetLeft(CropRectangle),
                                      (int)Canvas.GetTop(CropRectangle),
                                      (int)CropRectangle.Width,
                                      (int)CropRectangle.Height);

            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                return;

            BitmapSource croppedImage = new CroppedBitmap(ImageHolder.Source as BitmapSource, rect);
            CroppedImage.Source = croppedImage;
          
            InkCanvas.Width = CropRectangle.Width;
            InkCanvas.Height = CropRectangle.Height;
            PreviewImage.Width = CropRectangle.Width;
            PreviewImage.Height = CropRectangle.Height;
            CropCanvas.Width = CropRectangle.Width;
            CropCanvas.Height = CropRectangle.Height;

            utility.CropOriginalPicture(croppedImage);
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                CropImage();
            }

            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(DrawTButton);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CroppedImage.Source = null;

            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(DrawTButton);
        }

        /*
         * ======
         *  Zoom
         * ======
         */

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn(null, null);
                }
                else if (e.Delta < 0)
                {
                    ZoomOut(null, null);
                }
                e.Handled = true;
            }
        }

        private void ZoomIn(object sender, ExecutedRoutedEventArgs e)
        {
            if (ZoomSlider.Value < ZoomSlider.Maximum)
            {
                ZoomSlider.Value += 10;
            }
        }

        private void ZoomOut(object sender, ExecutedRoutedEventArgs e)
        {
            if (ZoomSlider.Value > ZoomSlider.Minimum)
            {
                ZoomSlider.Value -= 10;
            }
        }

        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (ImageHolder != null) {
                double zoomFactor = ZoomSlider.Value / 100;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);
            }
        }

        // Drag Move View

        private void ScrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                _isDragging = true;
                _startDragPoint = e.GetPosition(sender as UIElement);
                (sender as ScrollViewer).CaptureMouse();
                (sender as ScrollViewer).Cursor = Cursors.Hand;
            }
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var sv = sender as ScrollViewer;
                Point currentPoint = e.GetPosition(sv);
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - (currentPoint.X - _startDragPoint.X));
                sv.ScrollToVerticalOffset(sv.VerticalOffset - (currentPoint.Y - _startDragPoint.Y));
                _startDragPoint = currentPoint;
            }
        }

        private void ScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                (sender as ScrollViewer).ReleaseMouseCapture();
                (sender as ScrollViewer).Cursor = Cursors.Arrow;
            }
        }

    }

    public class PercentageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || !(value is double))
                return null;

            double numericValue = (double)value;
            int intValue = (int)Math.Round(numericValue);
            return string.Format("{0}%", intValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}