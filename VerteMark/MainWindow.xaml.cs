using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.IO;
using Microsoft.Win32;
using VerteMark.ObjectClasses;

namespace VerteMark
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        // Vlastnosti
        Utility utility;
        public MainWindow() {
            InitializeComponent();

            // Tady začíná kód
            utility = new Utility();
        }
        private void mnuOpen_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("New");
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
        }
    }
}