using System.Windows.Media.Imaging;
using System;
using System.Drawing;
using System.IO;
using Dicom;
using Dicom.Imaging;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

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

        public void CopyAllJsonFiles(string sourcePath) {

            string[] jsonFiles = Directory.GetFiles(sourcePath, "*.json");

            foreach (string file in jsonFiles) {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(outputPath, fileName);

                File.Copy(file, destPath, overwrite: true);
            }
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
            if (jsonPath != null)
            {
                string directory = Path.GetDirectoryName(jsonPath)!;
                string fileName = Path.GetFileName(jsonPath);

                // Odstraň prefix, pokud existuje
                if (fileName.StartsWith("v_") || fileName.StartsWith("a_"))
                {
                    fileName = fileName.Substring(2);
                }

                // Vytvoř cestu se správným prefixem
                string prefix = user.Validator ? "v_" : "a_";
                string path = Path.Combine(directory, prefix + fileName);

                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(jsonString);
                }

                // Aktualizuj jsonPath podle role
                jsonPath = path;
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
        public void ExtractAndSaveMetadata(User user)
        {
            if (string.IsNullOrEmpty(dicomPath) || !File.Exists(dicomPath))
                return;

            string metaFileName = $"{key}-{Path.GetFileName(dicomPath)}.meta";
            metaPath = Path.Combine(outputPath, metaFileName);

            // Open the DICOM file
            DicomFile dicomFile = DicomFile.Open(dicomPath);

            var allMetadata = new Dictionary<string, object>();

            // ---------------------------
            // 1) File Meta Information
            // ---------------------------
            var fileMeta = new Dictionary<string, Dictionary<string, string>>();
            foreach (DicomItem item in dicomFile.FileMetaInfo)
            {
                var m = new Dictionary<string, string>();
                m["Name"] = item.Tag.DictionaryEntry?.Name ?? "Unknown";
                m["Tag"] = item.Tag.ToString();
                m["VR"] = item.ValueRepresentation.Code;
                m["Value"] = ValueToString(dicomFile.FileMetaInfo, item);

                // key by tag string to avoid collisions
                fileMeta[item.Tag.ToString()] = m;
            }
            allMetadata["FileMetaInformation"] = fileMeta;

            // ---------------------------
            // 2) Main Dataset (top-level)
            // ---------------------------
            var dicomMetadata = new Dictionary<string, Dictionary<string, string>>();
            foreach (DicomItem item in dicomFile.Dataset)
            {
                var m = new Dictionary<string, string>();
                m["Name"] = item.Tag.DictionaryEntry?.Name ?? "Unknown";
                m["Tag"] = item.Tag.ToString();
                m["VR"] = item.ValueRepresentation.Code;
                m["Value"] = ValueToString(dicomFile.Dataset, item); // your helper

                // key by tag string to avoid collisions
                dicomMetadata[item.Tag.ToString()] = m;
            }
            allMetadata["DicomMetadata"] = dicomMetadata;

            // ---------------------------
            // 3) History
            // ---------------------------
            DateTime now = DateTime.Now;
            var history = new Dictionary<string, Dictionary<string, string>>();
            history[now.ToString("dd. MM. yyyy HH:mm:ss")] = new Dictionary<string, string>
            {
                { "id", user.UserID },
                { "action", user.Validator ? "VALIDATION" : "ANNOTATION" }
            };
            allMetadata["History"] = history;

            // ---------------------------
            // 4) Save
            // ---------------------------
            string json = JsonConvert.SerializeObject(allMetadata, Formatting.Indented);
            File.WriteAllText(metaPath, json);
        }


        private static string ValueToString(DicomDataset ds, DicomItem item)
        {
            if (item is DicomElement element)
            {
                var vr = item.ValueRepresentation;

                // Binary-ish VRs -> show byte length instead of junk
                if (vr == DicomVR.OB || vr == DicomVR.OW || vr == DicomVR.OF ||
                    vr == DicomVR.OD || vr == DicomVR.OL || vr == DicomVR.UN)
                {
                    long size = element.Buffer?.Size ?? 0;
                    return $"<binary, {size} byte(s)>";
                }

                // UIDs -> include friendly name if registry knows it
                if (vr == DicomVR.UI && ds.TryGetString(item.Tag, out var uidStr) && !string.IsNullOrWhiteSpace(uidStr))
                {
                    try
                    {
                        var uid = DicomUID.Parse(uidStr);
                        if (uid != null && !string.IsNullOrWhiteSpace(uid.Name) && !string.Equals(uid.Name, "Unknown", StringComparison.OrdinalIgnoreCase))
                            return $"{uidStr} ({uid.Name})";
                    }
                    catch { /* fall through */ }
                    return uidStr;
                }

                // Dates (DA) -> YYYY-MM-DD (handles multi-values)
                if (vr == DicomVR.DA && ds.TryGetString(item.Tag, out var da) && !string.IsNullOrWhiteSpace(da))
                {
                    string F(string d)
                    {
                        // DICOM DA: YYYYMMDD (or shorter). Format when possible.
                        if (d.Length >= 8 && d.AsSpan().Slice(0, 8).ToString().All(char.IsDigit))
                            return $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)}";
                        return d;
                    }
                    return string.Join("\\", da.Split('\\').Select(F));
                }

                // Times (TM) -> HH:MM:SS(.ffffff) (handles multi-values)
                if (vr == DicomVR.TM && ds.TryGetString(item.Tag, out var tm) && !string.IsNullOrWhiteSpace(tm))
                {
                    string F(string t)
                    {
                        // DICOM TM: HHMMSS.frac (components optional)
                        string hh = t.Length >= 2 ? t.Substring(0, 2) : "";
                        string mm = t.Length >= 4 ? t.Substring(2, 2) : "";
                        string ss = t.Length >= 6 ? t.Substring(4, 2) : "";
                        string frac = "";
                        int dot = t.IndexOf('.');
                        if (dot >= 0 && dot < t.Length - 1) frac = t.Substring(dot); // keep .xxxxx
                        var parts = new List<string>();
                        if (!string.IsNullOrEmpty(hh)) parts.Add(hh);
                        if (!string.IsNullOrEmpty(mm)) parts.Add(mm);
                        if (!string.IsNullOrEmpty(ss)) parts.Add(ss);
                        return parts.Count > 0 ? string.Join(":", parts) + frac : t;
                    }
                    return string.Join("\\", tm.Split('\\').Select(F));
                }

                // Default: ask the dataset; if empty, join individual values
                if (ds.TryGetString(item.Tag, out var s) && !string.IsNullOrEmpty(s))
                    return s;

                var partsJoin = new List<string>(element.Count);
                for (int i = 0; i < element.Count; i++)
                {
                    try { partsJoin.Add(element.Get<string>(i)); }
                    catch { partsJoin.Add(""); }
                }
                return string.Join("\\", partsJoin);
            }

            if (item is DicomSequence seq)
                return $"Sequence with {seq.Items.Count} item(s)";

            if (item is DicomFragmentSequence frag)
                return $"Fragmented binary ({frag.Fragments?.Count ?? 0} fragment(s))";

            return "";
        }

    }
}
