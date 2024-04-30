using System.Windows.Media.Imaging;
using System;
using System.Drawing;
using System.IO;
using Dicom;
using Dicom.Imaging;
using System.Diagnostics;
using Newtonsoft.Json;


namespace VerteMark.ObjectClasses.FolderClasses
{
    /// <summary>
    /// Správa a manipulace se soubory pro projekt.
    /// 
    /// Zahrnuje:
    /// * Načítání dat z DICOM souborů.
    /// * Konverzi DICOM dat do požadovaných formátů.
    /// * Ukládání projektu a souvisejících souborů.
    /// </summary>
    /// 
    // sračka
    internal class FileManager
    {

        public string outputPath;
        public string? dicomPath;
        public string? pngPath;
        public string? jsonPath;
        public string? metaPath;
        private string key;


        public FileManager()
        {
            outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            key = "XX";
        }


        public void SaveProject()
        {
            // ulozeni vseho do output slozky
        }

        // Kdyz se nacte DICOM, vytvori to slozku, ktera se nastavi jako outputPath
        public void CreateOutputFile(string outputDirectoryName)
        {
            if (outputPath != null)
            {
                string fullPath = Path.Combine(outputPath, outputDirectoryName);
                Directory.CreateDirectory(fullPath);
                outputPath = fullPath;
            }
        }


        // extrahuje png obrazek z dicom souboru a ulozi ho do slozky
        // nastavi instanci pngPath
        public void ExtractImageFromDicom()
        {

            DicomFile dicomFile = DicomFile.Open(dicomPath);

            DicomImage image = new DicomImage(dicomFile.Dataset);

            Bitmap bmp = image.RenderImage().As<Bitmap>();

            string fileName = Path.GetFileNameWithoutExtension(dicomPath);

            string outputPngName = fileName + ".png";
            pngPath = Path.Combine(outputPath, outputPngName);

            string outputJsonName = fileName + ".json";
            jsonPath = Path.Combine(outputPath, outputJsonName);

            // Uložení obrázku jako PNG
            bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
        }


        // extrahuje metadata do csv souboru do output slozky
        // nastavi instanci metaPath
        public async Task ExtractAndSaveMetadata()
        {
            await Task.Run(() =>
            {
                if (!File.Exists(dicomPath))
                {
                    Debug.WriteLine(dicomPath);
                    Debug.WriteLine("Zadaný DICOM soubor neexistuje.");
                    return;
                }

                string csvFileName = key + "-" + Path.GetFileNameWithoutExtension(dicomPath) + ".meta";
                metaPath = Path.Combine(outputPath, csvFileName);

                DicomFile dicomFile = DicomFile.Open(dicomPath);

                var allMetadata = new Dictionary<string, object>();

                // Uložení metadat DICOM
                var dicomMetadata = new Dictionary<string, Dictionary<string, string>>();

                foreach (DicomItem item in dicomFile.Dataset)
                {
                    var metadataItem = new Dictionary<string, string>();

                    metadataItem["Tag"] = item.Tag.ToString();
                    metadataItem["Value"] = item.ToString();
                    metadataItem["VR"] = item.ValueRepresentation.Code;

                    string description = DicomDictionary.Default[item.Tag].Name;
                    dicomMetadata[description] = metadataItem;
                }

                allMetadata["DicomMetadata"] = dicomMetadata;

                // Vytvoření slovníku pro ukládání historie
                var history = new Dictionary<string, Dictionary<string, string>>();

                // Přidání záznamů do historie
                history["1.1.2020"] = new Dictionary<string, string> { { "id", "ANOTATOR1" }, { "action", "ANNOTATION" } };
                history["5.1.2020"] = new Dictionary<string, string> { { "id", "ANOTATOR1" }, { "action", "ANNOTATION" } };
                history["10.1.2020"] = new Dictionary<string, string> { { "id", "VALIDATOR1" }, { "action", "VALIDATION" } };

                allMetadata["History"] = history;

                // Konverze do formátu JSON
                string jsonAllMetadata = JsonConvert.SerializeObject(allMetadata, Formatting.Indented);

                // Uložení do souboru
                File.WriteAllText(metaPath, jsonAllMetadata);
            });
            Debug.WriteLine("METADATA ULOZENA");
        }

        // nacte obrazek pomoci cesty pngPath
        public BitmapImage LoadBitmapImage()
        {
            try
            {
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

            catch (Exception ex)
            {
                // Handle any exceptions, e.g., file not found or invalid image format
                Console.WriteLine("Error loading image: " + ex.Message);
                return null;
            }
        }


        // pujde do funkce JSON maker - ulozeni do output slozky
        public List<Anotace> GetProjectAnotaces()
        {
            return null;
        }

        //Prozatimní funkce, používám ji při testování těch anotací
        public BitmapImage PEPEGetPictureAsBitmap(string path)
        {
            // Check if the file exists
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found", path);
            }

            // Load the image using System.Drawing
            using (var bitmap = new Bitmap(path))
            {
                // Convert System.Drawing.Bitmap to System.Windows.Media.Imaging.BitmapImage
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                MemoryStream memoryStream = new MemoryStream();
                // Save to a memory stream
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Seek(0, SeekOrigin.Begin);
                // Load the bitmap from memory stream
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
