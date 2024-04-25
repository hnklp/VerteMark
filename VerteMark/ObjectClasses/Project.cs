using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using VerteMark.ObjectClasses.FolderClasses;
using System.Diagnostics;
using System.Windows.Shell;
using Newtonsoft.Json;
using System.Windows.Annotations;
using Newtonsoft.Json.Linq;

namespace VerteMark.ObjectClasses
{
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

        private static Project instance;
        FolderUtilityManager folderUtilityManager;
        User? loggedInUser; // Info o uživateli
        List<Anotace> anotaces; // Objekty anotace
        Anotace? activeAnotace;
        BitmapImage? originalPicture; // Fotka toho krku
        Metadata? metadata; // Metadata projektu
        JsonManipulator jsonManip;


        public Project() {
            anotaces = new List<Anotace>();
            originalPicture = new BitmapImage();
            folderUtilityManager = new FolderUtilityManager();
            jsonManip = new JsonManipulator();
        }


        // SLOUZI POUZE PRO ZIP FILE
        // ZALOZENI SLOZKY TEMP V BEHOVEM PROSTREDI
        public bool TryOpeningProject(string path) {
            //CreateNewProject(path);
            folderUtilityManager.ExtractZip(path);
            return true;
        }

        public void CreateNewProject(string path) {
            // Vytvoř čistý metadata
            metadata = new Metadata();
            // Vytvoř čistý anotace
            CreateNewAnotaces();
            // Získej čistý (neoříznutý) obrázek do projektu ((filemanagerrrr))

            folderUtilityManager.CreateNewProject(path);
            originalPicture = folderUtilityManager.GetImage();
        }


        public void LoadProject(string path) {
            // Získej metadata
            // METADATA PRI LOADOVANI PROJEKTU NEPOTREBUJEME
            // VSECHNY POTREBNY INFORMACE BUDOU V JSON S ANOTACEMA
            // Získej anotace
            //CreateNewAnotaces(); // - prozatimni reseni!
            // Získej uložený obrázek do projektu
            
            string jsonContent = folderUtilityManager.LoadProject(path);
            Debug.WriteLine(jsonContent);
            Debug.WriteLine("--------JSON-------------");


            originalPicture = folderUtilityManager.GetImage();

            JObject jsonObject = JObject.Parse(jsonContent);

            // Získání seznamu anotací ze zpracovaného JObject
            JArray annotationsArray = (JArray)jsonObject["Annotations"];

            // Předání seznamu anotací pro další zpracování
            LoadAnnotations(annotationsArray);
        }


        public void SaveProject()
        {

            // zavolá filemanager aby uložil všechny instance (bude na to možná pomocná třída co to dá dohromady jako 1 json a 1 csv)
            // záležitosti správných složek a správných formátů souborů má na starost filemanager
            // ZKOUSKA UKLADANI TEMP DO ZIP
            SaveJson();
            folderUtilityManager.Save(); // bude brat parametr string json 
        }


        void SaveJson() {
            List<Dictionary<string, List<Tuple<int, int>>>> dicts = new List<Dictionary<string, List<Tuple<int, int>>>>();
            foreach (Anotace anot in anotaces) {
                dicts.Add(anot.GetAsDict());
            }

            string json = jsonManip.ExportJson(loggedInUser, dicts);

            folderUtilityManager.SaveJson(json);
        }


        public void LoginNewUser(string id, bool validator) {
            loggedInUser = new User(id, validator);
        }


        public void LogoutUser() {
            loggedInUser = null;
        }


        public User? GetLoggedInUser() {
            return loggedInUser;
        }


        void CreateNewAnotaces() {
            anotaces.Add(new Anotace(0, "C1", System.Drawing.Color.Red));
            anotaces.Add(new Anotace(1, "C2", System.Drawing.Color.Orange));
            anotaces.Add(new Anotace(2, "C3", System.Drawing.Color.Yellow));
            anotaces.Add(new Anotace(3, "C4", System.Drawing.Color.Lime));
            anotaces.Add(new Anotace(4, "C5", System.Drawing.Color.Aquamarine));
            anotaces.Add(new Anotace(5, "C6", System.Drawing.Color.Aqua));
            anotaces.Add(new Anotace(6, "C7", System.Drawing.Color.BlueViolet));
            anotaces.Add(new Anotace(7, "Implantát", System.Drawing.Color.DeepPink));
            SelectActiveAnotace(0);
        }

        void LoadAnnotations(JArray annotationsArray)
        {
            foreach (JObject annotationObj in annotationsArray)
            {
                foreach (var annotation in annotationObj)
                {
                    int anotaceId = int.Parse(annotation.Key);

                    // Vytvoření nového objektu Anotace s daným ID a názvem (zatím výchozí název)
                    Anotace novaAnotace = new Anotace(anotaceId, "C1", System.Drawing.Color.Red);

                    // Přidání nově vytvořené anotace do seznamu anotací
                    anotaces.Add(novaAnotace);

                    // Získání seznamu pixelů pro danou anotaci
                    JArray pixelsArray = (JArray)annotation.Value;

                    // Vytvoření prázdného plátna pro anotaci
                    novaAnotace.CreateEmptyCanvas(originalPicture.PixelHeight, originalPicture.PixelWidth);

                    // Nastavení pixelů na plátno anotace
                    foreach (JObject pixelObj in pixelsArray)
                    {
                        int x = (int)pixelObj["Item1"];
                        int y = (int)pixelObj["Item2"];

                        // Nastavení pixelu na dané pozici
                        novaAnotace.SetPixel(x, y, novaAnotace.Color);
                    }
                }
            }
        }





        public void UpdateSelectedAnotaceCanvas(WriteableBitmap bitmapSource) {
            if (activeAnotace != null)
            {
                activeAnotace.UpdateCanvas(bitmapSource);
            }
        }


        public void ClearActiveAnotace() {
            if (activeAnotace != null)
            {
                activeAnotace.ClearCanvas();
            }
        }


        public void SelectActiveAnotace(int id)
        {
            activeAnotace = FindAnotaceById(id);
        }


        public string ActiveAnotaceId() {
            if (activeAnotace != null)
            {
                return activeAnotace.Id.ToString();
            }
            return null;
        }
        public System.Windows.Media.Color ActiveAnotaceColor() {
            return System.Windows.Media.Color.FromArgb(activeAnotace.Color.A, activeAnotace.Color.R, activeAnotace.Color.G, activeAnotace.Color.B);
        }
        public WriteableBitmap ActiveAnotaceImage() {
            return activeAnotace.GetCanvas();
        }


        Anotace FindAnotaceById(int idAnotace)
        {
            Anotace? foundAnotace = anotaces.Find(anotace => anotace.Id == idAnotace);
            if (foundAnotace != null)
            {
                return foundAnotace;
            }
            else
            {
// !!!!!!
// POUZE SE ZOBRAZÍ PRÁZDNÝ CANVAS!!
// NEVYHODÍ VÝJIMKU

                //throw new InvalidOperationException($"Anotace with ID {idAnotace} not found.");
                return null;

            }
        }


        public BitmapImage? GetOriginalPicture()
        {
            return originalPicture;
        }


        public static Project GetInstance()
        {
            if (instance == null) {
                instance = new Project();
            }
            return instance;
        }

        public List<string> ChooseNewProject()
        {
            return folderUtilityManager.ChooseNewProject();
        }

        public List<string> ChooseContinueAnotation()
        {
            return folderUtilityManager.ChooseContinueAnotation();
        }

        public List<string> ChooseValidation()
        {
            return folderUtilityManager.ChooseValidation();
        }

        public void Choose(string path, string projectType)
        {
            string newPath = Path.Combine(folderUtilityManager.tempPath, projectType, path);
            if (projectType == "dicoms")
            {
                CreateNewProject(newPath);
            }
            else
            {
                LoadProject(newPath);
            }

            originalPicture = folderUtilityManager.GetImage();
        }
    }
}
