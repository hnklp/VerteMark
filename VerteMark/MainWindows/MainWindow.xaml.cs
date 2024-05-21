using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VerteMark.ObjectClasses;
using System.Windows.Ink;
using System.Globalization;
using System.Windows.Controls.Primitives;
using VerteMark.SubWindows;
using System.Windows.Shapes;
using System.Reflection;
using System.ComponentModel.DataAnnotations;



namespace VerteMark
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    /// TODO: Pridat nazev otevreneho souboru a rezimu anotator/validator do titulku aplikace
    public partial class MainWindow : Window {
        private Utility utility;
        private ToggleButton activeAnotButton;
        private ToggleButton activeToolbarButton;
        private Button plusButton;

        StateManager stateManager;

        // Toolbar drag and drop
        private bool isDragging = false;
        private Point offset;
        private Thumb grip;

        // Canvas Drag Move View
        private bool _isDragging = false;
        private Point _startDragPoint;

        // Image crop
        private Point? cropStartPoint = null;

        // Dont worry about it (canvas)
        private StylusPoint? firstPoint = null;
        private StylusPoint? lastPoint = null;

        private List<Image> previewImageList;
        private int savingParam;

        // Dotyková gesta
        private Point touchStart1;
        private Point touchStart2;
        private double initialDistance;
        private bool isPinching;

        public MainWindow() {
            InitializeComponent();
            utility = new Utility();
            previewImageList = new List<Image>();

            CommandBinding openCommandBinding = new CommandBinding(
                    ApplicationCommands.Open,
                    OpenProject_Click);
            this.CommandBindings.Add(openCommandBinding);

            // Přidání CommandBinding pro Save
            CommandBinding saveCommandBinding = new CommandBinding(
                ApplicationCommands.Save,
                Save_Click);
            this.CommandBindings.Add(saveCommandBinding);

            CreateButtons();

            User loggedInUser = utility.GetLoggedInUser();
            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "Validátor" : "Anotátor";
            ImageHolder.Source = utility.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;
            activeToolbarButton = DrawTButton;
            savingParam = 0;

            CanvasGrid.MouseEnter += CanvasGrid_MouseEnter;
            CanvasGrid.MouseLeave += CanvasGrid_MouseLeave;

            Loaded += delegate
            {
                SetCanvasComponentsSize();
                SwitchActiveAnot(0);

                // start at 25% zoom
                double zoomFactor = 0.25;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);
            };

            // zvalidneni vsech anotaci, pokud je user validator:
            if ( loggedInUser != null && loggedInUser.Validator) {
                utility.ValidateAll();
                savingParam = 2;
            }

            CanvasGrid.TouchDown += CanvasGrid_TouchDown;
            CanvasGrid.TouchMove += CanvasGrid_TouchMove;
            CanvasGrid.TouchUp += CanvasGrid_TouchUp;

        }

        private void CanvasGrid_TouchDown(object sender, TouchEventArgs e) {
            if (e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid).Count == 2) {
                var touchPoints = e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid);
                touchStart1 = touchPoints[0].Position;
                touchStart2 = touchPoints[1].Position;
                initialDistance = (touchStart1 - touchStart2).Length;
                isPinching = true;
            }
        }

        private void CanvasGrid_TouchMove(object sender, TouchEventArgs e) {
            if (isPinching && e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid).Count == 2) {
                var touchPoints = e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid);
                Point currentPoint1 = touchPoints[0].Position;
                Point currentPoint2 = touchPoints[1].Position;
                double currentDistance = (currentPoint1 - currentPoint2).Length;

                if (Math.Abs(currentDistance - initialDistance) > 10) {
                    double zoomFactor = currentDistance / initialDistance;
                    ZoomSlider.Value = Math.Min(Math.Max(ZoomSlider.Value * zoomFactor, ZoomSlider.Minimum), ZoomSlider.Maximum);
                    initialDistance = currentDistance;
                }
            }
        }

        private void CanvasGrid_TouchUp(object sender, TouchEventArgs e) {
            isPinching = false;
            initialDistance = 0;
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null) return parent;
            else return FindParent<T>(parentObject);
        }

        // Debugovací konstruktor pro volání z debug tlačítka
        public MainWindow(bool debug) {
            InitializeComponent();
            utility = new Utility();
            previewImageList = new List<Image>();
            utility.LoginUser("debug_user", true);

            CommandBinding openCommandBinding = new CommandBinding(
                    ApplicationCommands.Open,
                    OpenProject_Click);
            this.CommandBindings.Add(openCommandBinding);

            // Přidání CommandBinding pro Save
            CommandBinding saveCommandBinding = new CommandBinding(
                ApplicationCommands.Save,
                Save_Click);
            this.CommandBindings.Add(saveCommandBinding);
            User loggedInUser = utility.GetLoggedInUser();

            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "Validátor" : "Anotátor";
            utility.CreateNewProjectDEBUG();
            ImageHolder.Source = utility.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;
            activeToolbarButton = DrawTButton;
            savingParam = 2;

            Loaded += delegate {
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

        // Podle velikosti ImageHolder nastaví plátno
        private void SetCanvasComponentsSize() {
            InkCanvas.Width = ImageHolder.ActualWidth;
            InkCanvas.Height = ImageHolder.ActualHeight;
            InkCanvas.Margin = new Thickness(0);
            PreviewImage.Width = ImageHolder.ActualWidth;
            PreviewImage.Height = ImageHolder.ActualHeight;
            PreviewImage.Margin = new Thickness(0);
            CropCanvas.Width = ImageHolder.ActualWidth;
            CropCanvas.Height = ImageHolder.ActualHeight;
            CropCanvas.Margin = new Thickness(0);
            Grid.SetColumn(InkCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(InkCanvas, Grid.GetRow(ImageHolder));
            Grid.SetColumn(PreviewImage, Grid.GetColumn(ImageHolder));
            Grid.SetRow(PreviewImage, Grid.GetRow(ImageHolder));
            Grid.SetColumn(CropCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(CropCanvas, Grid.GetRow(ImageHolder));
            AddPreviewImages();
        }

        private void AddPreviewImages() {
            List<Anotace> Annotations = utility.GetAnnotationsList();

            foreach (Anotace anotace in Annotations)
            {
                AddPreviewImage();
            }
            previewImageList.Add(PreviewImage);
        }

        private void AddPreviewImage()
        {
            Image newImage = new Image();
            newImage.Width = ImageHolder.ActualWidth;
            newImage.Height = ImageHolder.ActualHeight;
            newImage.Margin = new Thickness(0);
            Grid.SetColumn(newImage, Grid.GetColumn(InkCanvas));
            Grid.SetRow(newImage, Grid.GetRow(InkCanvas));
            newImage.Stretch = Stretch.Fill;
            previewImageList.Add(newImage);
            PreviewGrid.Children.Add(newImage);
        }

        private void DeletePreviewImage(int anotaceId)
        {
            PreviewGrid.Children.Remove(previewImageList[anotaceId]);
            previewImageList.RemoveAt(anotaceId);

            if (int.TryParse(utility.GetActiveAnotaceId(), out int activeAnotaceId))
            {
                if (activeAnotaceId == anotaceId)
                {
                    SwitchActiveAnot(anotaceId - 1);

                    foreach (var child in ButtonGrid.Children)
                    {
                        if (child is ToggleButton toggleButton && toggleButton.Tag is int tag && tag == anotaceId - 1)
                        {
                            SwitchActiveAnotButton(toggleButton);
                            break;
                        }
                    }
                }
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e) {
            SaveAlertWindow saveAlertWindow = new SaveAlertWindow(this, utility.GetLoggedInUser().Validator);

            if (utility.saved) {
                saveAlertWindow.Browse();
            }
            else {
                double originalCenterX = Left + Width / 2;
                double originalCenterY = Top + Height / 2;

                saveAlertWindow.Left = originalCenterX - saveAlertWindow.Width / 2;
                saveAlertWindow.Top = originalCenterY - saveAlertWindow.Height / 2;

                saveAlertWindow.Show();
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

                    ValidateLabel.Visibility = Visibility.Visible;
                    ButtonGrid.Visibility = Visibility.Visible;

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

                    ValidateLabel.Visibility = Visibility.Collapsed;
                    ButtonGrid.Visibility = Visibility.Collapsed;

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

        private void SwitchActiveToolbarButton(ToggleButton pressedButton) {
            if (activeToolbarButton != null) {
                activeToolbarButton.IsChecked = false;
                pressedButton.IsChecked = true;
            }
            activeToolbarButton = pressedButton;
        }


        private void CanvasGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            CanvasGrid.Cursor = stateManager.CurrentState == AppState.Drawing ? Cursors.Pen :
                                    Cursors.Cross;

        }

        private void CanvasGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            CanvasGrid.Cursor = Cursors.Arrow; // nebo jiný výchozí kurzor podle vašich preferencí
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
            JustSaveAlertWindow saveAlertWindow = new JustSaveAlertWindow(this, utility.GetLoggedInUser().Validator);

            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            saveAlertWindow.Left = originalCenterX - saveAlertWindow.Width / 2;
            saveAlertWindow.Top = originalCenterY - saveAlertWindow.Height / 2;


            saveAlertWindow.Show();
        }

        /*
         * =========
         *  Drawing
         * =========
         */

        //Spojování linky
        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e) {
            if (firstPoint == null) {
                firstPoint = e.Stroke.StylusPoints.First();
            }
            lastPoint = e.Stroke.StylusPoints.Last();
        }

        private void ConnectStrokeAnotace() {
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
        private void UpdateElementsWithAnotace() {
            InkCanvas.Strokes.Clear();

            WriteableBitmap activeAnotaceImage = utility.GetActiveAnotaceImage();

            InkCanvas.Background = new ImageBrush(activeAnotaceImage);
        }

        private void SaveCanvasIntoAnot() {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(InkCanvas.ActualWidth), (int)(InkCanvas.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(InkCanvas);
            utility.UpdateSelectedAnotation(new WriteableBitmap(rtb));
        }

        // Když přestaneš držet myš při kreslení tak ulož co jsi nakreslil do anotace
        private void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (stateManager.CurrentState == AppState.Drawing) {
                // width a height a dpi by mohli dělat bordel při ukládání
                SaveCanvasIntoAnot();
                UpdateElementsWithAnotace();

                utility.SetActiveAnotaceIsAnotated(true);
                ToggleCropButton(!utility.GetIsAnotated());
            }
        }

        private void ToggleCropButton(bool isEnabled)
        {
            CropTButton.IsEnabled = isEnabled;
            CropTButton.Opacity = isEnabled ? 1 : 0.5;
        }

        //Smaže obsah vybrané anotace
        private void Delete_butt(object sender, RoutedEventArgs e) {
            utility.ClearActiveAnotace();
            UpdateElementsWithAnotace();

            utility.SetActiveAnotaceIsAnotated(false);
            ToggleCropButton(!utility.GetIsAnotated());
        }

        /* Ukázka všech anotací */
        private void PreviewAllAnotaces() {
            if(ImageHolder.Source != null) {
                List<WriteableBitmap> bitmaps = utility.AllInactiveAnotaceImages();
                for(int i = 0; i < bitmaps.Count; i++) {
                    previewImageList[i].Source = bitmaps[i];
                    previewImageList[i].Opacity = 0.5;
                }
            }
        }


        /*
         * =========
         *  Buttons
         * =========
         */

        private void SwitchActiveAnot(int id) {
            // Stroking the connection (spojení od začátku ke konci)
        //    ConnectStrokeAnotace();
            SaveCanvasIntoAnot();
            // The rest
            UpdateElementsWithAnotace();
            utility.ChangeActiveAnotation(id);
            //   previewImage.Source = utility.GetActiveAnotaceImage();
            InkCanvas.DefaultDrawingAttributes.Color = utility.GetActiveAnotaceColor();
            //  InkCanvas.Strokes.Clear();
            UpdateElementsWithAnotace();

            PreviewAllAnotaces();
        }

        private void SwitchActiveAnotButton(ToggleButton pressedButton) { 

            if(activeAnotButton != null) {

                activeAnotButton.IsChecked = false;
            }
            pressedButton.IsChecked = true;
            activeAnotButton = pressedButton;
        }

        private void CreateButtons()
        {
            List<Anotace> Annotations = utility.GetAnnotationsList();
            bool isValidator = utility.GetLoggedInUser().Validator;

            int i = 0;
            foreach(Anotace anotace in Annotations)
            {
                AddNewRow(anotace, isValidator, i);
                i++;
            }

            Button plusButton = new Button
            {
                Content = "+",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 20,
                Height = 20,
                Margin = new Thickness(109, 5 + 30 * i, 0, 0),
            };
            plusButton.Click += (sender, e) => PlusButton_Click(sender, e);
            this.plusButton = plusButton;

            ButtonGrid.Children.Add(plusButton);

        }

        private void AddNewRow(Anotace anotace, bool isValidator, int i)
        {
            Brush color = new SolidColorBrush(Color.FromArgb(anotace.Color.A, anotace.Color.R, anotace.Color.G, anotace.Color.B));

            if (i > 7)
            {
                Button minusButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 19,
                    Height = 19,
                    Margin = new Thickness(20, 5 + 30 * i, 0, 0),
                    Tag = i
                };
                Image binIcon = new Image
                {
                    Source = new BitmapImage(new Uri("../Resources/Icons/bin_icon.ico", UriKind.Relative)),
                    Width = 20,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                minusButton.Content = binIcon;
                minusButton.Click += (sender, e) => MinusButton_Click(sender, e);

                ButtonGrid.Children.Add(minusButton);
            }

            else
            {
                Rectangle rect = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    Fill = color,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 19,
                    Height = 19,
                    Margin = new Thickness(20, 5 + 30 * i, 0, 0),
                    Tag = i
                };
                ButtonGrid.Children.Add(rect);
            }

            ToggleButton toggleButton = new ToggleButton
            {
                Content = anotace.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 125,
                Height = 20,
                Margin = new Thickness(60, 5 + 30 * i, 0, 0),
                Tag = i
            };
            toggleButton.Click += (sender, e) => Button_Click(sender, e);

            if (i == 0)
                SwitchActiveAnotButton(toggleButton);

            CheckBox checkBox = new CheckBox {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 20,
                Height = 20,
                Margin = new Thickness(205, 6 + 30 * i, 0, 0),
                Tag = i,
                IsEnabled = isValidator,
                IsChecked = anotace.IsValidated
        };
            checkBox.Checked += SwitchValidation_Check;
            checkBox.Unchecked += SwitchValidation_Check;

            ButtonGrid.Children.Add(toggleButton);
            ButtonGrid.Children.Add(checkBox);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if(toggleButton != null && toggleButton.Tag != null)
            {
                int index;
                if (int.TryParse(toggleButton.Tag.ToString(), out index))
                {
                    SwitchActiveAnot(index);
                    SwitchActiveAnotButton(sender as ToggleButton);
                    e.Handled = true;
                }
            }
        }

        private void SwitchValidation_Check(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                int index;
                if (int.TryParse(checkBox.Tag.ToString(), out index))
                {
                    utility.SwitchAnotationValidation(index);
                }
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValidator = utility.GetLoggedInUser().Validator;
            Anotace implant = utility.CreateImplantAnnotation();
            AddPreviewImage();

            List<Anotace> Annotations = utility.GetAnnotationsList();
            AddNewRow(implant, isValidator, Annotations.Count - 1);
            MovePlusButton();
        }

        private void MovePlusButton(bool down = true)
        {
            if (down)
            {
                plusButton.Margin = new Thickness(plusButton.Margin.Left, plusButton.Margin.Top + 30, plusButton.Margin.Right, plusButton.Margin.Bottom);
            }
            else
            {
                plusButton.Margin = new Thickness(plusButton.Margin.Left, plusButton.Margin.Top - 30, plusButton.Margin.Right, plusButton.Margin.Bottom);
            }
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int index;
                if (int.TryParse(button.Tag.ToString(), out index))
                {
                    DeleteRow(index);
                    utility.DeleteAnnotation(index);
                    DeletePreviewImage(index);
                    MovePlusButton(false);
                }
            }
        }

        private void DeleteRow(int i)
        {
            // Najdeme všechny prvky s tagem `i` a odstraníme je z `ButtonGrid`
            var elementsToRemove = ButtonGrid.Children.OfType<UIElement>().Where(e => e is FrameworkElement fe && fe.Tag is int tag && tag == i).ToList();

            foreach (var element in elementsToRemove)
            {
                ButtonGrid.Children.Remove(element);
            }

            // Aktualizujeme pozice, tagy a obsah všech prvků po odstranění řádku
            foreach (var element in ButtonGrid.Children)
            {
                if (element is FrameworkElement fe && fe.Tag is int tag && tag > i)
                {
                    fe.Tag = tag - 1;

                    // Upravíme margin pro zachování správného rozložení
                    if (fe.Margin != null)
                    {
                        fe.Margin = new Thickness(fe.Margin.Left, fe.Margin.Top - 30, fe.Margin.Right, fe.Margin.Bottom);
                    }

                    // Aktualizujeme obsah ToggleButton, pokud je to ToggleButton
                    if (element is ToggleButton toggleButton)
                    {
                        toggleButton.Content = $"Implantát {tag - 7}";
                    }

                    utility.ChangeAnnotationId(tag);
                }
            }
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
            System.Windows.Point p = e.GetPosition(ToolBarTray);
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
                System.Windows.Point currentPoint = e.GetPosition(ToolBarTray);

                if (grip != null && grip.IsMouseCaptured) {
                    System.Windows.Point newPosition = Mouse.GetPosition(this);
                    int toolbarOffset = 18;
                    double newX = newPosition.X - offset.X;
                    double newY = newPosition.Y - offset.Y - toolbarOffset;

                    // Ensure the ToolBarTray stays within the bounds of the window
                    newX = Math.Max(0, Math.Min(newX, Grid.ColumnDefinitions[0].ActualWidth  + Grid.ColumnDefinitions[1].ActualWidth - ToolBarTray.ActualWidth));
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
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpi = 96.0;
            double dpiX = dpi;
            double dpiY = dpi;

            if (source != null)
            {
                dpiX = dpi * source.CompositionTarget.TransformToDevice.M11;
                dpiY = dpi * source.CompositionTarget.TransformToDevice.M22;
            }

            double scaleFactorX = dpiX / dpi;
            double scaleFactorY = dpiY / dpi;

            Int32Rect rect = new Int32Rect(
                (int)(Canvas.GetLeft(CropRectangle) * scaleFactorX),
                (int)(Canvas.GetTop(CropRectangle) * scaleFactorY),
                (int)(CropRectangle.Width * scaleFactorX),
                (int)(CropRectangle.Height * scaleFactorY)
            );

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
            foreach(Image img in previewImageList) {
                img.Width = CropRectangle.Width;
                img.Height = CropRectangle.Height;
                PreviewGrid.HorizontalAlignment = HorizontalAlignment.Left;
                PreviewGrid.VerticalAlignment = VerticalAlignment.Top;
            }

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

        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider.Value < ZoomSlider.Maximum)
            {
                ZoomSlider.Value += 10;
            }
        }

        private void ZoomOut(object sender, RoutedEventArgs e)
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


        /*
         * ================
         *  Drag Move View
         * ================
         */


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
                System.Windows.Point currentPoint = e.GetPosition(sv);
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