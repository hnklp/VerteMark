using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using VerteMark.ObjectClasses;
using WpfAnimatedGif;

namespace VerteMark.MainWindows
{
    /// <summary>
    /// Okno s uživatelskou příručkou obsahující hierarchickou strukturu návodů s GIF animacemi.
    /// </summary>
    public partial class GuideWindow : Window
    {
        /// <summary>Kolekce kategorií návodů</summary>
        public ObservableCollection<Category> Categories { get; set; }

        /// <summary>
        /// Vytvoří novou instanci GuideWindow a načte kategorie návodů.
        /// </summary>
        public GuideWindow()
        {
            InitializeComponent();
            LoadCategories();
            DataContext = this;
            MyTreeView.ItemsSource = Categories;
        }

        /// <summary>
        /// Načte a inicializuje kategorie návodů s jejich podkategoriemi a tlačítky.
        /// </summary>
        private void LoadCategories()
        {
            Categories = new ObservableCollection<Category>
            {
                new Category
                {
                    Title = "Práce s obratli",
                    Subcategories = new ObservableCollection<Subcategory>
                    {
                        new Subcategory
                        {
                            Title = "Mapování obratle",
                            Buttons = new ObservableCollection<ButtonInfo>
                            {
                            new ButtonInfo { Title = "Standardní obratel", GifPath = "pack://application:,,,/VerteMark;component/Resources/Gifs/standard_annotation_example.gif" },
                            new ButtonInfo { Title = "Anomálie/Implantát", GifPath = "pack://application:,,,/VerteMark;component/Resources/Gifs/gif_test.gif" },
                            }
                        },

                        new Subcategory
                        {
                            Title = "Validace obratle",
                            Buttons = new ObservableCollection<ButtonInfo>
                            {
                            new ButtonInfo { Title = "Modelově akceptovatelný obratel", GifPath = "pack://application:,,,/VerteMark;component/Resources/Gifs/gif_test.gif" },
                            new ButtonInfo { Title = "Neakceptovatelné nuance", GifPath = "pack://application:,,,/VerteMark;component/Resources/Gifs/gif_test.gif" },
                            }
                        }
                    }
                },

                new Category
                {
                    Title = "Program",
                    Subcategories = new ObservableCollection<Subcategory>
                    {
                        new Subcategory
                        {
                            Title = "Anotace",
                            Buttons = new ObservableCollection<ButtonInfo>
                            {
                                new ButtonInfo { Title = "Malování", GifPath = "pack://application:,,,/VerteMark;component/Resources/Gifs/gif_test.gif" },
                                new ButtonInfo { Title = "Posouvání", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" }
                            }
                        },
                        new Subcategory
                        {
                            Title = "Zkratky",
                            Buttons = new ObservableCollection<ButtonInfo>
                            {
                                new ButtonInfo { Title = "Přiblížení", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" },
                                new ButtonInfo { Title = "Oddálení", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" },
                                new ButtonInfo { Title = "Posouvání horizontálně", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" },
                                new ButtonInfo { Title = "Posouvání vertikálně", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" }
                            }
                        },
                        new Subcategory
                        {
                            Title = "Manipulace se soubory",
                            Buttons = new ObservableCollection<ButtonInfo>
                            {
                                new ButtonInfo { Title = "Sample1", GifPath = "pack://application:,,,/VerteMark;component/Gifs/gif_test.gif" }
                            }
                        }
                    }
                },
            };
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko návodu - zobrazí GIF animaci.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string gifPath = button.Tag as string;
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(gifPath, UriKind.Absolute);
                    image.EndInit();
                    ImageBehavior.SetAnimatedSource(GifImage, image);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Button '{button.Content}' clicked. GIF path: {gifPath}");
                }
            }
        }

        /// <summary>
        /// Obsluha kliknutí na tlačítko zpět - zavře okno.
        /// </summary>
        /// <param name="sender">Zdroj události</param>
        /// <param name="e">Argumenty události</param>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}


