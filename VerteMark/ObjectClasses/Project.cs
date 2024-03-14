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

        FileManager fileManager;
        User? loggedInUser;
        List<Anotace> anotaces;

        public Project() {
            fileManager = new FileManager();
            anotaces = new List<Anotace>();
        }

        public void TryOpeningProject(string path) {
            FolderState folderState = fileManager.CheckFolderType(path);

            switch (folderState) {
                case FolderState.New:
                    // Oznámit UI že je nový(aby otevřel další okno po načtení tady) a načíst ho tady
                    CreateNewProject(path);
                    break;
                case FolderState.Existing:
                    // Oznámit UI že je existing(aby otevřel další okno po načtení tady) a načíst ho tady
                    LoadProject(path);
                    break;
                case FolderState.Nonfunctional:
                    // Oznámit UI že je to špatný file a vrátit se 
                    break;
            }
        }
        public void CreateNewProject(string path) {
            // Vytvořím všechny složky (to dělá fileloader)

            // Initialize project
            InitializeProject();
        }
        public void LoadProject(string path) {
            // si řekne o složky atd

            // Initialize project
            InitializeProject();
        }
        void InitializeProject() {
            // nainstancuje všechny proměnné projektu
            // načte z jsonu anotace
            // načte **někam** metadata csv
            // načte **někam** png podklad

        }
        public void SaveProject() {
            // zavolá filemanager aby uložil všechny instance (bude na to možná pomocná třída co to dá dohromady jako 1 json a 1 csv)
            // záležitosti správných složek a správných formátů souborů má na starost filemanager
        }
        
        public void LoginNewUser(string id, bool validator) {
            loggedInUser = new User(id, validator);
        }
        public void LogoutUser() {
            loggedInUser = null;
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
            } else {
                throw new InvalidOperationException($"Anotace with ID {idAnotace} not found.");
            }
        }


    }
}
