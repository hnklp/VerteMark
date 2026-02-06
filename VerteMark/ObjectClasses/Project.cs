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
        public FolderUtilityManager folderUtilityManager;
        User loggedInUser; // Info o uživateli
        List<Anotace> anotaces; // Objekty anotace
        Anotace? activeAnotace;
        BitmapImage? originalPicture; // Fotka toho krku
        JsonManipulator? jsonManip;
        bool IsAnotated = false;
        bool newProject;
        public bool saved;
        public bool anyProjectAvailable;
        public string fileName;
        public string projectType;

        public Project() {
            anotaces = new List<Anotace>();
            originalPicture = new BitmapImage();
            folderUtilityManager = new FolderUtilityManager();
            jsonManip = new JsonManipulator();
            newProject = false;
            saved = false;
            anyProjectAvailable = true;
        }

        /// <summary>
        /// Ořízne původní obrázek podle zadané bitmapy.
        /// </summary>
        /// <param name="image">Bitmapa obsahující oříznutý obrázek</param>
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

        /// <summary>
        /// Vybere a extrahuje projekt ze ZIP souboru (.vmk).
        /// </summary>
        /// <param name="path">Cesta k ZIP souboru projektu</param>
        /// <returns>True, pokud byla extrakce úspěšná, jinak false</returns>
        public bool ChooseProjectFolder(string path) {
            return folderUtilityManager.ExtractZip(path);
        }

        /// <summary>
        /// Zjistí, zda je k dispozici nějaký projekt pro aktuálního uživatele.
        /// </summary>
        /// <returns>True, pokud je k dispozici alespoň jeden projekt, jinak false</returns>
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
        /// <summary>
        /// Vytvoří nový debug projekt pro testování bez DICOM souboru.
        /// Načte debug obrázek z Pictures/debug.png.
        /// </summary>
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

        /// <summary>
        /// Uloží aktuální projekt se všemi anotacemi, metadaty a obrázky.
        /// </summary>
        /// <param name="savingParameter">Parametr určující cílovou složku: 0 = to_anotate, 1 = to_validate, 2 = validated, 3 = invalid</param>
        /// <param name="button">Název tlačítka, které spustilo uložení (pro metadata)</param>
        public void SaveProject(int savingParameter, string button) {

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
                savingParameter, button); // bere tyto parametry pro ulozeni metadat
            this.saved = true;
            this.anyProjectAvailable = folderUtilityManager.anyProjectAvailable(loggedInUser.Validator);
        }

        /// <summary>
        /// Smaže dočasnou složku projektu.
        /// </summary>
        public void DeleteTempFolder() {
            folderUtilityManager.DeleteTempFolder();
        }

        /// <summary>
        /// Získá původní obrázek projektu.
        /// </summary>
        /// <returns>Bitmapa původního obrázku nebo null, pokud není načten</returns>
        public BitmapImage? GetOriginalPicture() {
            return originalPicture;
        }

        /// <summary>
        /// Nastaví původní obrázek projektu.
        /// </summary>
        /// <param name="image">Bitmapa obrázku k nastavení</param>
        public void SetOriginalPicture(BitmapImage image) {
            originalPicture = image;
        }

        /// <summary>
        /// Získá singleton instanci třídy Project.
        /// </summary>
        /// <returns>Jediná instance Project</returns>
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
                    JArray arr = (JArray)annotation.Value;
                    if (arr.Count > 0)
                    {
                        createdAnnotation.SetIsAnotated(true);
                    }

                    if (createdAnnotation.Type == AnotaceType.Implant || createdAnnotation.Type == AnotaceType.Fusion)
                    {
                        createdAnnotation.LoadAnnotationCanvas((JArray)annotation.Value, originalPicture.PixelWidth, originalPicture.PixelHeight);
                    }

                    if (createdAnnotation.Type == AnotaceType.Vertebra)
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
                        color = System.Drawing.Color.CornflowerBlue;
                        name = "C6";
                        break;
                    case "V6":
                        color = System.Drawing.Color.LightPink;
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

        /// <summary>
        /// Určí typ anotace podle jejího ID.
        /// </summary>
        /// <param name="id">ID anotace (např. "V0", "I1", "F0")</param>
        /// <returns>Typ anotace podle předpony ID (V=Vertebra, I=Implant, F=Fusion)</returns>
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

        /// <summary>
        /// Extrahuje číselnou část z ID anotace.
        /// </summary>
        /// <param name="id">ID anotace (např. "V3" -> 3)</param>
        /// <returns>Číselná část ID nebo 0, pokud parsování selže</returns>
        public int ExtractNumericId(string id)
        {
            return int.TryParse(id.Substring(1), out int result) ? result : 0;
        }

        /// <summary>
        /// Vytvoří novou anotaci zadaného typu s automaticky vygenerovaným ID.
        /// </summary>
        /// <param name="type">Typ anotace (Implant nebo Fusion)</param>
        /// <returns>Nově vytvořená anotace</returns>
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

        /// <summary>
        /// Získá bitmapu aktivní anotace.
        /// </summary>
        /// <returns>WriteableBitmap aktivní anotace</returns>
        public WriteableBitmap ActiveAnotaceImage() {
            if (activeAnotace == null) {
                SelectActiveAnotace("V0");
            }
            return activeAnotace.GetCanvas();
        }

        /// <summary>
        /// Nastaví preview obrázky pro všechny anotace kromě aktivní.
        /// </summary>
        public void PreviewAllAnotaces() {
            foreach(Anotace anot in anotaces) {
                if (activeAnotace != anot) {
                    anot.SetPreviewImage();
                }
            }
        }

        /// <summary>
        /// Najde anotaci podle jejího ID.
        /// </summary>
        /// <param name="idAnotace">ID anotace k vyhledání</param>
        /// <returns>Nalezená anotace nebo null, pokud nebyla nalezena</returns>
        public Anotace FindAnotaceById(string idAnotace) {
            Anotace? foundAnotace = anotaces.Find(anotace => anotace.Id == idAnotace);
            if (foundAnotace != null) {
                return foundAnotace;
            }
            else {
                return null;
            }
        }

        /// <summary>
        /// Přepne stav validace anotace podle ID.
        /// </summary>
        /// <param name="id">ID anotace k validaci/zrušení validace</param>
        public void ValidateAnnotationByID(string id) {
            Anotace anotace = FindAnotaceById(id);
            if (anotace.IsValidated) {
                anotace.Validate(false);
            }
            else {
                anotace.Validate(true);
            }
        }

        /// <summary>
        /// Nastaví aktivní anotaci podle ID.
        /// </summary>
        /// <param name="id">ID anotace k aktivaci</param>
        /// <returns>Vybraná anotace</returns>
        public Anotace SelectActiveAnotace(string id) {
            activeAnotace = FindAnotaceById(id);
            return activeAnotace;
        }

        /// <summary>
        /// Získá ID aktivní anotace.
        /// </summary>
        /// <returns>ID aktivní anotace nebo prázdný řetězec, pokud není žádná aktivní</returns>
        public string ActiveAnotaceId() {
            if (activeAnotace != null) {
                return activeAnotace.Id;
            }
            return "";
        }

        /// <summary>
        /// Získá barvu aktivní anotace ve formátu WPF Color.
        /// </summary>
        /// <returns>Barva aktivní anotace</returns>
        public System.Windows.Media.Color ActiveAnotaceColor() {
            return System.Windows.Media.Color.FromArgb(activeAnotace.Color.A, activeAnotace.Color.R, activeAnotace.Color.G, activeAnotace.Color.B);
        }

        /// <summary>
        /// Aktualizuje canvas aktivní anotace novou bitmapou.
        /// </summary>
        /// <param name="bitmapSource">Nová bitmapa pro canvas anotace</param>
        public void UpdateSelectedAnotaceCanvas(WriteableBitmap bitmapSource) {
            if (activeAnotace != null) {
                activeAnotace.UpdateCanvas(bitmapSource);
            }
        }

        /// <summary>
        /// Vymaže obsah aktivní anotace (canvas, body a čáry).
        /// </summary>
        public void ClearActiveAnotace() {
            if (activeAnotace != null) {
                activeAnotace.ClearCanvas();

                activeAnotace.Points.Clear();
                activeAnotace.Lines.Clear();
            }
        }

        /// <summary>
        /// Vymaže obsah zadané anotace.
        /// </summary>
        /// <param name="annotation">Anotace k vymazání</param>
        public void ClearAnotace(Anotace annotation)
        {
            annotation.ClearCanvas();
            annotation.Points.Clear();
            annotation.Lines.Clear();   
        }

        /// <summary>
        /// Vymaže všechny anotace v projektu.
        /// </summary>
        /// <param name="PointCanvas">Canvas pro odstranění bodových markerů</param>
        public void ClearAllAnotace(Canvas PointCanvas)
        {
            foreach (Anotace annotation in anotaces)
            {
                this.RemovePointsAndConnections(PointCanvas, annotation);
                this.ClearAnotace(annotation);
                this.SetActiveAnotaceIsAnotated(false);
            }
        }

        /// <summary>
        /// Validuje všechny anotace, pokud ještě nebyly validovány.
        /// </summary>
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

        /// <summary>
        /// Získá seznam všech anotací v projektu.
        /// </summary>
        /// <returns>Seznam všech anotací</returns>
        public List<Anotace> GetAnotaces(){
            return anotaces;
        }

        /// <summary>
        /// Smaže anotaci podle ID.
        /// </summary>
        /// <param name="annotationId">ID anotace ke smazání</param>
        public void DeleteAnnotation(string annotationId) { 
            Anotace annotation = FindAnotaceById(annotationId);
            if (annotation != null)
            {
                anotaces.Remove(annotation);
            }
        }

        /// <summary>
        /// Změní ID anotace na nové ID.
        /// </summary>
        /// <param name="annotationId">Původní ID anotace</param>
        /// <param name="newId">Nové ID anotace</param>
        public void ChangeAnnotationId(string annotationId, string newId)
        {
            Anotace annotation = FindAnotaceById(annotationId);
            if (annotation != null)
            {
                annotation.SetId(newId);
            }
        }

        /// <summary>
        /// Přidá bodový marker do aktivní anotace.
        /// </summary>
        /// <param name="point">Bodový marker k přidání</param>
        public void AddPointActiveAnot(PointMarker point)
        {
            if (activeAnotace != null)
            {
                activeAnotace.Points.Add(point);
            }
        }

        /// <summary>
        /// Získá počet bodů v aktivní anotaci.
        /// </summary>
        /// <returns>Počet bodů nebo 0, pokud není žádná aktivní anotace</returns>
        public int GetPointsCount()
        {
            if (activeAnotace != null)
            {
                return activeAnotace.Points.Count;
            }
            return 0;
        }

        /// <summary>
        /// Aktualizuje měřítko všech bodových markerů podle faktoru zoomu.
        /// </summary>
        /// <param name="zoomFactor">Faktor zoomu (1.0 = 100%)</param>
        public void UpdatePointsScale(double zoomFactor)
        {
            double scale = 1 / zoomFactor;

            foreach (Anotace anotace in anotaces)
                foreach (PointMarker point in anotace.Points)
                {
                    point.UpdateScale(scale);
                }
        }

        /// <summary>
        /// Zrcadlí původní obrázek horizontálně.
        /// </summary>
        public void MirrorOriginalPicture()
        {
            if (originalPicture == null)
                return;

            TransformedBitmap transformedBitmap = new TransformedBitmap(
                originalPicture,
                new ScaleTransform(-1, 1, 0, 0)
            );

            BitmapImage mirroredImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                encoder.Save(stream);
                stream.Position = 0;

                mirroredImage.BeginInit();
                mirroredImage.CacheOption = BitmapCacheOption.OnLoad;
                mirroredImage.StreamSource = stream;
                mirroredImage.EndInit();
                mirroredImage.Freeze();
            }

            originalPicture = mirroredImage;
        }

        /// <summary>
        /// Odstraní všechny body a čáry aktivní anotace z canvasu.
        /// </summary>
        /// <param name="canvas">Canvas, ze kterého se mají odstranit prvky</param>
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

        /// <summary>
        /// Odstraní všechny body a čáry zadané anotace z canvasu.
        /// </summary>
        /// <param name="canvas">Canvas, ze kterého se mají odstranit prvky</param>
        /// <param name="anotace">Anotace, jejíž prvky se mají odstranit</param>
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

        /// <summary>
        /// Odstraní poslední bod aktivní anotace a všechny čáry k němu připojené.
        /// </summary>
        /// <param name="canvas">Canvas, ze kterého se má bod odstranit</param>
        public void RemoveActiveLastPoint(Canvas canvas)
        {
            if (activeAnotace != null)
            {
                PointMarker lastPoint = activeAnotace.Points[^1];
                lastPoint.Remove(canvas);

                // delete all lines connected to last point
                var linesToRemove = activeAnotace.Lines
                    .Where(l => l != null && l._endPoint == lastPoint)
                    .ToList();

                foreach (LineConnection line in linesToRemove)
                {
                    line.Remove(canvas);
                    activeAnotace.Lines.Remove(line);
                }

                activeAnotace.Points.Remove(lastPoint);
            }
        }

        /// <summary>
        /// Načte všechny bodové markery ze všech anotací na canvas.
        /// </summary>
        /// <param name="canvas">Canvas pro vykreslení markerů</param>
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

        /// <summary>
        /// Vykreslí čáru spojující body v anotaci podle indexu.
        /// </summary>
        /// <param name="pointCanvas">Canvas pro vykreslení čáry</param>
        /// <param name="index">Index bodu (určuje, které body spojit)</param>
        /// <param name="anotace">Anotace, pro kterou se má čára vykreslit. Pokud je null, použije se aktivní anotace.</param>
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

        /// <summary>
        /// Ořízne preview obrázky všech anotací na zadané rozměry.
        /// </summary>
        /// <param name="width">Nová šířka preview obrázku</param>
        /// <param name="height">Nová výška preview obrázku</param>
        public void CropPreviewImages(double width, double height)
        {
            foreach (Anotace anotace in anotaces)
            {
                anotace.PreviewImage.Width = width;
                anotace.PreviewImage.Height = height;
            }
        }

        /// <summary>
        /// Zjistí, zda je projekt v režimu pouze pro čtení.
        /// Projekt je read-only, pokud je uživatel anotátor a existují validované soubory.
        /// </summary>
        /// <returns>True, pokud je projekt read-only, jinak false</returns>
        public bool IsReadOnly()
        {
            if (loggedInUser.Validator)
            {
                return false;
            }

            string directory = folderUtilityManager.fileManager.outputPath;

            if (!Directory.Exists(directory))
                return false;

            string[] files = Directory.GetFiles(directory, "v_*.json");
            return files.Length > 0;
        }

        /*
        * ===========
        * User metody
        * ===========
        */

        /// <summary>
        /// Přihlásí nového uživatele do systému.
        /// </summary>
        /// <param name="id">Identifikátor uživatele</param>
        /// <param name="validator">True, pokud je uživatel validátor, false pokud anotátor</param>
        public void LoginNewUser(string id, bool validator) {
            loggedInUser = new User(id, validator);
            Debug.WriteLine(loggedInUser);
        }

        /// <summary>
        /// Získá přihlášeného uživatele.
        /// </summary>
        /// <returns>Instance přihlášeného uživatele</returns>
        public User GetLoggedInUser() {
            return loggedInUser;
        }

        /*
        * =============================
        * Pouziti v FolderbrowserWindow
        * =============================
        */

        /// <summary>
        /// Vybere a načte projekt podle typu (nový projekt nebo pokračování).
        /// </summary>
        /// <param name="path">Cesta k projektu</param>
        /// <param name="projectType">Typ projektu: "dicoms" pro nový projekt, jinak pro načtení existujícího</param>
        public void Choose(string path, string projectType) {
            string newPath = Path.Combine(folderUtilityManager.tempPath, projectType, path);
            if (projectType == "dicoms") {
                CreateNewProject(newPath);
            }
            else {
                LoadProject(newPath);
            }
            originalPicture = folderUtilityManager.GetImage();
            this.fileName = path;
            this.projectType = projectType;
        }

        /// <summary>
        /// Zjistí, zda je projekt anotován (obsahuje alespoň jednu anotaci s obsahem).
        /// </summary>
        /// <returns>True, pokud je projekt anotován, jinak false</returns>
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

        /// <summary>
        /// Nastaví stav anotace aktivní anotace.
        /// </summary>
        /// <param name="isAnotated">True, pokud je anotace anotována, jinak false</param>
        public void SetActiveAnotaceIsAnotated(bool isAnotated)
        {
            if (activeAnotace != null)
            {
                activeAnotace.SetIsAnotated(isAnotated);
            }
        }

        /// <summary>
        /// Získá seznam DICOM souborů dostupných pro vytvoření nového projektu.
        /// </summary>
        /// <returns>Seznam názvů DICOM souborů</returns>
        public List<string> ChooseNewProject() {
            return folderUtilityManager.ChooseNewProject();
        }

        /// <summary>
        /// Získá seznam projektů dostupných pro pokračování v anotaci.
        /// </summary>
        /// <returns>Seznam názvů projektů</returns>
        public List<string> ChooseContinueAnotation() {
            return folderUtilityManager.ChooseContinueAnotation();
        }

        /// <summary>
        /// Získá seznam projektů dostupných pro validaci.
        /// </summary>
        /// <returns>Seznam názvů projektů</returns>
        public List<string> ChooseValidation() {
            return folderUtilityManager.ChooseValidation();
        }

        /// <summary>
        /// Získá seznam neplatných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů neplatných DICOM souborů</returns>
        public List<string> InvalidDicoms()
        {
            return folderUtilityManager.InvalidDicoms();
        }

        /// <summary>
        /// Získá seznam validovaných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů validovaných DICOM souborů</returns>
        public List<string> ValidatedDicoms()
        {
            return folderUtilityManager.ValidatedDicoms();
        }
    }
}
