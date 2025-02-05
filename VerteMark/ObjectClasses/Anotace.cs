using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Třída reprezentující anotaci.
    /// Poskytuje nástroje pro automatickou tvorbu v novém projektu, veřejné metody pro aktualizaci bitmapy a stavu validace.
    /// Umožňuje definici anotace s identifikátorem, názvem, barvou a stavem ověření.
    /// </summary>
    internal class Anotace {

        public int Id { get; private set; }
        public string Name { get; private set; }
        public System.Drawing.Color Color { get; private set; }
        public bool IsValidated { get; private set; }
        public bool IsAnotated { get; private set; }
        WriteableBitmap? canvas;

        public List<PointMarker> Points;
        public List<LineConnection> Lines;

        public Anotace(int id, string name, System.Drawing.Color color) {
            this.Id = id;
            this.Name = name;
            this.Color = color;
            this.IsAnotated = false;

            Points = new List<PointMarker>();
            Lines = new List<LineConnection>();
        }


        public void CreateEmptyCanvas(int width, int height) {
            canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }


        public void UpdateCanvas(WriteableBitmap bitmapSource) {
            if (bitmapSource is WriteableBitmap writableBitmap) {
                if (canvas == null) {
                    canvas = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Bgra32, null);
                }
                try {
                    canvas = new WriteableBitmap(bitmapSource);
                }
                catch (Exception) {
                    // Handle exception if needed
                }
            }
            else {
                try {
                    canvas = new WriteableBitmap(bitmapSource);
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
        public void LoadAnnotationPointMarker(JArray pixelsArray, int width, int height) {
            // ze souradnic udelat body

        }
        // *****************************************************************************************

        public WriteableBitmap GetCanvas() {
            return canvas;

        }


        public void ClearCanvas() {
            if (canvas != null) {
                canvas = new WriteableBitmap((int)canvas.Width, (int)canvas.Height, 96, 96, PixelFormats.Bgra32, null);
            }
        }

        public void Validate(bool validate) {
            IsValidated = validate;
        }


        List<Tuple<int, int>> BitmapAsList() {
            List<Tuple<int, int>> list = new List<Tuple<int, int>>();
            if (canvas != null) {
                int stride = canvas.PixelWidth * 4;
                int size = canvas.PixelHeight * stride;
                byte[] pixels = new byte[size];
                canvas.CopyPixels(pixels, stride, 0);

                for (int y = 0; y < canvas.PixelHeight; y++) {
                    for (int x = 0; x < canvas.PixelWidth; x++) {
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

            // Obratle <1, 7>
            if (Points != null) {
                List<Tuple<int, int>> list = new List<Tuple<int, int>>();

                foreach (PointMarker point in this.Points)
                {
                    int point_x = (int)point.Position.X;
                    int point_y = (int)point.Position.Y;

                    list.Add(Tuple.Create(point_x, point_y));
                }

                result.Add(Id.ToString(), list);
            }

            // Obratle >= 8
            else if (canvas != null)
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

        public void SetAnnotationId(int Id)
        {
            this.Id = Id;
            this.Name = "Implantát " + (Id - 6);
        }
    }
}
