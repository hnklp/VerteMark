using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Třída reprezentující anotaci.
    /// Poskytuje nástroje pro automatickou tvorbu v novém projektu, veřejné metody pro aktualizaci bitmapy a stavu validace.
    /// Umožňuje definici anotace s identifikátorem, názvem, barvou a stavem ověření.
    /// </summary>
    internal class Anotace {

        public enum AnotaceType
            {
                Vertebra,
                Implant,
                Fusion
            }
        
        public string Id { get; private set; }
        public string Name { get; private set; }
        public System.Drawing.Color Color { get; private set; }
        public bool IsValidated { get; private set; }
        public bool IsAnotated { get; private set; }
        public AnotaceType Type { get; private set; }
        public WriteableBitmap? Canvas;
        public Image PreviewImage;

        public List<PointMarker> Points;
        public List<LineConnection> Lines;

        public Anotace(string id, string name, System.Drawing.Color color, AnotaceType type = AnotaceType.Vertebra)
        {
            this.Id = id;
            this.Name = name;
            this.Color = color;
            this.IsAnotated = false;
            this.Type = type;

            Points = new List<PointMarker>();
            Lines = new List<LineConnection>();
        }


        public void CreateEmptyCanvas(int width, int height) {
            Canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }


        public void UpdateCanvas(WriteableBitmap bitmapSource) {
            if (bitmapSource is WriteableBitmap writableBitmap) {
                if (Canvas == null) {
                    Canvas = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Bgra32, null);
                }
                try {
                    Canvas = new WriteableBitmap(bitmapSource);
                }
                catch (Exception) {
                    // Handle exception if needed
                }
            }
            else {
                try {
                    Canvas = new WriteableBitmap(bitmapSource);
                }
                catch (Exception) {
                    // Handle exception if needed
                }
            }
        }

        // NACITANI CARY
        public void LoadAnnotationCanvas(JArray pixelsArray, int width, int height) {
            // Vytvoření pole pixelů
            byte[] pixels = new byte[width * height * 4];
            foreach (JObject pixelObj in pixelsArray) {
                int x = (int)pixelObj["Item1"];
                int y = (int)pixelObj["Item2"];
                // Nastavení barev pixelu
                int index = (y * width + x) * 4;
                pixels[index] = this.Color.B;
                pixels[index + 1] = this.Color.G;
                pixels[index + 2] = this.Color.R;
                pixels[index + 3] = this.Color.A;
            }
            // Vytvoření nového WriteableBitmap
            WriteableBitmap newBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            // Zkopírování pole pixelů do nového WriteableBitmap
            newBitmap.WritePixels(new Int32Rect(0, 0, newBitmap.PixelWidth, newBitmap.PixelHeight), pixels, newBitmap.PixelWidth * 4, 0);
            this.UpdateCanvas(newBitmap);
        }

        // *****************************************************************************************
        // NACITANI BODU
        public void LoadAnnotationPointMarker(JArray pixelsArray) {
            string[] labels = { "A", "B", "C", "D", "E", "F", "G", "H" };
            int index = 0;
            // ze souradnic udelat body

            foreach (JObject pixelObj in pixelsArray) {
                if (index >= labels.Length) break;

                int x = (int)pixelObj["Item1"];
                int y = (int)pixelObj["Item2"];
                System.Windows.Point position = new System.Windows.Point(x, y);
                System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(this.Color.A, this.Color.R, this.Color.G, this.Color.B);

                PointMarker point = new(position, new SolidColorBrush(color), labels[index]);
                this.Points.Add(point);

                index++;
            }
        }

        // *****************************************************************************************

        public WriteableBitmap GetCanvas() {
            return Canvas;

        }

        public void SetPreviewImage()
        {
            if (PreviewImage != null && Canvas != null) {
                PreviewImage.Source = Canvas;
                PreviewImage.Opacity = 0.5;
            }
        }

        public void ClearCanvas() {
            if (Canvas != null) {
                Canvas = new WriteableBitmap((int)Canvas.Width, (int)Canvas.Height, 96, 96, PixelFormats.Bgra32, null);
            }
        }

        public void Validate(bool validate) {
            IsValidated = validate;
        }

        List<Tuple<int, int>> BitmapAsList() {
            List<Tuple<int, int>> list = new List<Tuple<int, int>>();
            if (Canvas != null) {
                int stride = Canvas.PixelWidth * 4;
                int size = Canvas.PixelHeight * stride;
                byte[] pixels = new byte[size];
                Canvas.CopyPixels(pixels, stride, 0);

                for (int y = 0; y < Canvas.PixelHeight; y++) {
                    for (int x = 0; x < Canvas.PixelWidth; x++) {
                        int index = y * stride + 4 * x;
                        byte alpha = pixels[index + 3]; // Alpha kanál (průhlednost)
                        if (alpha > 0) // Kontrola průhlednosti
                        {
                            list.Add(Tuple.Create(x, y));
                        }
                    }
                }
            }
            return list;
        }

        // *****************************************************************************************
        public Dictionary<String, List<Tuple<int, int>>> GetAsDict() {
            Dictionary<String, List<Tuple<int, int>>> result = new Dictionary<String, List<Tuple<int, int>>>();

            // Anotace <0, 6>
            if (Points != null && Points.Count > 0) {
                List<Tuple<int, int>> list = new List<Tuple<int, int>>();

                foreach (PointMarker point in this.Points)
                {
                    int point_x = (int)point.Position.X;
                    int point_y = (int)point.Position.Y;

                    list.Add(Tuple.Create(point_x, point_y));
                }

                result.Add(Id.ToString(), list);
            }

            // Anotace >= 7
            else if (Canvas != null)
            {
                List<Tuple<int, int>> bitmap = BitmapAsList();
                result.Add(Id.ToString(), bitmap);
            }

            // Neidentifikovane objekty
            else
            {
                List<Tuple<int, int>> bitmap = new List<Tuple<int, int>>();
                result.Add(Id.ToString(), bitmap);
            }
            return result;
        }
        // *****************************************************************************************

        public void SetIsAnotated(bool isAnotated)
        {
            this.IsAnotated = isAnotated;
        }

        public void SetId(string id)
        {
           Id = id;
        }
    }
}
