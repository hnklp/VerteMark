﻿using System.Windows;
using VerteMark.MainWindows;
using VerteMark.ObjectClasses;

namespace VerteMark.SubWindows
{
    /// <summary>
    /// Interakční logika pro JustSaveAlertWindow.xaml
    /// </summary>
    public partial class JustSaveAlertWindow : Window
    {
        Project project;
        MainWindow mainWindow;
        bool validator;

        public JustSaveAlertWindow(MainWindow mainWindow, bool validator)
        {
            InitializeComponent();
            project = Project.GetInstance();
            this.mainWindow = mainWindow;
            this.validator = validator;

            mainWindow.IsEnabled = false;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.IsEnabled = true;
            this.Close();
        }

        private void SaveAndContinue_Click(object sender, RoutedEventArgs e)
        {
            if (!validator) { project.SaveProject(1); }
            else { project.SaveProject(2); }
            project.saved = true;
            Browse(false);
            mainWindow.IsEnabled = true;
            this.Close();

            if (!project.isAnyProjectAvailable())
            {
                App.RestartApplication();
            }
        }

        private void PreSaveAndContinue_Click(object sender, RoutedEventArgs e)
        {
            if (!validator) { project.SaveProject(0); }
            else { project.SaveProject(0); }
            project.saved = true;
            mainWindow.IsEnabled = true;
            this.Close();

        }
        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            Browse(true);
            mainWindow.IsEnabled = true;
            this.Close();
        }

        public void Browse(bool select)
        {
            FolderbrowserWindow folderbrowserWindow = new FolderbrowserWindow(select);

            folderbrowserWindow.oldMainWindow = mainWindow;

            // Získání středu původního okna
            double originalCenterX = Left + Width / 2;
            double originalCenterY = Top + Height / 2;

            // Nastavení nové pozice nového okna tak, aby jeho střed byl totožný se středem původního okna
            folderbrowserWindow.Left = originalCenterX - folderbrowserWindow.Width / 2;
            folderbrowserWindow.Top = originalCenterY - folderbrowserWindow.Height / 2;

            mainWindow.Visibility = Visibility.Hidden;
            folderbrowserWindow.Show();
            
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.IsEnabled = true;
        }
    }
}