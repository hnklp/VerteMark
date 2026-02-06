using Dicom;
using Dicom.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// Transformuje cesty PNG a JSON souborů do výstupního adresáře.
        /// </summary>
        public void TransformPaths() {
            pngPath = Path.Combine(outputPath, Path.GetFileName(pngPath));
            jsonPath = Path.Combine(outputPath, Path.GetFileName(jsonPath));
        }

        /// <summary>
        /// Zkopíruje metadata soubor ze zdrojové cesty do cílové cesty.
        /// </summary>
        /// <param name="sourcePath">Cesta ke zdrojovému metadata souboru</param>
        /// <exception cref="FileNotFoundException">Vyvoláno, pokud zdrojový soubor neexistuje</exception>
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

        /// <summary>
        /// Zkopíruje všechny JSON soubory ze zdrojového adresáře do výstupního adresáře.
        /// </summary>
        /// <param name="sourcePath">Cesta ke zdrojovému adresáři</param>
        public void CopyAllJsonFiles(string sourcePath) {

            string[] jsonFiles = Directory.GetFiles(sourcePath, "*.json");

            foreach (string file in jsonFiles) {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(outputPath, fileName);

                File.Copy(file, destPath, overwrite: true);
            }
        }

        /// <summary>
        /// Uloží oříznutý obrázek jako PNG soubor.
        /// </summary>
        /// <param name="image">Bitmapa obrázku k uložení</param>
        public void SaveCroppedImage(BitmapImage image) {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (FileStream stream = new FileStream(pngPath, FileMode.Create)) {
                // Uložení bitmapy do souboru pomocí encoderu
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// Uloží JSON řetězec do souboru s prefixem podle role uživatele (a_ pro anotátora, v_ pro validátora).
        /// </summary>
        /// <param name="jsonString">JSON řetězec k uložení</param>
        /// <param name="user">Uživatel určující prefix souboru</param>
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


        /// <summary>
        /// Načte bitmapu obrázku z cesty pngPath.
        /// </summary>
        /// <returns>Načtená bitmapa nebo null, pokud se načítání nezdařilo</returns>
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

        /// <summary>
        /// Přidá záznam o akci uživatele do metadata souboru.
        /// </summary>
        /// <param name="user">Uživatel, jehož akce se má zaznamenat</param>
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

        /// <summary>
        /// Vytvoří výstupní adresář pro projekt.
        /// </summary>
        /// <param name="outputDirectoryName">Název výstupního adresáře</param>
        public void CreateOutputFile(string outputDirectoryName) {
            if (outputPath != null) {
                string fullPath = Path.Combine(outputPath, outputDirectoryName);
                Directory.CreateDirectory(fullPath);
                outputPath = fullPath;
            }
        }


        /// <summary>
        /// Extrahuje PNG obrázek z DICOM souboru a uloží ho do složky.
        /// Nastaví vlastnosti pngPath a jsonPath.
        /// </summary>
        public void ExtractImageFromDicom() {
            DicomFile dicomFile = DicomFile.Open(dicomPath);
            DicomImage image = new DicomImage(dicomFile.Dataset);
            Bitmap bmp = image.RenderImage().As<Bitmap>();

            fileName = Path.GetFileName(dicomPath);
            pngPath = Path.Combine(outputPath, fileName + ".png");
            jsonPath = Path.Combine(outputPath, fileName + ".json");
            bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
        }


        /// <summary>
        /// Extrahuje všechna metadata z DICOM souboru a uloží je do .meta souboru.
        /// Volá se pouze při vytváření nového projektu.
        /// </summary>
        /// <param name="user">Uživatel pro historii metadat</param>
        public void ExtractAndSaveMetadata(User user)
        {
            if (string.IsNullOrEmpty(dicomPath) || !File.Exists(dicomPath))
                return;

            string metaFileName = $"{key}-{Path.GetFileName(dicomPath)}.meta";
            metaPath = Path.Combine(outputPath, metaFileName);

            // Open
            DicomFile dicomFile = DicomFile.Open(dicomPath);
            var ds = dicomFile.Dataset;

            var allMetadata = new Dictionary<string, object>();

            // ------------------------------------------------
            // 1) File Meta Information (0002,xxxx)
            // ------------------------------------------------
            var fileMeta = new Dictionary<string, Dictionary<string, string>>();
            foreach (DicomItem item in dicomFile.FileMetaInfo)
            {
                var m = new Dictionary<string, string>();
                m["Name"] = item.Tag.DictionaryEntry?.Name ?? "Unknown";
                m["Tag"] = item.Tag.ToString();
                m["VR"] = item.ValueRepresentation.Code;
                m["Value"] = ValueToString(dicomFile.FileMetaInfo, item);
                fileMeta[item.Tag.ToString()] = m; // key by tag to avoid collisions
            }
            allMetadata["FileMetaInformation"] = fileMeta;

            // ------------------------------------------------
            // 2) Main Dataset (top-level only, sequences summarized)
            // ------------------------------------------------
            var dicomMetadata = new Dictionary<string, Dictionary<string, string>>();
            foreach (DicomItem item in ds)
            {
                var m = new Dictionary<string, string>();
                m["Name"] = item.Tag.DictionaryEntry?.Name ?? "Unknown";
                m["Tag"] = item.Tag.ToString();
                m["VR"] = item.ValueRepresentation.Code;
                m["Value"] = ValueToString(ds, item);
                dicomMetadata[item.Tag.ToString()] = m;
            }
            allMetadata["DicomMetadata"] = dicomMetadata;

            // ------------------------------------------------
            // 3) History
            // ------------------------------------------------
            DateTime now = DateTime.Now;
            var history = new Dictionary<string, Dictionary<string, string>>();
            history[now.ToString("dd. MM. yyyy HH:mm:ss")] = new Dictionary<string, string>
                {
                    { "id", user.UserID },
                    { "action", user.Validator ? "VALIDATION" : "ANNOTATION" }
                };
            allMetadata["History"] = history;

            // ------------------------------------------------
            // 4) Save
            // ------------------------------------------------
            string json = JsonConvert.SerializeObject(allMetadata, Formatting.Indented);
            File.WriteAllText(metaPath, json);
        }



        private static string GetString(DicomDataset ds, DicomTag tag)
        {
            try { return ds.TryGetString(tag, out var s) ? s : null; } catch { return null; }
        }


        // -------- Pretty value printer used for the full dump --------
        private static string ValueToString(DicomDataset ds, DicomItem item)
        {
            if (item is DicomElement element)
            {
                var vr = item.ValueRepresentation;

                // Binary-ish VRs: show byte length (and small hex preview for tiny payloads)
                if (vr == DicomVR.OB || vr == DicomVR.OW || vr == DicomVR.OF ||
                    vr == DicomVR.OD || vr == DicomVR.OL || vr == DicomVR.UN)
                {
                    long size = element.Buffer?.Size ?? 0;

                    // First, see if it’s actually short text masquerading as UN/OB
                    if (TryDecodeTextFromBytes(ds, item, out var decodedText))
                        return decodedText;

                    // Special-case: (0002,0001) File Meta Information Version (often 2 bytes like "00 01")
                    if (item.Tag == DicomTag.FileMetaInformationVersion && size == 2)
                    {
                        try
                        {
                            var bytes = element.Buffer.Data;
                            var hex = BitConverter.ToString(bytes).Replace("-", " ");
                            return $"{hex} (File Meta Information Version)";
                        }
                        catch { }
                    }

                    // Tiny payloads: show short hex preview
                    if (size > 0 && size <= 32)
                    {
                        try
                        {
                            var bytes = element.Buffer.Data;
                            var hex = BitConverter.ToString(bytes).Replace("-", " ");
                            return $"<binary, {size} byte(s): {hex}>";
                        }
                        catch { }
                    }

                    return $"<binary, {size} byte(s)>";
                }

                // Dates (DA) -> YYYY-MM-DD (multi-valued supported)
                if (vr == DicomVR.DA && ds.TryGetString(item.Tag, out var da) && !string.IsNullOrWhiteSpace(da))
                {
                    string F(string d)
                    {
                        if (d.Length >= 8 && d.Take(8).All(char.IsDigit))
                            return $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)}";
                        return d;
                    }
                    return string.Join("\\", da.Split('\\').Select(F));
                }

                // Times (TM) -> HH:MM:SS(.frac) (multi-valued supported)
                if (vr == DicomVR.TM && ds.TryGetString(item.Tag, out var tm) && !string.IsNullOrWhiteSpace(tm))
                {
                    string F(string t)
                    {
                        string hh = t.Length >= 2 ? t.Substring(0, 2) : "";
                        string mm = t.Length >= 4 ? t.Substring(2, 2) : "";
                        string ss = t.Length >= 6 ? t.Substring(4, 2) : "";
                        string frac = t.Contains('.') ? t.Substring(t.IndexOf('.')) : "";
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

        private static bool TryDecodeTextFromBytes(DicomDataset ds, DicomItem item, out string text)
        {
            text = null;
            if (item is not DicomElement el || el.Buffer == null) return false;
            long size = el.Buffer.Size;
            if (size <= 0 || size > 256) return false; // only attempt on small blobs

            byte[] bytes;
            try { bytes = el.Buffer.Data; } catch { return false; }
            if (bytes == null || bytes.Length == 0) return false;

            // Trim trailing NULs and spaces
            int len = bytes.Length;
            while (len > 0 && (bytes[len - 1] == 0x00 || bytes[len - 1] == 0x20)) len--;
            if (len == 0) return false;

            // Heuristic: are most bytes printable?
            int printable = 0;
            for (int i = 0; i < len; i++)
            {
                byte b = bytes[i];
                if ((b >= 0x20 && b <= 0x7E) || b == 0x09 || b == 0x0A || b == 0x0D) printable++;
            }
            if (printable < len * 0.80) return false;

            // Map DICOM Specific Character Set to .NET Encoding (minimal but covers common sets)
            string cs = GetString(ds, DicomTag.SpecificCharacterSet) ?? "ISO_IR 100"; // your sample uses ISO_IR 100
            Encoding enc = Encoding.GetEncoding("ISO-8859-1"); // default Latin-1
            try
            {
                if (cs.Equals("ISO_IR 192", StringComparison.OrdinalIgnoreCase)) enc = Encoding.UTF8;        // UTF-8
                else if (cs.Equals("ISO_IR 6", StringComparison.OrdinalIgnoreCase)) enc = Encoding.ASCII;       // ASCII
                else if (cs.Equals("ISO_IR 100", StringComparison.OrdinalIgnoreCase)) enc = Encoding.GetEncoding("ISO-8859-1"); // Latin-1
                                                                                                                                // (extend with more mappings if you encounter them)
            }
            catch { /* keep default */ }

            try
            {
                text = enc.GetString(bytes, 0, len);
                // guard against control-character soup
                if (string.IsNullOrWhiteSpace(text)) return false;
                return true;
            }
            catch { return false; }
        }

    }
}
