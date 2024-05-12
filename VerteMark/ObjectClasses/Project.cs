using System.Windows.Media.Imaging;
using System.IO;
using VerteMark.ObjectClasses.FolderClasses;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Reflection;


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
        JsonManipulator? jsonManip;
        bool IsAnotated = false;
        bool newProject;
        bool saved;



        public Project() {
            anotaces = new List<Anotace>();
            originalPicture = new BitmapImage();
            folderUtilityManager = new FolderUtilityManager();
            jsonManip = new JsonManipulator();
            newProject = false;
            saved = false;
        }


        public bool TryOpeningProject(string path) {
            return folderUtilityManager.ExtractZip(path);
        }


        void CreateNewProject(string path) {
            anotaces = new List<Anotace>();
            newProject = true;
            activeAnotace = null;
            CreateNewAnotaces();
            folderUtilityManager.CreateNewProject(path);
            originalPicture = folderUtilityManager.GetImage();
        }
        public void CreateNewProjectDEBUG() {
            newProject = true;
            CreateNewAnotaces();
            // debug obrázek
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string projectDirectory = Directory.GetParent(assemblyDirectory).Parent.Parent.FullName;
            string relativeImagePath = Path.Combine("Pictures", "debug.png");
            string imagePath = Path.Combine(projectDirectory, relativeImagePath);
            if(File.Exists(imagePath)) {
                BitmapImage bitmap = new BitmapImage(new Uri(imagePath));
                originalPicture = bitmap;
            }
        }

        void LoadProject(string path) {
            newProject = false;
            anotaces = new List<Anotace>();
            activeAnotace = null;
            string jsonString = folderUtilityManager.LoadProject(path);

            JArray annotations = jsonManip.UnpackJson(jsonString);
            if (annotations != null || annotations.Count > 0) {

                originalPicture = folderUtilityManager.GetImage();
                List<int> created = LoadAnnotations(annotations);
                AddMissingAnnotations(created);
            }
            else {
                // UPOZORNENI, ZE ANOTACE NEBYLY NACTENY - NEMOHL SE IDENTIFIKOVAT SOUBOR
                CreateNewAnotaces();
            }
        }


        public void SaveProject() {

            // pred ulozenim - pokud je uzivatel anotator:
                    // zeptat se, zda je anotace zcela dokoncena a projekt je pripraven k validaci
            


            // kombinace starsi metody SaveJson()
            List<Dictionary<string, List<Tuple<int, int>>>> dicts = new List<Dictionary<string, List<Tuple<int, int>>>>();
            foreach (Anotace anot in anotaces) {
                dicts.Add(anot.GetAsDict());
            }
            folderUtilityManager.SaveJson(jsonManip.ExportJson(loggedInUser, dicts));
            folderUtilityManager.Save(loggedInUser, newProject, originalPicture); // bere tyto parametry pro ulozeni metadat
            this.saved = true;
        }



        public BitmapImage? GetOriginalPicture() {
            return originalPicture;
        }

        public void SetOriginalPicture(BitmapImage image) {
            originalPicture = image;
        }


        // Singleton metoda
        public static Project GetInstance() {
            if (instance == null) {
                instance = new Project();
            }
            return instance;
        }


        /*
        * ===========
        * ANOTACE
        * ===========
        */


        List<int> LoadAnnotations(JArray annotations) {
            List<int> createdIds = new List<int>();

            foreach (JObject annotationObj in annotations) {
                foreach (var annotation in annotationObj) {
                    int annotationId = int.Parse(annotation.Key);
                    createdIds.Add(annotationId);
                    CreateNewAnnotation(annotationId);

                    Anotace createdAnnotation = FindAnotaceById(annotationId);

                    //createdAnnotation.CreateEmptyCanvas(originalPicture.PixelWidth, originalPicture.PixelHeight);
                    createdAnnotation.LoadAnnotationCanvas((JArray)annotation.Value, originalPicture.PixelWidth, originalPicture.PixelHeight);
                }
            }
            return createdIds;
        }


        void CreateNewAnotaces() {
            for (int i = 0; i < 8; i++) {
                CreateNewAnnotation(i);
            }
        }


        void AddMissingAnnotations(List<int> existingIds) {
            // Seznam všech možných ID od 0 do 7
            List<int> allIds = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

            foreach (int id in allIds) {
                if (!existingIds.Contains(id)) {
                    CreateNewAnnotation(id);
                    Debug.WriteLine("VYTVARIM NOVE ANOTACE " + id);
                }
            }
        }


            void CreateNewAnnotation(int id) {
            // Barva odpovídající danému ID
            System.Drawing.Color color;
            string name;
            switch (id) {
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


        public WriteableBitmap ActiveAnotaceImage() {
            if (activeAnotace == null) {
                SelectActiveAnotace(0);
            }
            return activeAnotace.GetCanvas();
        }

        public List<WriteableBitmap> AllInactiveAnotaceImages() {
            List <WriteableBitmap> a = new List<WriteableBitmap>();
            foreach(Anotace anot in anotaces) {
                if(activeAnotace != anot) {
                    a.Add(anot.GetCanvas());
                }
            }
            return a;
        }

        Anotace FindAnotaceById(int idAnotace) {
            Anotace? foundAnotace = anotaces.Find(anotace => anotace.Id == idAnotace);
            if (foundAnotace != null) {
                return foundAnotace;
            }
            else {
                //throw new InvalidOperationException($"Anotace with ID {idAnotace} not found.");
                return null;
            }
        }


        public void ValidateAnnotationByID(int id) {
            Anotace anotace = FindAnotaceById(id);
            if (anotace.IsValidated) {
                anotace.Validate(false);
            }
            else {
                anotace.Validate(true);
            }
        }


        public void SelectActiveAnotace(int id) {
            activeAnotace = FindAnotaceById(id);
        }


        public string ActiveAnotaceId() {
            if (activeAnotace != null) {
                return activeAnotace.Id.ToString();
            }
            return null;
        }


        public System.Windows.Media.Color ActiveAnotaceColor() {
            return System.Windows.Media.Color.FromArgb(activeAnotace.Color.A, activeAnotace.Color.R, activeAnotace.Color.G, activeAnotace.Color.B);
        }


        public void UpdateSelectedAnotaceCanvas(WriteableBitmap bitmapSource) {
            if (activeAnotace != null) {
                activeAnotace.UpdateCanvas(bitmapSource);
            }
        }


        public void ClearActiveAnotace() {
            if (activeAnotace != null) {
                activeAnotace.ClearCanvas();
            }
        }


        /*
        * ===========
        * User metody
        * ===========
        */


        public void LoginNewUser(string id, bool validator) {
            loggedInUser = new User(id, validator);
        }


        public void LogoutUser() {
            loggedInUser = null;
        }


        public User? GetLoggedInUser() {
            return loggedInUser;
        }

        /*
        * =============================
        * Pouziti v FolderbrowserWindow
        * =============================
        */

        // ZDE JE VYBRANI CREATE NEBO LOAD !!
        // Zavisi na vybranem souboru v FolderbrowserWindow
        public void Choose(string path, string projectType) {
            string newPath = Path.Combine(folderUtilityManager.tempPath, projectType, path);
            if (projectType == "dicoms") {
                CreateNewProject(newPath);
            }
            else {
                LoadProject(newPath);
            }
            originalPicture = folderUtilityManager.GetImage();
        }

        public bool GetIsAnotated()
        {
            CheckIsAnotated();
            return this.IsAnotated;
        }

        private void CheckIsAnotated()
        {
            this.IsAnotated = false;
            foreach (var anotace in anotaces)
            {
                if (anotace.IsAnotated)
                {
                    this.IsAnotated = true;
                    break;
                }
            }
        }

        public void SetActiveAnotaceIsAnotated(bool isAnotated)
        {
            if (activeAnotace != null)
            {
                activeAnotace.SetIsAnotated(isAnotated);
            }
        }    


        // Vola se z FolderbrowserWindow - vraci vsechny soubory, ktere jsou k dispozici
        public List<string> ChooseNewProject() {
            return folderUtilityManager.ChooseNewProject();
        }

        public List<string> ChooseContinueAnotation() {
            return folderUtilityManager.ChooseContinueAnotation();
        }

        public List<string> ChooseValidation() {
            return folderUtilityManager.ChooseValidation();
        }
    }
}
