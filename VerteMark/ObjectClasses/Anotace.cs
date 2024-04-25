using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace VerteMark.ObjectClasses {
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
        WriteableBitmap? canvas;

        public Anotace(int id, string name, System.Drawing.Color color) {
            this.Id = id;
            this.Name = name;
            this.Color = color;
        }
        public void CreateEmptyCanvas(int width, int height) {
            canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }
        public void UpdateCanvas(WriteableBitmap bitmapSource) {
            if(bitmapSource is WriteableBitmap writableBitmap) {
                if(canvas == null) {
                    canvas = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Bgra32, null);
                }
                try {
                    canvas = new WriteableBitmap(bitmapSource);
                } catch(Exception) {
                    // Handle exception if needed
                }
            } else {
                try {
                    canvas = new WriteableBitmap(bitmapSource);
                } catch(Exception) {
                    // Handle exception if needed
                }
            }
        }

        public WriteableBitmap GetCanvas() {
            return canvas;
        }
        public void ClearCanvas() {
            if (canvas != null)
            {
                canvas = new WriteableBitmap((int)canvas.Width, (int)canvas.Height, 96, 96, PixelFormats.Bgra32, null);
            }
        }


        public void Validate() {

        }


        private List<Tuple<int, int>> BitmapAsList()
        {
            List<Tuple<int, int>> list = new List<Tuple<int, int>>();
            if (canvas != null)
            {
                int stride = canvas.PixelWidth * 4;
                int size = canvas.PixelHeight * stride;
                byte[] pixels = new byte[size];
                canvas.CopyPixels(pixels, stride, 0);

                for (int y = 0; y < canvas.PixelHeight; y++)
                {
                    for (int x = 0; x < canvas.PixelWidth; x++)
                    {
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


        public Dictionary<string, List<Tuple<int, int>>> GetAsDict()
        {
            Dictionary<string, List<Tuple<int, int>>> result = new Dictionary<string, List<Tuple<int, int>>>();
            if (canvas != null)
            {
                List<Tuple<int, int>> bitmap = BitmapAsList();
                result.Add(Id.ToString(), bitmap);
            }
            return result;
        }

    }
}
