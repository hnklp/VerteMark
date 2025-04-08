using System.Windows.Media.Imaging;
using System.IO;
using VerteMark.ObjectClasses.FolderClasses;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using static VerteMark.ObjectClasses.Anotace;


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
        User loggedInUser; // Info o uživateli
        List<Anotace> anotaces; // Objekty anotace
        Anotace? activeAnotace;
        BitmapImage? originalPicture; // Fotka toho krku
        JsonManipulator? jsonManip;
        bool IsAnotated = false;
        bool newProject;
        public bool saved;
        public bool anyProjectAvailable;

        public Project() {
            anotaces = new List<Anotace>();
            originalPicture = new BitmapImage();
            folderUtilityManager = new FolderUtilityManager();
            jsonManip = new JsonManipulator();
            newProject = false;
            saved = false;
            anyProjectAvailable = true;
        }

        public void CropOriginalPicture(BitmapSource image)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder(); // nebo jiný vhodný encoder podle vašich potřeb
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            SetOriginalPicture(bitmapImage);
        }

        public bool ChooseProjectFolder(string path) {
            return folderUtilityManager.ExtractZip(path);
        }

        public bool isAnyProjectAvailable()
        {
            return anyProjectAvailable;
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

            List<JArray> annotations = jsonManip.UnpackJson(jsonString);
            if (annotations != null || annotations.Count > 0) {

                // První JArray -> Just Annotated
                JArray canvasAnnotations = annotations[0];
                JArray validations = annotations[1];

                originalPicture = folderUtilityManager.GetImage();
                List<string> created = LoadAnnotations(canvasAnnotations, validations);
                AddMissingAnnotations(created);
            }
            else {
                // UPOZORNENI, ZE ANOTACE NEBYLY NACTENY - NEMOHL SE IDENTIFIKOVAT SOUBOR
                CreateNewAnotaces();
            }

            anotaces = anotaces.OrderBy(a => a.Id).ToList();
        }

        public void SaveProject(int savingParameter) {

            // pred ulozenim - pokud je uzivatel anotator:
                    // zeptat se, zda je anotace zcela dokoncena a projekt je pripraven k validaci

            // kombinace starsi metody SaveJson()
            List<Dictionary<string, List<Tuple<int, int>>>> dicts = new List<Dictionary<string, List<Tuple<int, int>>>>();
            List<string>? valids = new List<string>();

            foreach (Anotace anot in anotaces) {
                dicts.Add(anot.GetAsDict());
                if (anot.IsValidated) {
                    valids.Add(anot.Id);
                }
            }
            folderUtilityManager.Save(loggedInUser, newProject, 
                originalPicture, jsonManip.ExportJson(loggedInUser, dicts, valids), 
                savingParameter); // bere tyto parametry pro ulozeni metadat
            this.saved = true;
            this.anyProjectAvailable = folderUtilityManager.anyProjectAvailable(loggedInUser.Validator);
        }

        public void DeleteTempFolder() {
            folderUtilityManager.DeleteTempFolder();
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

        // *****************************************************************************************
        List<string> LoadAnnotations(JArray annotations, JArray validations) {
            List<string> createdIds = new List<string>();
            HashSet<string> validationSet = validations.ToObject<HashSet<string>>();

            foreach (JObject annotationObj in annotations) {
                foreach (var annotation in annotationObj) {
                    string annotationId = annotation.Key;
                    createdIds.Add(annotationId);
                    CreateNewAnnotation(annotationId);

                    Anotace createdAnnotation = FindAnotaceById(annotationId);

                    if (createdAnnotation.Type == AnotaceType.Implant)
                    {
                        createdAnnotation.LoadAnnotationCanvas((JArray)annotation.Value, originalPicture.PixelWidth, originalPicture.PixelHeight);
                    }

                    if (createdAnnotation.Type == AnotaceType.Vertebra || createdAnnotation.Type == AnotaceType.Fusion)
                    {
                        createdAnnotation.LoadAnnotationPointMarker((JArray)annotation.Value);
                    }


                    if (validationSet.Contains(annotationId)) {
                        createdAnnotation.Validate(true);
                    }
                }
            }
            return createdIds;
        }
        // *****************************************************************************************


        void CreateNewAnotaces() {
            List<string> allIds = new List<string> { "V0", "V1", "V2", "V3", "V4", "V5", "V6", "I0", "F0" };

            foreach (string id in allIds)
            {
                CreateNewAnnotation(id);
            }
        }

        void AddMissingAnnotations(List<string> existingIds) {
            List<string> allIds = new List<string> {"V0", "V1", "V2", "V3", "V4", "V5", "V6", "I0", "F0"};

            foreach (string id in allIds) {
                if (!existingIds.Contains(id)) {
                    CreateNewAnnotation(id);
                }
            }
        }

        void CreateNewAnnotation(string id) {
            // Barva odpovídající danému ID
            System.Drawing.Color color = System.Drawing.Color.DeepPink;
            string name = "";
            AnotaceType type = GetTypeForId(id);
            
            if (type == AnotaceType.Vertebra)
            {
                switch (id) {
                    case "V0":
                        color = System.Drawing.Color.Red;
                        name = "C1";
                        break;
                    case "V1":
                        color = System.Drawing.Color.Orange;
                        name = "C2";
                        break;
                    case "V2":
                        color = System.Drawing.Color.Yellow;
                        name = "C3";
                        break;
                    case "V3":
                        color = System.Drawing.Color.Lime;
                        name = "C4";
                        break;
                    case "V4":
                        color = System.Drawing.Color.Aquamarine;
                        name = "C5";
                        break;
                    case "V5":
                        color = System.Drawing.Color.Aqua;
                        name = "C6";
                        break;
                    case "V6":
                        color = System.Drawing.Color.BlueViolet;
                        name = "C7";
                        break;
                }
            }
            else if (type == AnotaceType.Implant)
            {
                int idInt = ExtractNumericId(id);
                color = System.Drawing.Color.DeepPink;
                name = idInt == 0  ? "Implantát" : "Implantát" + (idInt + 1);   
            }
            else
            {
                int idInt = ExtractNumericId(id);
                color = System.Drawing.Color.Gold;
                name = idInt == 0 ? "Fúze" : "Fúze" + (idInt + 1);
            }

            // Vytvoření nové anotace s odpovídající barvou a názvem
            anotaces.Add(new Anotace(id, name, color, type));
        }

        // Funkce pro získání správného typu podle ID
        public static AnotaceType GetTypeForId(string id)
        {
            if (id.StartsWith("V"))
                return AnotaceType.Vertebra;
            if (id.StartsWith("I"))
                return AnotaceType.Implant;
            if (id.StartsWith("F"))
                return AnotaceType.Fusion;

            return AnotaceType.Vertebra;
        }

        public int ExtractNumericId(string id)
        {
            return int.TryParse(id.Substring(1), out int result) ? result : 0;
        }

        public Anotace CreateNewAnnotation(AnotaceType type)
        {
            var existing = anotaces
                .Where(a => a.Type == type)
                .Select(a => ExtractNumericId(a.Id));

            int nextNumber = existing.Any() ? existing.Max() + 1 : 1;
            string prefix = type == AnotaceType.Fusion ? "F" : "I";
            System.Drawing.Color color = type == AnotaceType.Fusion ? System.Drawing.Color.Gold : System.Drawing.Color.DeepPink;

            var newAnnotation = new Anotace
            (
                prefix + nextNumber,
                $"{(type == AnotaceType.Fusion ? "Fúze" : "Implantát")} {nextNumber + 1}",
                color,
                type
            );

            anotaces.Add(newAnnotation);
            return newAnnotation;
        }

        public WriteableBitmap ActiveAnotaceImage() {
            if (activeAnotace == null) {
                SelectActiveAnotace("V0");
            }
            return activeAnotace.GetCanvas();
        }

        public void PreviewAllAnotaces() {
            foreach(Anotace anot in anotaces) {
                if (activeAnotace != anot) {
                    anot.SetPreviewImage();
                }
            }
        }

        public Anotace FindAnotaceById(string idAnotace) {
            Anotace? foundAnotace = anotaces.Find(anotace => anotace.Id == idAnotace);
            if (foundAnotace != null) {
                return foundAnotace;
            }
            else {
                return null;
            }
        }

        public void ValidateAnnotationByID(string id) {
            Anotace anotace = FindAnotaceById(id);
            if (anotace.IsValidated) {
                anotace.Validate(false);
            }
            else {
                anotace.Validate(true);
            }
        }

        public Anotace SelectActiveAnotace(string id) {
            activeAnotace = FindAnotaceById(id);
            return activeAnotace;
        }


        public string ActiveAnotaceId() {
            if (activeAnotace != null) {
                return activeAnotace.Id;
            }
            return "";
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

                activeAnotace.Points.Clear();
                activeAnotace.Lines.Clear();
            }
        }

        public void ClearAnotace(Anotace annotation)
        {
            annotation.ClearCanvas();
            annotation.Points.Clear();
            annotation.Lines.Clear();   
        }

        public void ClearAllAnotace(Canvas PointCanvas)
        {
            foreach (Anotace annotation in anotaces)
            {
                this.RemovePointsAndConnections(PointCanvas, annotation);
                this.ClearAnotace(annotation);
                this.SetActiveAnotaceIsAnotated(false);
            }
        }


        public void ValidateAll() {
            bool wasValidated = false;
            foreach (Anotace annotation in anotaces) {
                if (annotation.IsValidated) {
                    wasValidated = true;
                }
            }
            if (!wasValidated) {
				foreach (Anotace annotation in anotaces) {
					annotation.Validate(true);
				}
			}
        }
        

        public List<Anotace> GetAnotaces(){
            return anotaces;
        }

        public void DeleteAnnotation(string annotationId) { 
            Anotace annotation = FindAnotaceById(annotationId);
            if (annotation != null)
            {
                anotaces.Remove(annotation);
            }
        }

        public void ChangeAnnotationId(string annotationId, string newId)
        {
            Anotace annotation = FindAnotaceById(annotationId);
            if (annotation != null)
            {
                annotation.SetId(newId);
            }
        }

        public void AddPointActiveAnot(PointMarker point)
        {
            if (activeAnotace != null)
            {
                activeAnotace.Points.Add(point);
            }
        }

        public int GetPointsCount()
        {
            if (activeAnotace != null)
            {
                return activeAnotace.Points.Count;
            }
            return 0;
        }

        public void UpdatePointsScale(double zoomFactor)
        {
            double scale = 1 / zoomFactor;

            foreach (Anotace anotace in anotaces)
                foreach (PointMarker point in anotace.Points)
                {
                    point.UpdateScale(scale);
                }
        }

        public void RemoveActivePointsAndConnections(Canvas canvas)
        {
            if (activeAnotace != null)
            {
                foreach (LineConnection line in activeAnotace.Lines)
                {
                    line.Remove(canvas);
                }
                foreach (PointMarker point in activeAnotace.Points)
                {
                    point.Remove(canvas);
                }
            }
        }

        public void RemovePointsAndConnections(Canvas canvas, Anotace anotace)
        {
            
            foreach (LineConnection line in anotace.Lines)
            {
                line.Remove(canvas);
            }
            foreach (PointMarker point in anotace.Points)
            {
                point.Remove(canvas);
            }
        }

        public void LoadPointMarkers(Canvas canvas)
        {
            foreach (Anotace anotace in anotaces)
            {
                for (int i = 0; i < anotace.Points.Count; i++)
                {
                    anotace.Points[i].DrawPointMarker(canvas);
                    DrawLineConnection(canvas, i + 1, anotace);
                }
            }


        }

        public void DrawLineConnection(Canvas pointCanvas, int index, Anotace? anotace = null)
        {
            if (anotace == null && activeAnotace != null)
            {
                anotace = this.activeAnotace;
            }

            if (anotace == null) return;

            if (index < 2) return;

            var color = Color.FromArgb(anotace.Color.A, anotace.Color.R, anotace.Color.G, anotace.Color.B);
            Brush brush = new SolidColorBrush(color);

            if (index == 2)
            {
                var start = anotace.Points[index - 2]; // Poslední bod
                var end = anotace.Points[index - 1]; // Aktuální bod

                if (start == null || end == null) return;

                var line = new LineConnection(start, end, pointCanvas, brush);
                anotace.Lines.Add(line);

                return;
            }

            // Odstranění poslední čáry, pokud je připojený počet bodů sudý
            if (index % 2 == 0)
            {
                var lastLine = anotace.Lines[^1];
                if (lastLine == null) return;

                lastLine.Remove(pointCanvas);
                anotace.Lines.RemoveAt(anotace.Lines.Count - 1);
            }

            var lastLastPoint = anotace.Points[index - 3]; // Předposlední bod
            var lastPoint = anotace.Points[index - 2]; // Poslední bod
            var point = anotace.Points[index - 1]; // Aktuální bod

            if (lastLastPoint == null || lastPoint == null || point == null) return;

            var line1 = new LineConnection(lastLastPoint, point, pointCanvas, brush);
            anotace.Lines.Add(line1);

            var line2 = new LineConnection(lastPoint, point, pointCanvas, brush);
            anotace.Lines.Add(line2);
        }

        public void CropPreviewImages(double width, double height)
        {
            foreach (Anotace anotace in anotaces)
            {
                anotace.PreviewImage.Width = width;
                anotace.PreviewImage.Height = height;
            }
        }

        /*
        * ===========
        * User metody
        * ===========
        */

        public void LoginNewUser(string id, bool validator) {
            loggedInUser = new User(id, validator);
            Debug.WriteLine(loggedInUser);
        }

        public User GetLoggedInUser() {
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

        public List<string> InvalidDicoms()
        {
            return folderUtilityManager.InvalidDicoms();
        }

        public List<string> ValidatedDicoms()
        {
            return folderUtilityManager.ValidatedDicoms();
        }
    }
}
