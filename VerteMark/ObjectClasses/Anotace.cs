using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses {
    internal class Anotace {

        public int Id { get; private set; }
        public string Name { get; private set;}
        public Color Color { get; private set;}
        public bool IsValidated {  get; private set;}
        PaintingCanvas canvas;

        public Anotace(int id, string name, Color color) {
            this.Id = id;
            this.Name = name;
            this.Color = color;
            canvas = new PaintingCanvas();
        }

        public void UpdateCanvas() {

        }
        public void ClearCanvas() {

        }
        public void Validate() {

        }

    }
}
