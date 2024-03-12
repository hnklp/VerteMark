using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VerteMark.ObjectClasses {
    /// <summary>
    /// Hlavní třída projektu.
    /// Propojuje ostatní třídy a drží informace o aktuálním stavu 
    /// 
    /// Zahrnuje:
    /// * Všechny anotace
    /// * Přihlášeného uživatele
    /// * Manipulaci s anotacemi, uživatelem a soubory (nepřímo)
    /// </summary>
    internal class Project {

        User? user;
        List<Anotace> anotaces;

        public Project() {
            anotaces = new List<Anotace>();
        }

        public void CreateNewProject(string path) {
            // Load DCIM
            // Create JPEG picture
            // Create MetaData
        }
        public void LoadProject(string path) {

        }
        public void SaveProject() { 
        
        }
        // Initializes new project into the app
        void InitializeProject() {

        }

        void NewAnotaces() {
            anotaces.Add(new Anotace(0, "C1", System.Drawing.Color.Red));
            anotaces.Add(new Anotace(1, "C2", System.Drawing.Color.Red));
            anotaces.Add(new Anotace(2, "C3", System.Drawing.Color.Red));
        }
        public void UpdateAnotaceCanvas(int idAnotace) {
            FindAnotaceById(idAnotace).UpdateCanvas();
        }

        void ClearAnotace(int idAnotace) {
            FindAnotaceById(idAnotace).ClearCanvas(); 
        }

        Anotace FindAnotaceById(int idAnotace) { 
            Anotace? foundAnotace = anotaces.Find(anotace => anotace.Id == idAnotace);
            if (foundAnotace != null) {
                return foundAnotace;
            }else {
                throw new InvalidOperationException($"Anotace with ID {idAnotace} not found.");
            }
        }

    }
}
