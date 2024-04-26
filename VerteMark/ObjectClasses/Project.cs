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
using System.Windows;
using System.Runtime.InteropServices;

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

            originalPicture = folderUtilityManager.GetImage();

            JObject jsonObject = JObject.Parse(jsonContent);

            // Získání seznamu anotací ze zpracovaného JObject
            JArray annotationsArray = (JArray)jsonObject["Annotations"];

            // Předání seznamu anotací pro další zpracování
            LoadAnnotations(annotationsArray);
        }


        public async void SaveProject()
        {

            // zavolá filemanager aby uložil všechny instance (bude na to možná pomocná třída co to dá dohromady jako 1 json a 1 csv)
            // záležitosti správných složek a správných formátů souborů má na starost filemanager
            // ZKOUSKA UKLADANI TEMP DO ZIP
            SaveJson();
            await folderUtilityManager.Save(); // bude brat parametr string json 
        }


        async void SaveJson() {
            List<Dictionary<string, List<Tuple<int, int>>>> dicts = new List<Dictionary<string, List<Tuple<int, int>>>>();
            foreach (Anotace anot in anotaces) {
                dicts.Add(anot.GetAsDict());
            }

            string json = jsonManip.ExportJson(loggedInUser, dicts);

            await folderUtilityManager.SaveJson(json);
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
            for(int i = 0; i <= 7; i++) {
                CreateAnnotation(i);
            }
        }


        void LoadAnnotations(JArray annotationsArray)
        {
            List<int> createdIds = new List<int>();

            foreach (JObject annotationObj in annotationsArray)
            {
                foreach (var annotation in annotationObj)
                {
                    int annotationId = int.Parse(annotation.Key);
                    createdIds.Add(annotationId);
                    CreateAnnotation(annotationId);
                    SelectActiveAnotace(annotationId);
                    activeAnotace.CreateEmptyCanvas(originalPicture.PixelWidth, originalPicture.PixelHeight);

                    JArray pixelsArray = (JArray)annotation.Value;

                    // Vytvoření pole pixelů
                    byte[] pixels = new byte[originalPicture.PixelWidth * originalPicture.PixelHeight * 4];

                    foreach (JObject pixelObj in pixelsArray)
                    {
                        int x = (int)pixelObj["Item1"];
                        int y = (int)pixelObj["Item2"];

                        // Nastavení barev pixelu
                        int index = (y * originalPicture.PixelWidth + x) * 4;
                        pixels[index] = activeAnotace.Color.B;
                        pixels[index + 1] = activeAnotace.Color.G;
                        pixels[index + 2] = activeAnotace.Color.R;
                        pixels[index + 3] = activeAnotace.Color.A;
                    }

                    // Vytvoření nového WriteableBitmap
                    WriteableBitmap newBitmap = new WriteableBitmap(originalPicture.PixelWidth, originalPicture.PixelHeight, 96, 96, PixelFormats.Bgra32, null);
                    // Zkopírování pole pixelů do nového WriteableBitmap
                    newBitmap.WritePixels(new Int32Rect(0, 0, newBitmap.PixelWidth, newBitmap.PixelHeight), pixels, newBitmap.PixelWidth * 4, 0);

                    // Aktualizace canvasu pomocí metody UpdateCanvas
                    activeAnotace.UpdateCanvas(newBitmap);
                    Debug.WriteLine("---ORIGINAL PICTURE LOAD WIDTH & HEIGHT----");
                    Debug.WriteLine(originalPicture.PixelWidth);
                    Debug.WriteLine(originalPicture.PixelHeight);
                    Debug.WriteLine("-------------------");
                }
            }
            AddMissingAnnotations(createdIds);
            SelectActiveAnotace(0);
        }




        void CreateAnnotation(int id)
        {
                // Barva odpovídající danému ID
                System.Drawing.Color color;
                string name;
                switch (id)
                {
                    case 0:
                        color = System.Drawing.Color.Red;
                        name = "C" + id;
                        break;
                    case 1:
                        color = System.Drawing.Color.Orange;
                        name = "C" + id;
                        break;
                    case 2:
                        color = System.Drawing.Color.Yellow;
                        name = "C" + id;
                        break;
                    case 3:
                        color = System.Drawing.Color.Lime;
                        name = "C" + id;
                        break;
                    case 4:
                        color = System.Drawing.Color.Aquamarine;
                        name = "C" + id;
                        break;
                    case 5:
                        color = System.Drawing.Color.Aqua;
                        name = "C" + id;
                        break;
                    case 6:
                        color = System.Drawing.Color.BlueViolet;
                        name = "C" + id;
                        break;
                    case 7:
                        color = System.Drawing.Color.DeepPink;
                        name = "Implantát";
                        break;
                    default:
                        // Pokud je ID mimo rozsah, použije se defaultní barva a název
                        color = System.Drawing.Color.Black;
                        name = "Unknown";
                        break;
                }

                // Vytvoření nové anotace s odpovídající barvou a názvem
                anotaces.Add(new Anotace(id, name, color));
        }


        void AddMissingAnnotations(List<int> existingIds)
        {
            // Seznam všech možných ID od 0 do 7
            List<int> allIds = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

            // Pro každé ID, které není v seznamu existingIds, přidej novou anotaci
            foreach (int id in allIds)
            {
                if (!existingIds.Contains(id))
                {
                    CreateAnnotation(id);
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
            Debug.WriteLine("--------------------------");
            Debug.WriteLine("ORIGINAL PICTURE WIDTH , HEIGHT");
            Debug.WriteLine(originalPicture.PixelWidth);
            Debug.WriteLine(originalPicture.PixelHeight);
            Debug.WriteLine("--------------------------");
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
