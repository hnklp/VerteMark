using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark.SubWindows {
    /// <summary>
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class SaveAlertWindow : Window {
        Utility utility;
        MainWindow mainWindow;

        public SaveAlertWindow(MainWindow mainWindow) {
            InitializeComponent();
            utility = new Utility();
            this.mainWindow = mainWindow;
        }

        private void Back_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void SaveAndContinue_Click(object sender, RoutedEventArgs e) {
            utility.SaveProject();
            utility.saved = true;
            Browse();
            this.Close();
        }

        public void Browse() {
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(false);

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;


            folderbrowserWindow.Show();
        }
    }
}
