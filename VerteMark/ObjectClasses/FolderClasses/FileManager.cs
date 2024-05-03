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


        public FileManager() {
            outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            key = "XX"; //specialni oznaceni metadat pro UJEP (zatim podle zadani pouzivame XX)
        }


        public void SaveProject() {
            
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
                Console.WriteLine("Error loading image: " + ex.Message);
                return null;
            }
        }


        /*
        * ============================================
        * Specialne pri nacitani rozdelaneho projektu:
        * ============================================
        */


        // pujde do funkce JSON maker - ulozeni do output slozky
        public List<Anotace> GetProjectAnotaces() {
            return null;
        }



        public void AddUserActionToMetadata(User user) {
            if (!File.Exists(metaPath)) {
                // .meta file doesn't exist, cannot add user action
                return;
            }

            DateTime currentTime = DateTime.Now;

            // Load existing metadata from .meta file
            string jsonMetadata = File.ReadAllText(metaPath);
            JObject allMetadata = JObject.Parse(jsonMetadata);

            // Check if 'History' already exists, if not, create it
            if (allMetadata["History"] == null) {
                allMetadata["History"] = new JObject();
            }

            // Create new history entry key with date and time
            string historyEntryKey = currentTime.ToString("dd. MM. yyyy HH:mm:ss");

            // Check if the same key already exists in history
            if (((JObject)allMetadata["History"]).ContainsKey(historyEntryKey)) {
                // If the same key exists, append milliseconds to make it unique
                historyEntryKey += "." + currentTime.Millisecond.ToString();
            }

            // Create new history entry
            JObject newEntry = new JObject(
                new JProperty("id", user.UserID),
                new JProperty("action", user.Validator ? "VALIDATION" : "ANNOTATION")
            );

            // Add new history entry to existing ones
            ((JObject)allMetadata["History"]).Add(historyEntryKey, newEntry);

            // Serialize updated metadata back to JSON
            string updatedJsonMetadata = allMetadata.ToString(Formatting.Indented);

            // Write updated metadata back to .meta file
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
            string outputFileName = Path.GetFileNameWithoutExtension(dicomPath) + ".png";
            pngPath = Path.Combine(outputPath, outputFileName);
            bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
        }


        // extrahuje metadata do output slozky - vola se pouze pokud je vytvoreny novy projekt
        public void ExtractAndSaveMetadata(User user) {
            if (!File.Exists(dicomPath)) {
                return;
            }
            string csvFileName = key + "-" + Path.GetFileNameWithoutExtension(dicomPath) + ".meta";
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
