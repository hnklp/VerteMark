using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;
using VerteMark.SubWindows;
using Windows.System.UserProfile;
using static VerteMark.ObjectClasses.Anotace;

namespace VerteMark
{
    /// <summary>
    /// Hlavní okno aplikace pro anotaci rentgenových snímků.
    /// Poskytuje nástroje pro kreslení anotací, práci s obratli, implantáty a fúzemi.
    /// </summary>
    /// 
    /// TODO: Pridat nazev otevreneho souboru a rezimu anotator/validator do titulku aplikace
    public partial class MainWindow : Window
    {
        private Project project;
        private ToggleButton activeAnotButton;
        private ToggleButton activeToolbarButton;
        private Button plusButton;

        StateManager stateManager;

        // Toolbar drag and drop
        private bool isDragging = false;
        private System.Windows.Point offset;
        private Thumb grip;

        // Canvas Drag Move View
        private bool _isDragging = false;
        private System.Windows.Point _startDragPoint;

        // Image crop
        private System.Windows.Point? cropStartPoint = null;

        // Dont worry about it (canvas)
        private StylusPoint? firstPoint = null;
        private StylusPoint? lastPoint = null;

        private int savingParam;

        // Dotyková gesta
        private System.Windows.Point touchStart1;
        private System.Windows.Point touchStart2;
        private double initialDistance;
        private bool isPinching;

        /// <summary>
        /// Vytvoří novou instanci MainWindow a inicializuje projekt, uživatele a UI komponenty.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            project = Project.GetInstance();

            // Přidání CommandBinding pro Open (Ctrl+O)
            CommandBinding openCommandBinding = new CommandBinding(
                ApplicationCommands.Open,
                OpenProject_Click);
            this.CommandBindings.Add(openCommandBinding);

            // Přidání CommandBinding pro Save (Ctrl+S)
            CommandBinding saveCommandBinding = new CommandBinding(
                ApplicationCommands.Save,
                Save_Click);
            this.CommandBindings.Add(saveCommandBinding);

            // Přidání CommandBinding pro Close (Ctrl+Q)
            CommandBinding closeCommandBinding = new CommandBinding(
                ApplicationCommands.Close,
                CloseItem_Click);
            this.CommandBindings.Add(closeCommandBinding);

            // CommandBinding pro Undo (Ctrl+Z)
            CommandBinding undoCommandBinding = new CommandBinding(
                ApplicationCommands.Undo,
                UndoLastPoint);
            this.CommandBindings.Add(undoCommandBinding);

            User loggedInUser = project.GetLoggedInUser();
            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "Validátor" : "Anotátor";
            FileName.Text = project.folderUtilityManager.fileManager.fileName;
            ImageHolder.Source = project.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;
            activeToolbarButton = DrawTButton;
            savingParam = 0;

            CanvasGrid.MouseEnter += CanvasGrid_MouseEnter;
            CanvasGrid.MouseLeave += CanvasGrid_MouseLeave;

            // zvalidneni vsech anotaci, pokud je user validator:
            if (loggedInUser != null && loggedInUser.Validator)
            {
                project.ValidateAll();
                savingParam = 2;
            }

            CreateButtons();

            CanvasGrid.TouchDown += CanvasGrid_TouchDown;
            CanvasGrid.TouchMove += CanvasGrid_TouchMove;
            CanvasGrid.TouchUp += CanvasGrid_TouchUp;

            CanvasScrollViewer.ManipulationDelta += CustomScrollViewer_ManipulationDelta; // Upravený řádek
            CanvasScrollViewer.ManipulationInertiaStarting += CustomScrollViewer_ManipulationInertiaStarting; // Upravený řádek

            Loaded += delegate
            {
                SetCanvasComponentsSize();
                AddPreviewImages();
                SwitchActiveAnot("V0");
                LoadPointMarkers();

                // start at 25% zoom
                double zoomFactor = 0.25;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);

                if (project.IsReadOnly())
                {
                    stateManager.CurrentState = AppState.ReadOnly;

                    MessageBox.Show(
                        "Tento DICOM již byl validován. Je možné pouze prohlížení.",
                        "Režim prohlížení",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                ToggleCropButton(!project.GetIsAnotated());
            };
        }

        private void CanvasGrid_TouchDown(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid).Count == 2)
            {
                var touchPoints = e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid);
                touchStart1 = touchPoints[0].Position;
                touchStart2 = touchPoints[1].Position;
                initialDistance = (touchStart1 - touchStart2).Length;
                isPinching = true;
            }
        }

        private void CanvasGrid_TouchMove(object sender, TouchEventArgs e)
        {
            if (isPinching && e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid).Count == 2)
            {
                var touchPoints = e.TouchDevice.GetIntermediateTouchPoints(CanvasGrid);
                System.Windows.Point currentPoint1 = touchPoints[0].Position;
                System.Windows.Point currentPoint2 = touchPoints[1].Position;
                double currentDistance = (currentPoint1 - currentPoint2).Length;

                if (Math.Abs(currentDistance - initialDistance) > 10)
                {
                    double zoomFactor = currentDistance / initialDistance;
                    ZoomSlider.Value = Math.Min(Math.Max(ZoomSlider.Value * zoomFactor, ZoomSlider.Minimum), ZoomSlider.Maximum);
                    initialDistance = currentDistance;
                }
            }
        }

        private void CanvasGrid_TouchUp(object sender, TouchEventArgs e)
        {
            isPinching = false;
            initialDistance = 0;
        }

        private void CustomScrollViewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.DeltaManipulation.Translation.X);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.DeltaManipulation.Translation.Y);
                e.Handled = true;
            }
        }

        private void CustomScrollViewer_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 10 * 96.0 / (1000.0 * 1000.0); // example value
            e.Handled = true;
        }

        /// <summary>
        /// Debugovací konstruktor pro testování bez DICOM souboru.
        /// </summary>
        /// <param name="debug">True pro aktivaci debug režimu</param>
        public MainWindow(bool debug)
        {
            InitializeComponent();
            project = new Project();
            project.LoginNewUser("debug_user", true);

            CommandBinding openCommandBinding = new CommandBinding(
                    ApplicationCommands.Open,
                    OpenProject_Click);
            this.CommandBindings.Add(openCommandBinding);

            // Přidání CommandBinding pro Save
            CommandBinding saveCommandBinding = new CommandBinding(
                ApplicationCommands.Save,
                Save_Click);
            this.CommandBindings.Add(saveCommandBinding);
            User loggedInUser = project.GetLoggedInUser();

            UserIDStatus.Text = "ID: " + loggedInUser.UserID.ToString();
            RoleStatus.Text = loggedInUser.Validator ? "Validátor" : "Anotátor";
            project.CreateNewProjectDEBUG();
            ImageHolder.Source = project.GetOriginalPicture() ?? ImageHolder.Source; // Pokud og picture není null tak ho tam dosad
            stateManager = new StateManager();
            stateManager.StateChanged += HandleStateChanged;
            activeToolbarButton = DrawTButton;
            savingParam = 2;

            Loaded += delegate
            {
                SetCanvasComponentsSize();
                SwitchActiveAnot("V0");

                // start at 25% zoom
                double zoomFactor = 0.25;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);
            };
        }


        /// <summary>
        /// Nastaví velikost všech canvas komponent podle velikosti ImageHolder.
        /// </summary>
        private void SetCanvasComponentsSize()
        {
            InkCanvas.Width = ImageHolder.ActualWidth;
            InkCanvas.Height = ImageHolder.ActualHeight;
            InkCanvas.Margin = new Thickness(0);
            PreviewImage.Width = ImageHolder.ActualWidth;
            PreviewImage.Height = ImageHolder.ActualHeight;
            PreviewImage.Margin = new Thickness(0);
            CropCanvas.Width = ImageHolder.ActualWidth;
            CropCanvas.Height = ImageHolder.ActualHeight;
            CropCanvas.Margin = new Thickness(0);
            PointCanvas.Width = ImageHolder.ActualWidth;
            PointCanvas.Height = ImageHolder.ActualHeight;
            PointCanvas.Margin = new Thickness(0);
            Grid.SetColumn(InkCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(InkCanvas, Grid.GetRow(ImageHolder));
            Grid.SetColumn(PreviewImage, Grid.GetColumn(ImageHolder));
            Grid.SetRow(PreviewImage, Grid.GetRow(ImageHolder));
            Grid.SetColumn(CropCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(CropCanvas, Grid.GetRow(ImageHolder));
            Grid.SetColumn(PointCanvas, Grid.GetColumn(ImageHolder));
            Grid.SetRow(PointCanvas, Grid.GetRow(ImageHolder));
        }

        /// <summary>
        /// Přidá preview obrázky pro všechny anotace.
        /// </summary>
        private void AddPreviewImages()
        {
            List<Anotace> Annotations = project.GetAnotaces();

            foreach (Anotace anot in Annotations)
            {
                AddPreviewImage(anot);
            }
        }

        /// <summary>
        /// Přidá preview obrázek pro zadanou anotaci.
        /// </summary>
        /// <param name="anotace">Anotace, pro kterou se má vytvořit preview obrázek</param>
        private void AddPreviewImage(Anotace anotace)
        {
            Image newImage = new Image();

            if (CroppedImage.Source == null)
            {
                newImage.Width = ImageHolder.ActualWidth;
                newImage.Height = ImageHolder.ActualHeight;
            }
            else
            {
                newImage.Width = CropRectangle.Width;
                newImage.Height = CropRectangle.Height;
            }

            newImage.Margin = new Thickness(0);
            Grid.SetColumn(newImage, Grid.GetColumn(InkCanvas));
            Grid.SetRow(newImage, Grid.GetRow(InkCanvas));
            newImage.Stretch = Stretch.Fill;
            anotace.PreviewImage = newImage;
            PreviewGrid.Children.Add(newImage);
        }

        /// <summary>
        /// Odstraní preview obrázek pro zadanou anotaci.
        /// </summary>
        /// <param name="anotaceId">ID anotace, jejíž preview obrázek se má odstranit</param>
        private void DeletePreviewImage(string anotaceId)
        {
            var anot = project.FindAnotaceById(anotaceId);
            if (anot?.PreviewImage != null)
            {
                PreviewGrid.Children.Remove(anot.PreviewImage);
                anot.PreviewImage = null;
            }

            string activeAnotaceId = project.ActiveAnotaceId();
            if (activeAnotaceId == anotaceId)
            {
                string prefix = anotaceId.Substring(0, 1); // "V", "I", "F"
                if (int.TryParse(anotaceId.Substring(1), out int index) && index > 0)
                {
                    string previousId = prefix + (index - 1);
                    SwitchActiveAnot(previousId);

                    foreach (var child in ButtonGrid.Children)
                    {
                        if (child is ToggleButton toggleButton && toggleButton.Tag is string tag && tag == previousId)
                        {
                            SwitchActiveAnotButton(toggleButton);
                            break;
                        }
                    }
                }
                else
                {
                    SwitchActiveAnotButton(null);
                }
            }
        }


        /// <summary>
        /// Obsluha kliknutí na otevření projektu (Ctrl+O nebo menu).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = sender is Button button ? button.Name : "Unknown";

            JustSaveAlertWindow saveAlertWindow = new JustSaveAlertWindow(this, project.GetLoggedInUser().Validator, false, buttonName);

            if (project.saved)
            {
                double originalCenterX = Left + Width / 2;
                double originalCenterY = Top + Height / 2;

                saveAlertWindow.Left = originalCenterX - saveAlertWindow.Width / 2;
                saveAlertWindow.Top = originalCenterY - saveAlertWindow.Height / 2;

                saveAlertWindow.Browse(false);
            }
            else
            {
                double originalCenterX = Left + Width / 2;
                double originalCenterY = Top + Height / 2;

                saveAlertWindow.Left = originalCenterX - saveAlertWindow.Width / 2;
                saveAlertWindow.Top = originalCenterY - saveAlertWindow.Height / 2;

                saveAlertWindow.Show();
            }

            ToggleCropButton(!project.GetIsAnotated());
        }

        /// <summary>
        /// Zobrazí dialog s informacemi o nahlášení chyby.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ReportItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nalezli jste chybu v aplikaci?" + "\n" + "Napište nám prosím mail na software@digitech.ujep.cz" + "\n" + "\n" + "Jako předmět uveďte BUG - VerteMark - Krátký popis chyby" + "\n" + "Do zprávy napište podrobný popis chyby a pokud víte, tak postup jak ji můžeme zreplikovat." + "\n" + "\n" + "Děkujeme za spolupráci!", "Nahlásit chybu");
        }

        /*
         * ============
         *  App states
         * ============
         */

        /// <summary>
        /// Obsluha změny stavu aplikace - aktualizuje UI podle nového stavu.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="newState">Nový stav aplikace</param>
        private void HandleStateChanged(object sender, AppState newState)
        {

            switch (newState)
            {
                case AppState.ReadOnly:

                    CropTButton.IsEnabled = false;
                    DrawTButton.IsEnabled = false;
                    MirrorTButton.IsEnabled = false;
                    delete.IsEnabled = false;
                    ButtonGrid.IsEnabled = false;
                    InvalidButtonUI.IsEnabled = false;
                    DiscardButtonUI.IsEnabled = false;
                    PointCanvas.IsEnabled = false;

                    ReadOnlySeparator.Visibility = Visibility.Visible;
                    ReadOnlyText.Visibility = Visibility.Visible;

                    goto case AppState.Drawing;

                case AppState.Drawing:

                    SetInkCanvasMode(newState);
                    CropCanvas.Visibility = Visibility.Hidden;
                    CropLabel.Visibility = Visibility.Collapsed;
                    CropConfirmButton.Visibility = Visibility.Collapsed;
                    CropCancelButton.Visibility = Visibility.Collapsed;

                    ValidateLabel.Visibility = Visibility.Visible;
                    ButtonGrid.Visibility = Visibility.Visible;

                    if (CroppedImage.Source != null)
                    {
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

                    if (CroppedImage.Source != null)
                    {
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

        private void SetInkCanvasMode(AppState state)
        {
            InkCanvas.EditingMode = state == AppState.Drawing ? InkCanvasEditingMode.Ink :
                                    InkCanvasEditingMode.None;
        }

        private void CropTButton_Click(object sender, RoutedEventArgs e)
        {
            stateManager.CurrentState = AppState.Cropping;
            SwitchActiveToolbarButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void DrawTButton_Click(object sender, RoutedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                CropImage();
            }
            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(sender as ToggleButton);
            e.Handled = true;
        }

        private void SwitchActiveToolbarButton(ToggleButton pressedButton)
        {
            if (activeToolbarButton != null)
            {
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

        /// <summary>
        /// Otevře okno s informacemi o aplikaci.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
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

        /// <summary>
        /// Obsluha zavření aplikace (Ctrl+Q nebo menu).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void CloseItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Obsluha uložení projektu (Ctrl+S nebo menu).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = sender is Button button ? button.Name : "Unknown";

            JustSaveAlertWindow saveAlertWindow = new JustSaveAlertWindow(this, project.GetLoggedInUser().Validator, true, buttonName);

            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            saveAlertWindow.Left = originalCenterX - saveAlertWindow.Width / 2;
            saveAlertWindow.Top = originalCenterY - saveAlertWindow.Height / 2;

            saveAlertWindow.Show();
        }


        /// <summary>
        /// Označí projekt jako neplatný a uloží ho do složky invalid.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Invalid_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            project.SaveProject(3, button.Name);
            project.saved = true;
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(true);

            folderbrowserWindow.oldMainWindow = this;

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;

            this.Visibility = Visibility.Hidden;
            folderbrowserWindow.Show();

            this.Close();

        }

        /// <summary>
        /// Zrcadlí původní obrázek horizontálně.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Mirror_Click(object sender, RoutedEventArgs e)
        {

            //Zešednutí tlačítka po anotaci se provádí společně s cropem v ToggleCropButton

            if (project.GetOriginalPicture() != null)
            {
                project.MirrorOriginalPicture();

                if (CroppedImage.Source != null)
                {
                    CroppedImage.Source = project.GetOriginalPicture();
                }
                else
                {
                    ImageHolder.Source = project.GetOriginalPicture();
                }
    
                // Update sizes
                SetCanvasComponentsSize();
                UpdateElementsWithAnotace();
                LoadPointMarkers();
            }
            else
            {
                MessageBox.Show("Obrázek není načtený. Pokud vidíte tento dialog, napište nám prosím na software@digitech.ujep.cz a do předmětu napiště MIRROR - NOT LOADED a do zprávy postup, jak chybu reprodukovat. Děkujeme.", "Chyba - MIRROR - NOT LOADED", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obsluha zavírání okna - smaže dočasnou složku projektu.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void DeleteTempFolder_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            project.DeleteTempFolder();
        }

        /// <summary>
        /// Odstraní poslední bod aktivní anotace (Ctrl+Z).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void UndoLastPoint(object sender, ExecutedRoutedEventArgs e)
        {
            if (stateManager.CurrentState != AppState.Drawing)
                return;

            int pointsCount = project.GetPointsCount();
            if (pointsCount <= 0) return;

            var activeAnotaceIdString = project.ActiveAnotaceId();
            if (int.TryParse(activeAnotaceIdString, out int activeIndex))
            {
                var allAnnots = project.GetAnotaces();
                if (activeIndex >= 0 && activeIndex < allAnnots.Count)
                {
                    var anot = allAnnots[activeIndex];
                    if (anot.Type == AnotaceType.Implant || anot.Type == AnotaceType.Fusion)
                    {
                        return;
                    }
                }
            }

            project.RemoveActiveLastPoint(PointCanvas);

            project.SetActiveAnotaceIsAnotated(pointsCount - 1 > 0);
            ToggleCropButton(!project.GetIsAnotated());

            project.UpdatePointsScale(ZoomSlider.Value / 100);
        }

        /*
         * =========
         *  Drawing
         * =========
         */

        /// <summary>
        /// Obsluha dokončení tahu na InkCanvas - zaznamená první a poslední bod pro případné spojení.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (firstPoint == null)
            {
                firstPoint = e.Stroke.StylusPoints.First();
            }
            lastPoint = e.Stroke.StylusPoints.Last();
        }

        /// <summary>
        /// Spojí první a poslední bod tahu, pokud jsou blízko sebe (méně než 100 pixelů).
        /// </summary>
        private void ConnectStrokeAnotace()
        {
            if (stateManager != null && stateManager.CurrentState == AppState.Drawing && firstPoint != null && lastPoint != null)
            {
                // Calculate the distance between the first and last points
                double distance = Math.Sqrt(Math.Pow(lastPoint.Value.X - firstPoint.Value.X, 2) + Math.Pow(lastPoint.Value.Y - firstPoint.Value.Y, 2));

                // If the distance is less than 100, connect the points with a line
                if (distance < 100)
                {
                    // Create a new stroke for the connecting line
                    StylusPointCollection points = new StylusPointCollection();
                    points.Add(firstPoint.Value);
                    points.Add(lastPoint.Value);
                    Stroke lineStroke = new Stroke(points);

                    // Set the color and thickness of the connecting line
                    lineStroke.DrawingAttributes.Color = project.ActiveAnotaceColor();
                    lineStroke.DrawingAttributes.Width = InkCanvas.DefaultDrawingAttributes.Width;
                    lineStroke.DrawingAttributes.Height = InkCanvas.DefaultDrawingAttributes.Height;

                    // Add the connecting line stroke to the InkCanvas
                    InkCanvas.Strokes.Add(lineStroke);
                    firstPoint = null;
                }
            }
        }
        /// <summary>
        /// Aktualizuje InkCanvas s obsahem aktivní anotace.
        /// </summary>
        private void UpdateElementsWithAnotace()
        {
            InkCanvas.Strokes.Clear();

            WriteableBitmap activeAnotaceImage = project.ActiveAnotaceImage();
            InkCanvas.Background = new ImageBrush(activeAnotaceImage);
        }

        /// <summary>
        /// Uloží obsah InkCanvas do aktivní anotace jako bitmapu.
        /// </summary>
        private void SaveCanvasIntoAnot()
        {
            var dpi = VisualTreeHelper.GetDpi(InkCanvas);
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)(InkCanvas.ActualWidth * dpi.DpiScaleX),
                (int)(InkCanvas.ActualHeight * dpi.DpiScaleY),
                dpi.PixelsPerInchX,
                dpi.PixelsPerInchY,
                PixelFormats.Pbgra32
            );

            rtb.Render(InkCanvas);
            project.UpdateSelectedAnotaceCanvas(new WriteableBitmap(rtb));
        }

        /// <summary>
        /// Obsluha uvolnění myši při kreslení - uloží nakreslený obsah do aktivní anotace.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Drawing)
            {
                // width a height a dpi by mohli dělat bordel při ukládání
                SaveCanvasIntoAnot();
                UpdateElementsWithAnotace();

                project.SetActiveAnotaceIsAnotated(true);
                ToggleCropButton(!project.GetIsAnotated());
            }
        }

        /// <summary>
        /// Povolí/zakáže tlačítka pro ořezávání a zrcadlení podle stavu anotace.
        /// </summary>
        /// <param name="isEnabled">True pro povolení tlačítek, false pro zakázání</param>
        private void ToggleCropButton(bool isEnabled)
        {
            CropTButton.IsEnabled = isEnabled;
            CropTButton.Opacity = isEnabled ? 1 : 0.5;
            MirrorTButton.IsEnabled = isEnabled;
            MirrorTButton.Opacity = isEnabled ? 1 : 0.5;
        }

        /// <summary>
        /// Smaže obsah aktivní anotace (body, čáry a canvas).
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Delete_button(object sender, RoutedEventArgs e)
        {
            project.RemoveActivePointsAndConnections(PointCanvas);
            project.ClearActiveAnotace();
            UpdateElementsWithAnotace();

            project.SetActiveAnotaceIsAnotated(false);
            ToggleCropButton(!project.GetIsAnotated());
        }

        /// <summary>
        /// Zahodí všechny změny provedené na aktuálním snímku po potvrzení uživatele.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Opravdu chcete zahodit veškeré změny provedené na aktuálním snímku?",
                    "Zahodit změny",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                project.ClearAllAnotace(PointCanvas);
                UpdateElementsWithAnotace();
                ToggleCropButton(true);
                project.PreviewAllAnotaces();
            }
        }

        /*
         * =========
         *  Buttons
         * =========
         */

        /// <summary>
        /// Přepne aktivní anotaci podle ID a aktualizuje UI.
        /// </summary>
        /// <param name="id">ID anotace k aktivaci</param>
        private void SwitchActiveAnot(string id)
        {
            SaveCanvasIntoAnot();
            var anotace = project.SelectActiveAnotace(id);

            if (anotace.Type == AnotaceType.Vertebra)
            {
                // pokud chci bodovat => PointCanvas
                PointCanvas.IsHitTestVisible = true;
            }
            else
            {
                // pokud chci kreslit => InkCanvas 
                PointCanvas.IsHitTestVisible = false;
            }

            InkCanvas.DefaultDrawingAttributes.Color = project.ActiveAnotaceColor();
            UpdateElementsWithAnotace();
            project.PreviewAllAnotaces();
        }

        /// <summary>
        /// Přepne aktivní tlačítko anotace v UI.
        /// </summary>
        /// <param name="pressedButton">Tlačítko, které se má aktivovat</param>
        private void SwitchActiveAnotButton(ToggleButton pressedButton)
        {
            if (activeAnotButton != null)
            {
                activeAnotButton.IsChecked = false;
            }
            pressedButton.IsChecked = true;
            activeAnotButton = pressedButton;
        }

        /// <summary>
        /// Vytvoří tlačítka pro všechny anotace v projektu podle jejich typu.
        /// </summary>
        private void CreateButtons()
        {
            List<Anotace> annotations = project.GetAnotaces();
            bool isValidator = project.GetLoggedInUser().Validator;

            // Rozdělení podle typu
            var vertebrae = annotations.Where(a => a.Type == Anotace.AnotaceType.Vertebra).ToList();
            var fusions = annotations.Where(a => a.Type == Anotace.AnotaceType.Fusion).ToList();
            var implants = annotations.Where(a => a.Type == Anotace.AnotaceType.Implant).ToList();

            int rowIndex = 0;

            // Vertebra tlačítka
            foreach (var anotace in vertebrae)
            {
                AddNewRow(anotace, isValidator, rowIndex++);
            }

            // Fúze (jeden button pro všechny fúze)
            foreach (var fusion in fusions)
            {
                AddNewRow(fusion, isValidator, rowIndex++);
            }

            // Plus pro fúzi
            AddPlusButton(rowIndex++, Anotace.AnotaceType.Fusion);

            // Implantáty
            foreach (var implant in implants)
            {
                AddNewRow(implant, isValidator, rowIndex++);
            }

            // Plus pro implantát
            AddPlusButton(rowIndex++, Anotace.AnotaceType.Implant);
        }

        /// <summary>
        /// Přidá tlačítko plus pro vytvoření nové anotace zadaného typu.
        /// </summary>
        /// <param name="rowIndex">Index řádku pro umístění tlačítka</param>
        /// <param name="type">Typ anotace (Fusion nebo Implant)</param>
        private void AddPlusButton(int rowIndex, Anotace.AnotaceType type)
        {
            Button plusButton = new Button
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(109, 10 + 42 * rowIndex, 0, 0),
                FontFamily = new FontFamily("Segoe UI Regular"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0),
                Tag = type // zapamatujeme si, jaký typ má přidávat
            };

            Image plusIcon = new Image
            {
                Source = new BitmapImage(new Uri("../Resources/Icons/plus_icon.ico", UriKind.Relative)),
                Width = 26,
                Height = 26,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            plusButton.Content = plusIcon;
            plusButton.Click += PlusButton_Click;

            ButtonGrid.Children.Add(plusButton);
        }
        
        /// <summary>
        /// Přidá nový řádek s tlačítkem a checkboxem pro zadanou anotaci.
        /// </summary>
        /// <param name="anotace">Anotace, pro kterou se má vytvořit řádek</param>
        /// <param name="isValidator">True, pokud je uživatel validátor</param>
        /// <param name="rowIndex">Index řádku pro umístění prvků</param>
        private void AddNewRow(Anotace anotace, bool isValidator, int rowIndex)
        {
            Brush color = new SolidColorBrush(Color.FromArgb(anotace.Color.A, anotace.Color.R, anotace.Color.G, anotace.Color.B));
            int numId = project.ExtractNumericId(anotace.Id);

            if (numId !=0 && anotace.Type != AnotaceType.Vertebra)
            {
                Button minusButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(20, 10 + 42 * rowIndex, 0, 0),
                    Padding = new Thickness(0),
                    Tag = anotace.Id
                };
                Image binIcon = new Image
                {
                    Source = new BitmapImage(new Uri("../Resources/Icons/bin_icon.ico", UriKind.Relative)),
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                minusButton.Content = binIcon;
                minusButton.Click += MinusButton_Click;

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
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(20, 10 + 42 * rowIndex, 0, 0),
                    Tag = anotace.Id,
                };
                ButtonGrid.Children.Add(rect);
            }

            ToggleButton toggleButton = new ToggleButton
            {
                Content = anotace.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 125,
                Height = 32,
                Margin = new Thickness(60, 10 + 42 * rowIndex, 0, 0),
                Tag = anotace.Id,
                FontSize = 12,
            };
            toggleButton.Click += Button_Click;

            if (rowIndex == 0)
                SwitchActiveAnotButton(toggleButton);

            CheckBox checkBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 42,
                Height = 42,
                Margin = new Thickness(195, 15 + 42 * rowIndex, 0, 0),
                Tag = anotace.Id,
                IsEnabled = isValidator,
                IsChecked = anotace.IsValidated
            };
            checkBox.Checked += SwitchValidation_Check;
            checkBox.Unchecked += SwitchValidation_Check;

            ButtonGrid.Children.Add(toggleButton);
            ButtonGrid.Children.Add(checkBox);
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko anotace - přepne aktivní anotaci.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && toggleButton.Tag is string id)
            {
                SwitchActiveAnot(id); // předáváme string id
                SwitchActiveAnotButton(toggleButton);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Obsluha změny stavu validace anotace pomocí checkboxu.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void SwitchValidation_Check(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.Tag is string id)
            {
                project.ValidateAnnotationByID(id);
            }
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko plus - vytvoří novou anotaci zadaného typu.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button plusButton && plusButton.Tag is AnotaceType type)
            {
                bool isValidator = project.GetLoggedInUser().Validator;
                Anotace anot = project.CreateNewAnnotation(type);
                AddPreviewImage(anot);

                int rowIndex = (int)(plusButton.Margin.Top / 42);
                MoveElementsBelow(plusButton.Margin.Top);
                AddNewRow(anot, isValidator, rowIndex);
            }
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko minus - smaže anotaci.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string id)
            {
                Anotace anot = project.FindAnotaceById(id);
                project.RemovePointsAndConnections(PointCanvas, anot);
                UpdateElementsWithAnotace();

                DeletePreviewImage(id);
                project.DeleteAnnotation(id);
                DeleteRow(id);

                // najdi odpovídající plusButton podle typu anotace (např. prefix "V", "I", "F")
                string prefix = id.Substring(0, 1);
                AnotaceType type = GetAnotaceTypeFromPrefix(prefix);
                MoveElementsBelow(button.Margin.Top - 1, false);
            }
        }

        /// <summary>
        /// Posune všechny prvky pod zadanou pozicí nahoru nebo dolů.
        /// </summary>
        /// <param name="topThreshold">Prahová hodnota pozice Y</param>
        /// <param name="down">True pro posun dolů, false pro posun nahoru</param>
        private void MoveElementsBelow(double topThreshold, bool down = true)
        {
            foreach (var element in ButtonGrid.Children.OfType<FrameworkElement>())
            {
                if (element.Margin.Top >= topThreshold)
                {
                    element.Margin = new Thickness(
                        element.Margin.Left,
                        element.Margin.Top + (down ? 42 : -42),
                        element.Margin.Right,
                        element.Margin.Bottom
                    );
                }
            }
        }

        /// <summary>
        /// Určí typ anotace podle předpony ID.
        /// </summary>
        /// <param name="prefix">Předpona ID (V, I, F)</param>
        /// <returns>Typ anotace podle předpony</returns>
        private AnotaceType GetAnotaceTypeFromPrefix(string prefix)
        {
            return prefix switch
            {
                "V" => AnotaceType.Vertebra,
                "I" => AnotaceType.Implant,
                "F" => AnotaceType.Fusion,
                _ => AnotaceType.Vertebra
            };
        }

        /// <summary>
        /// Odstraní řádek s anotací z UI a aktualizuje ID následujících anotací.
        /// </summary>
        /// <param name="id">ID anotace, jejíž řádek se má odstranit</param>
        private void DeleteRow(string id)
        {
            string prefix = id.Substring(0, 1);         // např. "I", "F"
            int numId = project.ExtractNumericId(id);   // např. "3" z "V3"

            // Odstraníme prvky s odpovídajícím ID
            var elementsToRemove = ButtonGrid.Children.OfType<UIElement>()
                .Where(e => e is FrameworkElement fe && fe.Tag is string tag && tag == id)
                .ToList();

            foreach (var element in elementsToRemove)
            {
                ButtonGrid.Children.Remove(element);
            }

            // Aktualizujeme pozice, tagy a obsah všech následujících prvků po odstranění řádku
            foreach (var element in ButtonGrid.Children)
            {
                if (element is FrameworkElement fe && fe.Tag is string tag &&
                   tag.StartsWith(prefix) && int.TryParse(tag.Substring(1), out int tagIndex) &&
                   tagIndex > numId)
                {
                    int newIndex = tagIndex - 1;
                    string newId = prefix + newIndex;

                    fe.Tag = newId;

                    // Upravíme margin pro zachování správného rozložení
                    //fe.Margin = new Thickness(fe.Margin.Left, fe.Margin.Top - 30, fe.Margin.Right, fe.Margin.Bottom);

                    // Aktualizujeme obsah ToggleButton
                    if (element is ToggleButton toggleButton)
                    {
                        switch (prefix)
                        {
                            case "I":
                                toggleButton.Content = $"Implantát {tagIndex}";
                                break;
                            case "F":
                                toggleButton.Content = $"Fúze {tagIndex}";
                                break;
                            default:
                                toggleButton.Content = tagIndex;
                                MessageBox.Show("Toto nemělo nastat. Restartujte aplikaci.");
                                break;
                        }
                    }

                    project.ChangeAnnotationId(tag, newId);
                }
            }
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
            System.Windows.Point p = e.GetPosition(ToolBarTray);
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
                System.Windows.Point currentPoint = e.GetPosition(ToolBarTray);

                if (grip != null && grip.IsMouseCaptured)
                {
                    System.Windows.Point newPosition = Mouse.GetPosition(this);
                    int toolbarOffset = 18;
                    double newX = newPosition.X - offset.X;
                    double newY = newPosition.Y - offset.Y - toolbarOffset;

                    // Ensure the ToolBarTray stays within the bounds of the window
                    newX = Math.Max(0, Math.Min(newX, Grid.ColumnDefinitions[0].ActualWidth + Grid.ColumnDefinitions[1].ActualWidth - ToolBarTray.ActualWidth));
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

        private void CropCanvas_MouseLeave(object sender, MouseEventArgs e)
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

        /// <summary>
        /// Ořízne obrázek podle vybrané oblasti na CropCanvas.
        /// </summary>
        private void CropImage()
        {
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
            PointCanvas.Width = CropRectangle.Width;
            PointCanvas.Height = CropRectangle.Height;
            PreviewImage.Width = CropRectangle.Width;
            PreviewImage.Height = CropRectangle.Height;
            CropCanvas.Width = CropRectangle.Width;
            CropCanvas.Height = CropRectangle.Height;
            PreviewGrid.HorizontalAlignment = HorizontalAlignment.Left;
            PreviewGrid.VerticalAlignment = VerticalAlignment.Top;
            project.CropPreviewImages(CropRectangle.Width, CropRectangle.Height);

            project.CropOriginalPicture(croppedImage);
        }

        /// <summary>
        /// Potvrdí ořez obrázku a přepne do režimu kreslení.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (stateManager.CurrentState == AppState.Cropping)
            {
                CropImage();
            }

            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(DrawTButton);
        }

        /// <summary>
        /// Zruší ořez obrázku a vrátí se do režimu kreslení.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CroppedImage.Source = null;
            project.SetOriginalPicture(ImageHolder.Source as BitmapImage);

            stateManager.CurrentState = AppState.Drawing;
            SwitchActiveToolbarButton(DrawTButton);
        }

        /*
         * ======
         *  Zoom
         * ======
         */

        /// <summary>
        /// Obsluha kolečka myši s Ctrl/Shift - přiblížení/oddálení s zachováním pozice kurzoru.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // Získání pozice kurzoru vzhledem ke CanvasGrid
                var mousePos = e.GetPosition(CanvasGrid);

                // Získání pozice kurzoru vzhledem ke ScrollVieweru
                var mousePosInScroll = e.GetPosition(CanvasScrollViewer);

                // Aktuální zoom
                double oldZoom = ZoomSlider.Value / 100.0;

                // Změna zoomu
                if (e.Delta > 0)
                {
                    ZoomIn(null, null);
                }
                else if (e.Delta < 0)
                {
                    ZoomOut(null, null);
                }

                // Nový zoom
                double newZoom = ZoomSlider.Value / 100.0;

                // Výpočet nových offsetů tak, aby kurzor zůstal na stejném místě
                if (CanvasScrollViewer != null)
                {
                    // Poměr změny zoomu
                    double zoomRatio = newZoom / oldZoom;

                    // Nové offsety
                    double newHorizontalOffset = (mousePosInScroll.X + CanvasScrollViewer.HorizontalOffset) * zoomRatio - mousePosInScroll.X;
                    double newVerticalOffset = (mousePosInScroll.Y + CanvasScrollViewer.VerticalOffset) * zoomRatio - mousePosInScroll.Y;

                    // Nastavení nových offsetů
                    CanvasScrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
                    CanvasScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Zvětší zoom o 10%.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider.Value < ZoomSlider.Maximum)
            {
                ZoomSlider.Value += 10;
            }
        }

        /// <summary>
        /// Zmenší zoom o 10%.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ZoomOut(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider.Value > ZoomSlider.Minimum)
            {
                ZoomSlider.Value -= 10;
            }
        }

        /// <summary>
        /// Obsluha změny hodnoty zoom slideru - aktualizuje měřítko canvasu a bodových markerů.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ImageHolder != null)
            {
                double zoomFactor = ZoomSlider.Value / 100;
                CanvasGrid.LayoutTransform = new ScaleTransform(zoomFactor, zoomFactor);

                project.UpdatePointsScale(zoomFactor);
            }
        }


        /*
         * ================
         *  Drag Move View
         * ================
         */


        /// <summary>
        /// Začátek tažení viewportu pomocí pravého tlačítka myši.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ScrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _startDragPoint = e.GetPosition(sender as UIElement);
            (sender as ScrollViewer).CaptureMouse();
            (sender as ScrollViewer).Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Tažení viewportu pomocí pravého tlačítka myši.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
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

        /// <summary>
        /// Konec tažení viewportu pomocí pravého tlačítka myši.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void ScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                (sender as ScrollViewer).ReleaseMouseCapture();
                (sender as ScrollViewer).Cursor = Cursors.Arrow;
            }
        }

        /*
         * ==============
         *  PointMarkers
         * ==============
         */

        /// <summary>
        /// Obsluha kliknutí na PointCanvas - vytvoří nový bodový marker pro aktivní anotaci obratle.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void PointCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 1) Pokud není stav kreslení, nic nedělat
            if (stateManager.CurrentState != AppState.Drawing)
                return;

            int pointsCount = project.GetPointsCount();
            if (pointsCount >= 8) return;

            string[] labels = { "A", "B", "C", "D", "E", "F", "G", "H" };

            // 2) Zjistit aktivní anotaci podle ID
            var activeAnotaceIdString = project.ActiveAnotaceId();
            if (int.TryParse(activeAnotaceIdString, out int activeIndex))
            {
                var allAnnots = project.GetAnotaces();
                if (activeIndex >= 0 && activeIndex < allAnnots.Count)
                {
                    var anot = allAnnots[activeIndex];
                    // 3) Je-li aktivní anotace „Implantát“, body nevytváříme 
                    //    (kreslí se tahy na InkCanvas)
                    if (anot.Type == AnotaceType.Implant || anot.Type == AnotaceType.Fusion)
                    {
                        return;
                    }
                }
            }

            // -------------------------
            // 4) Jinak vytvořit bod
            // -------------------------

            // a) Pozice kliku 
            var position = e.GetPosition(PointCanvas);

            // b) Barva aktivní anotace
            var color = project.ActiveAnotaceColor();

            // c) Samotné vytvoření bodu
            var point = new PointMarker(
              PointCanvas,
              position,
              new SolidColorBrush(color),
              labels[pointsCount]
            );
            project.AddPointActiveAnot(point);

            project.SetActiveAnotaceIsAnotated(true);
            ToggleCropButton(!project.GetIsAnotated());

            // e) Vykreslení, měřítko, spojnice mezi body...
            project.UpdatePointsScale(ZoomSlider.Value / 100);
            project.DrawLineConnection(PointCanvas, pointsCount + 1);
        }

        /// <summary>
        /// Načte všechny bodové markery ze všech anotací na PointCanvas.
        /// </summary>
        public void LoadPointMarkers()
        {
            project.LoadPointMarkers(PointCanvas);
            project.UpdatePointsScale(ZoomSlider.Value / 100);
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