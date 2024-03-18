using System;
using System.Collections.Generic;
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
        public string Name { get; private set;}
        public System.Drawing.Color Color { get; private set;}
        public bool IsValidated {  get; private set;}
        WriteableBitmap? canvas;

        public Anotace(int id, string name, System.Drawing.Color color) {
            this.Id = id;
            this.Name = name;
            this.Color = color;
        }
        public void CreateEmptyCanvas(int width, int height) {
            canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }
        public void UpdateCanvas(BitmapSource bitmapSource) {
            if (bitmapSource is WriteableBitmap writableBitmap) {
                if (canvas == null) {
                    canvas = new WriteableBitmap(writableBitmap.PixelWidth, writableBitmap.PixelHeight, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Bgra32, null);
                }

                // Get the pixels of both images
                byte[] canvasPixels = new byte[canvas.PixelWidth * canvas.PixelHeight * 4];
                byte[] newPixels = new byte[writableBitmap.PixelWidth * writableBitmap.PixelHeight * 4];

                writableBitmap.CopyPixels(new Int32Rect(0, 0, writableBitmap.PixelWidth, writableBitmap.PixelHeight), newPixels, writableBitmap.PixelWidth * 4, 0);
                canvas.CopyPixels(new Int32Rect(0, 0, canvas.PixelWidth, canvas.PixelHeight), canvasPixels, canvas.PixelWidth * 4, 0);

                // Blend the pixels of the two images
                for (int i = 0; i < canvasPixels.Length; i += 4) {
                    byte newAlpha = newPixels[i + 3];
                    byte invAlpha = (byte)(255 - newAlpha);

                    canvasPixels[i] = (byte)((canvasPixels[i] * invAlpha + newPixels[i] * newAlpha) / 255);
                    canvasPixels[i + 1] = (byte)((canvasPixels[i + 1] * invAlpha + newPixels[i + 1] * newAlpha) / 255);
                    canvasPixels[i + 2] = (byte)((canvasPixels[i + 2] * invAlpha + newPixels[i + 2] * newAlpha) / 255);
                    canvasPixels[i + 3] = (byte)(255 - ((255 - canvasPixels[i + 3]) * (255 - newAlpha)) / 255);
                }

                // Update the canvas with the blended pixels
                canvas.WritePixels(new Int32Rect(0, 0, canvas.PixelWidth, canvas.PixelHeight), canvasPixels, canvas.PixelWidth * 4, 0);
            } else {
                try {
                    canvas = new WriteableBitmap(bitmapSource);
                } catch (Exception) {
                    // Handle exception if needed
                }
            }
        }
        public WriteableBitmap GetCanvas() {
            return canvas;
        }
        public void ClearCanvas() {
            canvas = new WriteableBitmap((int)canvas.Width, (int)canvas.Height, 96, 96, PixelFormats.Bgra32, null);
        }
        public void Validate() {

        }

    }
}
