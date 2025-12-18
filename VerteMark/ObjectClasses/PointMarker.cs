using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

/// <summary>
/// Reprezentuje bodový marker pro anotace obratlů.
/// Umožňuje zobrazení, přesouvání a škálování bodového markeru na canvasu.
/// </summary>
public class PointMarker
{
    private Canvas? _canvas;
    /// <summary>Pozice markeru na canvasu</summary>
    public System.Windows.Point Position { get; private set; }
    private FrameworkElement? _cross;
    /// <summary>Transformace pro škálování markeru</summary>
    public ScaleTransform _scaleTransform { get; private set; }
    private const double BaseCrossSize = 10;
    private const double HitBoxSize = 20;
    /// <summary>Událost vyvolaná při změně pozice markeru</summary>
    public event Action? PositionChanged;
    private Brush _colorBrush;
    private String _label;

    private bool _isDragging = false;
    private Point _dragOffset;

    /// <summary>
    /// Vytvoří novou instanci bodového markeru a okamžitě ho vykreslí na canvas.
    /// </summary>
    /// <param name="canvas">Canvas pro vykreslení markeru</param>
    /// <param name="position">Počáteční pozice markeru</param>
    /// <param name="colorBrush">Barva markeru</param>
    /// <param name="label">Textový popisek markeru</param>
    public PointMarker(Canvas canvas, Point position, Brush colorBrush, string label)
    {
        Position = position;
        _colorBrush = colorBrush;
        _label = label;

        _scaleTransform = new ScaleTransform(1, 1);

        DrawPointMarker(canvas);
    }

    /// <summary>
    /// Vytvoří novou instanci bodového markeru bez okamžitého vykreslení.
    /// </summary>
    /// <param name="position">Počáteční pozice markeru</param>
    /// <param name="colorBrush">Barva markeru</param>
    /// <param name="label">Textový popisek markeru</param>
    public PointMarker(Point position, Brush colorBrush, string label)
    {
        Position = position;
        _colorBrush = colorBrush;
        _label = label;

        _scaleTransform = new ScaleTransform(1, 1);
    }

    /// <summary>
    /// Vykreslí bodový marker na zadaný canvas.
    /// </summary>
    /// <param name="canvas">Canvas pro vykreslení markeru</param>
    public void DrawPointMarker(Canvas canvas)
    {
        _canvas = canvas;

        // Vytvoření kříže
        _cross = CreateCross();


        // Přidání na plátno
        _canvas.Children.Add(_cross);
        Panel.SetZIndex(_cross, 6);

        UpdatePosition(Position);
    }

    private FrameworkElement CreateCross()
    {
        var group = new Canvas();

        var hitbox = new Ellipse
        {
            Width = HitBoxSize,
            Height = HitBoxSize,
            Fill = Brushes.Transparent,
            IsHitTestVisible = true
        };

        // Nastavení hitboxu do středu
        Canvas.SetLeft(hitbox, -HitBoxSize / 2);
        Canvas.SetTop(hitbox, -HitBoxSize / 2);


        var line1 = new Line
        {
            X1 = -BaseCrossSize / 2,
            Y1 = 0,
            X2 = BaseCrossSize / 2,
            Y2 = 0,
            Stroke = _colorBrush,
            StrokeThickness = 2
        };

        var line2 = new Line
        {
            X1 = 0,
            Y1 = -BaseCrossSize / 2,
            X2 = 0,
            Y2 = BaseCrossSize / 2,
            Stroke = _colorBrush,
            StrokeThickness = 2
        };

        var label = new TextBlock
        {
            Text = _label,
            Foreground = Brushes.White, // Bílé písmo
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black, // Černý obrys
                ShadowDepth = 0
            }
        };

        Canvas.SetLeft(label, 10);
        Canvas.SetTop(label, - 10);

        group.Children.Add(line1);
        group.Children.Add(line2);
        group.Children.Add(hitbox);
        group.Children.Add(label);
        group.RenderTransform = _scaleTransform;

        group.MouseLeftButtonDown += OnMouseLeftButtonDown;
        group.MouseMove += OnMouseMove;
        group.MouseLeftButtonUp += OnMouseLeftButtonUp;

        return group;
    }

    /// <summary>
    /// Aktualizuje měřítko markeru.
    /// </summary>
    /// <param name="scale">Nové měřítko (1.0 = 100%)</param>
    public void UpdateScale(double scale)
    {
        _scaleTransform.ScaleX = scale;
        _scaleTransform.ScaleY = scale;
    }

    /// <summary>
    /// Aktualizuje pozici markeru na canvasu.
    /// </summary>
    /// <param name="newPosition">Nová pozice markeru</param>
    public void UpdatePosition(Point newPosition)
    {
        Position = newPosition;

        Canvas.SetLeft(_cross, Position.X);
        Canvas.SetTop(_cross, Position.Y);

        PositionChanged?.Invoke();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragOffset = e.GetPosition(_canvas) - new Vector(Position.X, Position.Y);
        _cross.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var mousePos = e.GetPosition(_canvas);
            UpdatePosition(new Point(mousePos.X - _dragOffset.X, mousePos.Y - _dragOffset.Y));
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _cross.ReleaseMouseCapture();
        }
    }

    /// <summary>
    /// Odstraní marker z canvasu.
    /// </summary>
    /// <param name="canvas">Canvas, ze kterého se má marker odstranit</param>
    public void Remove(Canvas canvas)
    {
        canvas.Children.Remove(_cross);
    }
}
