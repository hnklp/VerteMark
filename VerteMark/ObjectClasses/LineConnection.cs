using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public class LineConnection
{
    private Line _line;
    private PointMarker _startPoint;
    private PointMarker _endPoint;
    private ScaleTransform _scaleTransform;

    public LineConnection(PointMarker startPoint, PointMarker endPoint,
        Canvas canvas)
    {
        _startPoint = startPoint;
        _endPoint = endPoint;

        _scaleTransform = new ScaleTransform(1, 1);

        // Vytvoření čáry
        _line = new Line
        {
            Stroke = Brushes.DarkRed,
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

    public void UpdateScale(double scale)
    {
        _scaleTransform.ScaleX = scale;
        _scaleTransform.ScaleY = scale;
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