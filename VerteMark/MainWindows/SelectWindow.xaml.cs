using System.Windows;
using VerteMark.ObjectClasses;

namespace VerteMark
{
    /// <summary>
    /// Interakční logika pro SelectWindow.xaml
    /// </summary>
    public partial class SelectWindow : Window
    {
        public SelectWindow()
        {
            InitializeComponent();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow MainWindow = new MainWindow();

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            MainWindow.Left = originalCenterX - MainWindow.Width / 2;
            MainWindow.Top = originalCenterY - MainWindow.Height / 2;

            MainWindow.Show();

            this.Close();
        }
    }
}
