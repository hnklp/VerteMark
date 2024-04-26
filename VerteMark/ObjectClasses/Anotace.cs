using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
                canvas.CopyPixels(new Int32Rect(0, 0, canvas.PixelWidth, canvas.PixelHeight), pixels, stride, 0);

                Debug.WriteLine("---1. HEIGHT -- 2. WIDTH");
                Debug.WriteLine(canvas.PixelHeight);
                Debug.WriteLine(canvas.PixelWidth);
                Debug.WriteLine("-------------------");

                int maxx = 0;
                int maxy = 0;

                for (int y = 0; y < canvas.PixelHeight; y++)
                {
                    for (int x = 0; x < canvas.PixelWidth; x++)
                    {
                        int index = y * stride + 4 * x;
                        byte alpha = pixels[index + 3]; // Alpha kanál (průhlednost)
                        if (alpha == 255) // Pokud je alfa hodnota 1 (plná průhlednost)
                        {
                            list.Add(Tuple.Create(x, y)); // Uložit pozici pixelu
                        }
                        maxx += 1;
                    }
                    maxy += 1;
                }
                Debug.WriteLine("---MAX X --MAX  Y--");
                Debug.WriteLine(maxx);
                Debug.WriteLine(maxy);
                Debug.WriteLine("-------------------");
            }
            return list;
        }


public void SaveBitmapAsPng(List<Tuple<int, int>> pixelList)
    {
        if (pixelList == null || pixelList.Count == 0)
        {
            // Pokud není k dispozici žádný seznam pixelů, není co ukládat
            return;
        }

        // Zjistit rozměry obrazu
        int width = pixelList.Max(p => p.Item1) + 1; // Maximální hodnota x + 1
        int height = pixelList.Max(p => p.Item2) + 1; // Maximální hodnota y + 1

        // Vytvořit nový obraz s průhledností
        WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        // Vytvořit byte array pro pixely obrazu
        byte[] pixels = new byte[width * height * 4];

        // Nastavit pixely na průhledné (Transparent)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 3] = 0; // Alpha kanál
        }

        // Nastavit pixely podle seznamu pixelů
        foreach (var pixel in pixelList)
        {
            int x = pixel.Item1;
            int y = pixel.Item2;

            // Vypočítat index v byte array pro aktuální pixel
            int index = (y * width + x) * 4;

            // Nastavit černou barvu pixelu (RGBA: 0, 0, 0, 255)
            pixels[index] = 0; // R
            pixels[index + 1] = 0; // G
            pixels[index + 2] = 0; // B
            pixels[index + 3] = 255; // Alpha
        }

        // Nastavit pixely obrazu
        Int32Rect rect = new Int32Rect(0, 0, width, height);
        int stride = width * 4;
        bitmap.WritePixels(rect, pixels, stride, 0);

        // Uložit obraz jako soubor PNG na plochu
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "obraz.png");

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
    }





    public Dictionary<string, List<Tuple<int, int>>> GetAsDict()
        {
            Dictionary<string, List<Tuple<int, int>>> result = new Dictionary<string, List<Tuple<int, int>>>();
            if (canvas != null)
            {
                List<Tuple<int, int>> bitmap = BitmapAsList();
                result.Add(Id.ToString(), bitmap);
                SaveBitmapAsPng(bitmap);
            }
            Debug.WriteLine("---MAX LIST TUPLE LENGHT----");
            Debug.WriteLine(result.Count); ;
            Debug.WriteLine("-------------------");
          
            return result;
            
        }

    }
}
