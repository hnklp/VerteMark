using System.Windows;

namespace VerteMark
{
    /// <summary>
    /// Okno s informacemi o aplikaci VerteMark.
    /// </summary>
    public partial class AboutWindow : Window
    {
        /// <summary>
        /// Vytvoří novou instanci AboutWindow.
        /// </summary>
        public AboutWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko OK - zavře okno.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
