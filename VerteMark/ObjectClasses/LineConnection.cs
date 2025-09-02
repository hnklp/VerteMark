using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public class LineConnection
{
    private Line _line;
    private PointMarker _startPoint;
    public PointMarker _endPoint { get; private set; }
    private ScaleTransform _scaleTransform;
    private Brush _colorBrush;

    public LineConnection(PointMarker startPoint, PointMarker endPoint,
        Canvas canvas, Brush colorBrush)
    {
        _startPoint = startPoint;
        _endPoint = endPoint;
        _colorBrush = colorBrush;

        _scaleTransform = new ScaleTransform(1, 1);

        // Vytvoření čáry
        _line = new Line
        {
            Stroke = _colorBrush,
            Opacity = 0.5,
            StrokeThickness = 3,
            RenderTransform = _scaleTransform
        };

        // Přidání čáry na plátno
        canvas.Children.Add(_line);
        Panel.SetZIndex(_line, 5);

        // Aktualizace pozice
        UpdatePosition();

        // Přihlášení k událostem bodů
        _startPoint.PositionChanged += OnPointMoved;
        _endPoint.PositionChanged += OnPointMoved;
    }

    public void Remove(Canvas canvas)
    {
        canvas.Children.Remove(_line);
        _startPoint.PositionChanged -= OnPointMoved;
        _endPoint.PositionChanged -= OnPointMoved;
    }

    private void OnPointMoved()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        _line.X1 = _startPoint.Position.X;
        _line.Y1 = _startPoint.Position.Y;
        _line.X2 = _endPoint.Position.X;
        _line.Y2 = _endPoint.Position.Y;
    }
}