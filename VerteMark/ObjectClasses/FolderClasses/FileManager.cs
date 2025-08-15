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
            // 3) TrainingHeader (typed + derived fields for ML)
            // ------------------------------------------------

            // UIDs + SOP
            string studyUid = GetString(ds, DicomTag.StudyInstanceUID);
            string seriesUid = GetString(ds, DicomTag.SeriesInstanceUID);
            string instanceUid = GetString(ds, DicomTag.SOPInstanceUID);
            string sopClassUid = GetString(ds, DicomTag.SOPClassUID);
            string sopClassName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(sopClassUid))
                {
                    var uid = DicomUID.Parse(sopClassUid);
                    if (uid != null && !string.IsNullOrWhiteSpace(uid.Name) &&
                        !string.Equals(uid.Name, "Unknown", StringComparison.OrdinalIgnoreCase))
                        sopClassName = uid.Name;
                }
            }
            catch { }

            // Geometry
            int? rows = GetInt(ds, DicomTag.Rows);
            int? cols = GetInt(ds, DicomTag.Columns);

            // Prefer ImagerPixelSpacing for DX/CR; fallback to PixelSpacing
            var imagerPS = GetDoubleArray(ds, DicomTag.ImagerPixelSpacing, 2);
            var pixelPS = GetDoubleArray(ds, DicomTag.PixelSpacing, 2);
            double? dy = imagerPS?.Length == 2 ? imagerPS[0] : pixelPS?.Length == 2 ? pixelPS[0] : (double?)null;
            double? dx = imagerPS?.Length == 2 ? imagerPS[1] : pixelPS?.Length == 2 ? pixelPS[1] : (double?)null;

            var ipp = GetDoubleArray(ds, DicomTag.ImagePositionPatient, 3);
            var iop = GetDoubleArray(ds, DicomTag.ImageOrientationPatient, 6);
            double[] direction = iop != null ? BuildDirectionCosines(iop) : null;

            double? dz = GetDouble(ds, DicomTag.SpacingBetweenSlices) ?? GetDouble(ds, DicomTag.SliceThickness);
            int? numberOfFrames = GetInt(ds, DicomTag.NumberOfFrames);

            // Pixel encoding
            int? samplesPerPixel = GetInt(ds, DicomTag.SamplesPerPixel);
            string photometric = GetString(ds, DicomTag.PhotometricInterpretation) ?? "MONOCHROME2";
            int? bitsAllocated = GetInt(ds, DicomTag.BitsAllocated);
            int? bitsStored = GetInt(ds, DicomTag.BitsStored);
            int? highBit = GetInt(ds, DicomTag.HighBit);
            int? pixelRepresentation = GetInt(ds, DicomTag.PixelRepresentation); // 0=unsigned, 1=signed
            bool invertNeeded = string.Equals(photometric, "MONOCHROME1", StringComparison.OrdinalIgnoreCase);

            // Pixel intensity relationship (LIN/DISP/LOG vendor-specific)
            string pir = GetString(ds, DicomTag.PixelIntensityRelationship);
            bool? isLinear = pir != null ? (bool?)(pir.Equals("LIN", StringComparison.OrdinalIgnoreCase)) : null;

            // Intensity scaling
            double? slope = GetDouble(ds, DicomTag.RescaleSlope) ?? 1.0;
            double? intercept = GetDouble(ds, DicomTag.RescaleIntercept) ?? 0.0;
            string rescaleType = GetString(ds, DicomTag.RescaleType);

            // Display
            double? windowCenter = GetDoubleFirst(ds, DicomTag.WindowCenter);
            double? windowWidth = GetDoubleFirst(ds, DicomTag.WindowWidth);
            double? windowMin = (windowCenter.HasValue && windowWidth.HasValue) ? windowCenter - windowWidth / 2.0 : null;
            double? windowMax = (windowCenter.HasValue && windowWidth.HasValue) ? windowCenter + windowWidth / 2.0 : null;

            // Transfer Syntax / compression
            string transferSyntaxUid = GetString(dicomFile.FileMetaInfo, DicomTag.TransferSyntaxUID);
            bool? compressed = null;
            string compressionMethod = null;
            if (!string.IsNullOrWhiteSpace(transferSyntaxUid))
            {
                if (transferSyntaxUid == "1.2.840.10008.1.2" ||
                    transferSyntaxUid == "1.2.840.10008.1.2.1" ||
                    transferSyntaxUid == "1.2.840.10008.1.2.2")
                {
                    compressed = false; compressionMethod = "Uncompressed";
                }
                else if (transferSyntaxUid == "1.2.840.10008.1.2.1.99")
                {
                    compressed = true; compressionMethod = "Deflated Explicit VR Little Endian";
                }
                else if (transferSyntaxUid.StartsWith("1.2.840.10008.1.2.4.90") ||
                         transferSyntaxUid.StartsWith("1.2.840.10008.1.2.4.91"))
                {
                    compressed = true; compressionMethod = "JPEG 2000";
                }
                else if (transferSyntaxUid.StartsWith("1.2.840.10008.1.2.4.80") ||
                         transferSyntaxUid.StartsWith("1.2.840.10008.1.2.4.81"))
                {
                    compressed = true; compressionMethod = "JPEG-LS";
                }
                else if (transferSyntaxUid.StartsWith("1.2.840.10008.1.2.4."))
                {
                    compressed = true; compressionMethod = "JPEG";
                }
                else if (transferSyntaxUid == "1.2.840.10008.1.2.5")
                {
                    compressed = true; compressionMethod = "RLE";
                }
                else
                {
                    compressed = true; compressionMethod = "Compressed";
                }
            }

            // Lossy flags
            bool? lossy = GetBoolFrom00_01(ds, DicomTag.LossyImageCompression);
            double? lossyRatio = GetDouble(ds, DicomTag.LossyImageCompressionRatio);

            // DX/CR extras
            string viewPosition = GetString(ds, DicomTag.ViewPosition);
            string laterality = GetString(ds, DicomTag.Laterality);
            double? dsd = GetDouble(ds, DicomTag.DistanceSourceToDetector);
            double? dsp = GetDouble(ds, DicomTag.DistanceSourceToPatient);
            double? exposureTimeMs = GetDouble(ds, DicomTag.ExposureTime);
            double? tubeCurrentmA = GetDouble(ds, DicomTag.XRayTubeCurrent);
            double? kvp = GetDouble(ds, DicomTag.KVP);
            double? exposureIndex = GetDouble(ds, DicomTag.RelativeXRayExposure);
            string detectorType = GetString(ds, DicomTag.DetectorType);
            string detectorConfig = GetString(ds, DicomTag.DetectorConfiguration);
            double? estimated_mAs = (tubeCurrentmA.HasValue && exposureTimeMs.HasValue)
                                       ? tubeCurrentmA * (exposureTimeMs / 1000.0)
                                       : (double?)null;

            // Theoretical pixel value range
            var theoRange = ComputeTheoreticalRange(bitsStored, pixelRepresentation);

            // Pixel aspect ratio (dx/dy)
            double? pixelAspect = (dx.HasValue && dy.HasValue && dy.Value != 0) ? (dx / dy) : null;

            // Build TrainingHeader
            var trainingHeader = new Dictionary<string, object>
            {
                ["sop"] = new Dictionary<string, object>
                {
                    ["class_uid"] = sopClassUid,
                    ["name"] = sopClassName
                },
                ["uids"] = new Dictionary<string, object>
                {
                    ["study"] = studyUid,
                    ["series"] = seriesUid,
                    ["instance"] = instanceUid
                },
                ["geometry"] = new Dictionary<string, object>
                {
                    ["rows"] = rows,
                    ["cols"] = cols,
                    ["spacing_mm"] = new object[] { dy, dx, dz }, // (row, col, slice)
                    ["origin_mm"] = ipp != null ? new object[] { (object)ipp[0], ipp[1], ipp[2] } : null,
                    ["direction"] = direction,                    // 3x3 row-major or null
                    ["pixel_aspect_ratio"] = pixelAspect
                },
                ["frames"] = new Dictionary<string, object>
                {
                    ["number_of_frames"] = numberOfFrames
                },
                ["pixel_encoding"] = new Dictionary<string, object>
                {
                    ["samples_per_pixel"] = samplesPerPixel,
                    ["photometric_interpretation"] = photometric,
                    ["bits_allocated"] = bitsAllocated,
                    ["bits_stored"] = bitsStored,
                    ["high_bit"] = highBit,
                    ["pixel_representation"] = pixelRepresentation,
                    ["invert_needed"] = invertNeeded,
                    ["theoretical_range"] = theoRange,
                    ["pixel_intensity_relationship"] = pir,
                    ["is_linear"] = isLinear
                },
                ["intensity_scale"] = new Dictionary<string, object>
                {
                    ["slope"] = slope,
                    ["intercept"] = intercept,
                    ["type"] = rescaleType
                },
                ["modality"] = GetString(ds, DicomTag.Modality),
                ["display"] = new Dictionary<string, object>
                {
                    ["window_center"] = windowCenter,
                    ["window_width"] = windowWidth,
                    ["window_min_max"] = (windowMin.HasValue && windowMax.HasValue)
                                           ? new object[] { windowMin, windowMax }
                                           : null
                },
                ["transfer_syntax_uid"] = transferSyntaxUid,
                ["compression"] = new Dictionary<string, object>
                {
                    ["lossy"] = lossy,
                    ["ratio"] = lossyRatio,
                    ["method"] = compressionMethod,
                    ["compressed"] = compressed
                },
                ["dx"] = new Dictionary<string, object>
                {
                    ["view_position"] = viewPosition,
                    ["laterality"] = laterality,
                    ["distance_source_detector"] = dsd,
                    ["distance_source_patient"] = dsp,
                    ["exposure_time_ms"] = exposureTimeMs,
                    ["xray_tube_current_mA"] = tubeCurrentmA,
                    ["estimated_mAs"] = estimated_mAs,
                    ["kvp"] = kvp,
                    ["exposure_index"] = exposureIndex,
                    ["detector_type"] = detectorType,
                    ["detector_configuration"] = detectorConfig
                }
            };

            allMetadata["TrainingHeader"] = trainingHeader;

            // ------------------------------------------------
            // 4) History
            // ------------------------------------------------
            DateTime now = DateTime.Now;
            var history = new Dictionary<string, Dictionary<string, string>>();
            history[now.ToString("dd. MM. yyyy HH:mm:ss")] = new Dictionary<string, string>
    {
        { "id", user.UserID },
        { "action", user.Validator ? "VALIDATION" : "ANNOTATION" }
    };
            allMetadata["History"] = history;

            // Marker (handy for sanity checks)
            allMetadata["__generator"] = "vertemark-meta-3";

            // ------------------------------------------------
            // 5) Save
            // ------------------------------------------------
            string json = JsonConvert.SerializeObject(allMetadata, Formatting.Indented);
            File.WriteAllText(metaPath, json);
        }



        private static string GetString(DicomDataset ds, DicomTag tag)
        {
            try { return ds.TryGetString(tag, out var s) ? s : null; } catch { return null; }
        }

        private static int? GetInt(DicomDataset ds, DicomTag tag)
        {
            try { if (ds.TryGetSingleValue(tag, out int iv)) return iv; } catch { }
            try { if (ds.TryGetSingleValue(tag, out ushort us)) return (int)us; } catch { }
            try { if (ds.TryGetSingleValue(tag, out short s)) return (int)s; } catch { }
            return null;
        }

        private static double? GetDouble(DicomDataset ds, DicomTag tag)
        {
            try { if (ds.TryGetSingleValue(tag, out double dv)) return dv; } catch { }
            try { if (ds.TryGetSingleValue(tag, out float fv)) return (double)fv; } catch { }
            try
            {
                if (ds.TryGetString(tag, out var s) && !string.IsNullOrWhiteSpace(s))
                {
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                        return d;
                }
            }
            catch { }
            return null;
        }

        private static double? GetDoubleFirst(DicomDataset ds, DicomTag tag)
        {
            try { if (ds.TryGetValues(tag, out double[] arr) && arr?.Length > 0) return arr[0]; } catch { }
            try { if (ds.TryGetValues(tag, out float[] arrF) && arrF?.Length > 0) return (double)arrF[0]; } catch { }
            try
            {
                if (ds.TryGetString(tag, out var s) && !string.IsNullOrWhiteSpace(s))
                {
                    var first = s.Split('\\')[0];
                    if (double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                        return d;
                }
            }
            catch { }
            return null;
        }

        private static double[] GetDoubleArray(DicomDataset ds, DicomTag tag, int expectedCount = -1)
        {
            try
            {
                if (ds.TryGetValues(tag, out double[] arr) && arr != null && (expectedCount < 0 || arr.Length == expectedCount))
                    return arr;
            }
            catch { }
            try
            {
                if (ds.TryGetString(tag, out var s) && !string.IsNullOrWhiteSpace(s))
                {
                    var parts = s.Split('\\');
                    var list = new List<double>(parts.Length);
                    foreach (var p in parts)
                        if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) list.Add(d);
                    if (list.Count > 0 && (expectedCount < 0 || list.Count == expectedCount))
                        return list.ToArray();
                }
            }
            catch { }
            return null;
        }

        private static double[] BuildDirectionCosines(double[] iop)
        {
            // iop = [rx, ry, rz, cx, cy, cz]
            double rx = iop[0], ry = iop[1], rz = iop[2];
            double cx = iop[3], cy = iop[4], cz = iop[5];

            // slice (k) = r x c
            double sx = ry * cz - rz * cy;
            double sy = rz * cx - rx * cz;
            double sz = rx * cy - ry * cx;

            return new[] { rx, ry, rz, cx, cy, cz, sx, sy, sz };
        }

        private static object ComputeTheoreticalRange(int? bitsStored, int? pixelRepresentation)
        {
            if (bitsStored is null || pixelRepresentation is null) return null;
            int b = Math.Max(1, bitsStored.Value);
            if (pixelRepresentation == 0) // unsigned
            {
                double max = Math.Pow(2, b) - 1;
                return new object[] { 0.0, max };
            }
            else // signed
            {
                double min = -Math.Pow(2, b - 1);
                double max = Math.Pow(2, b - 1) - 1;
                return new object[] { min, max };
            }
        }

        private static bool? GetBoolFrom00_01(DicomDataset ds, DicomTag tag)
        {
            try
            {
                if (!ds.TryGetString(tag, out var s) || string.IsNullOrWhiteSpace(s)) return null;
                s = s.Trim();
                if (s == "01" || s == "1") return true;
                if (s == "00" || s == "0") return false;
            }
            catch { }
            return null;
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
