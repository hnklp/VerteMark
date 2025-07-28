using System.Windows.Media.Imaging;
using System;
using System.Drawing;
using System.IO;
using Dicom;
using Dicom.Imaging;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VerteMark.ObjectClasses.FolderClasses {
    /// <summary>
    /// Správa a manipulace se soubory pro projekt.
    /// 
    /// Zahrnuje:
    /// * Načítání dat z DICOM souborů.
    /// * Konverzi DICOM dat do požadovaných formátů.
    /// * Ukládání projektu a souvisejících souborů.
    /// </summary>
    /// 
    internal class FileManager {
        public string outputPath;
        public string? dicomPath;
        public string? pngPath;
        public string? jsonPath;
        public string? metaPath;
        private string key;
        public string? fileName;


        public FileManager() {
            outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            key = "XX"; //specialni oznaceni metadat pro UJEP (zatim podle zadani pouzivame XX)
        }

        public void TransformPaths() {
            pngPath = Path.Combine(outputPath, Path.GetFileName(pngPath));
            jsonPath = Path.Combine(outputPath, Path.GetFileName(jsonPath));
        }

        public void CopyMetaFile(string sourcePath) {
            if (!File.Exists(sourcePath)) {
                throw new FileNotFoundException($"The source file does not exist: {sourcePath}");
            }

            // Ensure the destination directory exists
            string destinationDirectory = Path.GetDirectoryName(metaPath);
            if (!Directory.Exists(destinationDirectory)) {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, metaPath, overwrite: true);
        }

        public void SaveCroppedImage(BitmapImage image) {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (FileStream stream = new FileStream(pngPath, FileMode.Create)) {
                // Uložení bitmapy do souboru pomocí encoderu
                encoder.Save(stream);
            }
        }

        public void SaveJson(string jsonString, User user)
        {
            string? path = jsonPath;

            if (path != null)
            {
                string directory = Path.GetDirectoryName(path)!;
                string fileName = Path.GetFileName(path);

                // Odeber případný stávající prefix
                if (fileName.StartsWith("v_") || fileName.StartsWith("a_"))
                {
                    fileName = fileName.Substring(2);
                }

                // Přidej nový prefix podle role
                string prefix = user.Validator ? "v_" : "a_";
                path = Path.Combine(directory, prefix + fileName);

                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(jsonString);
                }
            }
        }


        // nacte obrazek pomoci cesty pngPath
        public BitmapImage LoadBitmapImage() {
            try {
                BitmapImage bitmapImage = new BitmapImage();

                // Set BitmapImage properties
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.UriSource = new Uri(pngPath);
                bitmapImage.EndInit();

                // Ensure the BitmapImage is fully loaded before returning
                bitmapImage.Freeze();

                return bitmapImage;
            }

            catch (Exception ex) {
                // Handle any exceptions, e.g., file not found or invalid image format
                return null;
            }
        }


        /*
        * ============================================
        * Specialne pri nacitani rozdelaneho projektu:
        * ============================================
        */

        public void AddUserActionToMetadata(User user) {
            if (!File.Exists(metaPath)) {
                return;
            }
            DateTime currentTime = DateTime.Now;
            string jsonMetadata = File.ReadAllText(metaPath);
            JObject allMetadata = JObject.Parse(jsonMetadata);
            if (allMetadata["History"] == null) {
                allMetadata["History"] = new JObject();
            }
            string historyEntryKey = currentTime.ToString("dd. MM. yyyy HH:mm:ss");
            if (((JObject)allMetadata["History"]).ContainsKey(historyEntryKey)) {
                historyEntryKey += "." + currentTime.Millisecond.ToString();
            }
            JObject newEntry = new JObject(
                new JProperty("id", user.UserID),
                new JProperty("action", user.Validator ? "VALIDATION" : "ANNOTATION")
            );
            ((JObject)allMetadata["History"]).Add(historyEntryKey, newEntry);
            string updatedJsonMetadata = allMetadata.ToString(Formatting.Indented);
            File.WriteAllText(metaPath, updatedJsonMetadata);
        }

        /*
        * ======================================
        *  Specialne pri nacitani dicom souboru:
        * ======================================
        */

        // Kdyz se nacte DICOM, vytvori to slozku, ktera se nastavi jako outputPath
        public void CreateOutputFile(string outputDirectoryName) {
            if (outputPath != null) {
                string fullPath = Path.Combine(outputPath, outputDirectoryName);
                Directory.CreateDirectory(fullPath);
                outputPath = fullPath;
            }
        }


        // extrahuje png obrazek z dicom souboru a ulozi ho do slozky
        // nastavi instanci pngPath
        public void ExtractImageFromDicom() {
            DicomFile dicomFile = DicomFile.Open(dicomPath);
            DicomImage image = new DicomImage(dicomFile.Dataset);
            Bitmap bmp = image.RenderImage().As<Bitmap>();

            fileName = Path.GetFileName(dicomPath);
            pngPath = Path.Combine(outputPath, fileName + ".png");
            jsonPath = Path.Combine(outputPath, fileName + ".json");
            bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
        }


        // extrahuje metadata do output slozky - vola se pouze pokud je vytvoreny novy projekt
        public void ExtractAndSaveMetadata(User user) {
            if (!File.Exists(dicomPath)) {
                return;
            }
            string csvFileName = key + "-" + Path.GetFileName(dicomPath) + ".meta";
            metaPath = Path.Combine(outputPath, csvFileName);

            DicomFile dicomFile = DicomFile.Open(dicomPath);

            var allMetadata = new Dictionary<string, object>();
            var dicomMetadata = new Dictionary<string, Dictionary<string, string>>();
            foreach (DicomItem item in dicomFile.Dataset) {
                var metadataItem = new Dictionary<string, string>();
                metadataItem["Tag"] = item.Tag.ToString();
                metadataItem["Value"] = item.ToString();
                metadataItem["VR"] = item.ValueRepresentation.Code;
                string description = DicomDictionary.Default[item.Tag].Name;
                dicomMetadata[description] = metadataItem;
            }
            allMetadata["DicomMetadata"] = dicomMetadata;

            DateTime theTime = DateTime.Now;

            var history = new Dictionary<string, Dictionary<string, string>>();
            history[theTime.ToString("dd. MM. yyyy HH:mm:ss")] = new Dictionary<string, string>{ 
                { "id", user.UserID }, { "action", user.Validator ? "VALIDATION" : "ANNOTATION" } };
            allMetadata["History"] = history;

            string jsonAllMetadata = JsonConvert.SerializeObject(allMetadata, Formatting.Indented);

            File.WriteAllText(metaPath, jsonAllMetadata);
        }
    }
}
