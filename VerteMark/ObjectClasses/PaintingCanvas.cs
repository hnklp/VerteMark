using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses {
    /// <summary>
    /// Plátno patřící k anotaci
    /// Obsahuje bitmapu anotace a metody pro manipulaci s ní (bitmapou).
    /// Bitmapa by měla být nastavená podle obrázku nad kterým se pracuje (momentální crop)
    /// </summary>
        internal class PaintingCanvas {

        WriteableBitmap? bitmapCanvas;

        public PaintingCanvas() {

        }

        public void CreateNewEmptyCanvas(int width, int height) {
         //   bitmapCanvas = new WriteableBitmap(width, height);
        }
        
        public void UpdateCanvas() {

        }

        

    }
}
