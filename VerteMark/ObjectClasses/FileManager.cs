using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System;
using System.Drawing;
using System.IO;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO;


namespace VerteMark.ObjectClasses {
    /// <summary>
    /// Správa a manipulace se soubory pro projekt.
    /// 
    /// Zahrnuje:
    /// * Načítání dat z DICOM souborů.
    /// * Konverzi DICOM dat do požadovaných formátů.
    /// * Ukládání projektu a souvisejících souborů.
    /// </summary>
    internal class FileManager {

        public string outputPath;
        public string? dicomPath;


        public FileManager() {
            this.outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); ;
            this.dicomPath = null;
        }

        public void SaveProject() {

        }


        // Kdyz se nacte DICOM, vytvori to slozku, ktera se nastavi jako outputPath
        void CreateOutputFile(string outputDirectoryName)
        {
            if (outputPath != null)
            {
                string fullPath = System.IO.Path.Combine(outputPath, outputDirectoryName);
                if (Directory.Exists(fullPath))
                {
                    fullPath += "_new";
                }
                Directory.CreateDirectory(fullPath);
                this.outputPath = fullPath;
            }
        }


        // zatim nastaveno na to, aby se png ukladalo na desktop
        // problem je v tom, ze to nefunguje - Dicom Image se nechce renderovat
        void ExtractImageFromDicom(string dicomPath, string outputFolderPath)
        {

            DicomFile dicomFile = DicomFile.Open(dicomPath);

            DicomImage image = new DicomImage(dicomFile.Dataset);

            Bitmap bmp = image.RenderImage().As<Bitmap>();

            string outputFileName = System.IO.Path.GetFileNameWithoutExtension(dicomPath) + ".png";
            string outputPath = System.IO.Path.Combine(outputFolderPath, outputFileName);

            // Uložení obrázku jako PNG
            bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        }


        public FolderState CheckFolderType(string path)
        {
            // zjistí typ/stav souboru a vrátí enum, co to je

            return FolderState.Nonfunctional;
        }


        // nacitani DICOM souboru - nutno prejmenovat funkci
        // path = dicom soubor
        public BitmapImage GetPictureAsBitmapImage(string path) {
            try {
                // Check if the file exists
                if (!File.Exists(path)) {
                    throw new FileNotFoundException("File not found.", path);
                }

                this.dicomPath = path;
                CreateOutputFile("test");

                ExtractImageFromDicom(this.dicomPath, this.outputPath);

                path = System.IO.Path.Combine(this.outputPath, "00000000.png");

                // Create a new BitmapImage

                BitmapImage image = LoadBitmapImage(path);
                return image;
            }
            catch (Exception ex)
            {
                // Handle any exceptions, e.g., file not found or invalid image format
                Console.WriteLine("Error loading image: " + ex.Message);
                return null;
            }
        }


        BitmapImage LoadBitmapImage(string pathToImage)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage();

                // Set BitmapImage properties
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.UriSource = new Uri(pathToImage);
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


        // nevim, jestli bude potreba - data o pacientovy nejsou potreba
        public Metadata GetProjectMetada() {
            return null;
        }


        // pujde do funkce JSON maker - ulozeni do output slozky
        public List<Anotace> GetProjectAnotaces() {
            return null;
        }

    }


    public enum FolderState {
        New,
        Existing,
        Nonfunctional
    }
}
