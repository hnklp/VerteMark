using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses {
    internal class PaintingCanvas {

        WriteableBitmap? bitmapCanvas;

        public PaintingCanvas() {

        }

        public void CreateNewEmptyCanvas(int width, int height) {
         //   bitmapCanvas = new WriteableBitmap(width, height);
        }
        // Tohle bude (nepřímo) pro kreslení v UI
        public void UpdateCanvas() {

        }

        

    }
}
