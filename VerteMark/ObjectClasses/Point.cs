using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

public class Point
{
    private Canvas _canvas;
    public System.Windows.Point Position { get; private set; }
    private FrameworkElement _cross;
    private ScaleTransform _scaleTransform;
    private const double BaseCrossSize = 10; // Základní velikost kříže

    public Point(Canvas canvas, System.Windows.Point position)
    {
        _canvas = canvas;
        Position = position;
        _scaleTransform = new ScaleTransform(1, 1);
        _cross = CreateCross(position);
        _canvas.Children.Add(_cross);
    }

    private FrameworkElement CreateCross(System.Windows.Point position)
    {
        var group = new Canvas();

        var line1 = new Line
        {
            X1 = -BaseCrossSize / 2,
            Y1 = 0,
            X2 = BaseCrossSize / 2,
            Y2 = 0,
            Stroke = Brushes.Red,
            StrokeThickness = 2
        };

        var line2 = new Line
        {
            X1 = 0,
            Y1 = -BaseCrossSize / 2,
            X2 = 0,
            Y2 = BaseCrossSize / 2,
            Stroke = Brushes.Red,
            StrokeThickness = 2
        };

        group.Children.Add(line1);
        group.Children.Add(line2);

        Canvas.SetLeft(group, position.X);
        Canvas.SetTop(group, position.Y);

        group.RenderTransform = _scaleTransform;

        return group;
    }

    public void UpdateScale(double scale)
    {
        _scaleTransform.ScaleX = scale;
        _scaleTransform.ScaleY = scale;
    }

    public void MoveTo(System.Windows.Point newPosition)
    {
        Position = newPosition;
        Canvas.SetLeft(_cross, newPosition.X);
        Canvas.SetTop(_cross, newPosition.Y);
    }
}
