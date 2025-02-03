using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerteMark.ObjectClasses
{
    public class ButtonInfo
    {
        public string Title { get; set; }
        public string GifPath { get; set; }
    }

    public class Subcategory
    {
        public string Title { get; set; }
        public ObservableCollection<ButtonInfo> Buttons { get; set; }
    }

    public class Category
    {
        public string Title { get; set; }
        public ObservableCollection<Subcategory> Subcategories { get; set; }
    }
}
