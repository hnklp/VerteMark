using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Reprezentuje informace o tlačítku v uživatelské příručce.
    /// </summary>
    public class ButtonInfo
    {
        /// <summary>Název tlačítka</summary>
        public string Title { get; set; }
        /// <summary>Cesta k GIF souboru s návodem</summary>
        public string GifPath { get; set; }
    }

    /// <summary>
    /// Reprezentuje podkategorii v hierarchii uživatelské příručky.
    /// </summary>
    public class Subcategory
    {
        /// <summary>Název podkategorie</summary>
        public string Title { get; set; }
        /// <summary>Kolekce tlačítek v podkategorii</summary>
        public ObservableCollection<ButtonInfo> Buttons { get; set; }
    }

    /// <summary>
    /// Reprezentuje kategorii v hierarchii uživatelské příručky.
    /// </summary>
    public class Category
    {
        /// <summary>Název kategorie</summary>
        public string Title { get; set; }
        /// <summary>Kolekce podkategorií v kategorii</summary>
        public ObservableCollection<Subcategory> Subcategories { get; set; }
    }
}
