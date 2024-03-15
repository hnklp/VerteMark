using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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
        
        
        public FileManager() {

        }
        
        public void SaveProject() {

        }

        public FolderState CheckFolderType(string path) {
            // zjistí typ/stav souboru a vrátí enum, co to je

            return FolderState.Nonfunctional;
        }
        //Return DICOM image as bitmapImage so we can use it and crop it
        public BitmapImage GetPictureAsBitmapImage() {
            return null;
        }
        public BitmapImage GetPictureAsBitmapImage(string path) {
            try {
                // Check if the file exists
                if (!File.Exists(path)) {
                    throw new FileNotFoundException("File not found.", path);
                }

                // Create a new BitmapImage
                BitmapImage bitmapImage = new BitmapImage();

                // Set BitmapImage properties
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.UriSource = new Uri(path);
                bitmapImage.EndInit();

                // Ensure the BitmapImage is fully loaded before returning
                bitmapImage.Freeze();

                return bitmapImage;
            } catch (Exception ex) {
                // Handle any exceptions, e.g., file not found or invalid image format
                Console.WriteLine("Error loading image: " + ex.Message);
                return null;
            }
        }
        public Metadata GetProjectMetada() {
            return null;
        }
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
