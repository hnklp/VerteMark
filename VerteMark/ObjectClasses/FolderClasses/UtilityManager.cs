using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;

namespace VerteMark.ObjectClasses.FolderClasses
{
    internal class UtilityManager
    {
        ZipManager zipManager;
        FileManager fileManager;
        FolderManager folderManager;
        string tempPath;

        public UtilityManager()
        {
            zipManager = new ZipManager();
            fileManager = new FileManager();
            folderManager = new FolderManager();
            tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        }

        public void ExtractZip(string path)
        {
            zipManager.LoadZip(path);
        }


        public void SaveZip()
        {
            zipManager.UpdateZipFromTempFolder();
        }

        public BitmapImage GetImage()
        {
            return fileManager.LoadBitmapImage();
        }


        public void CreateNewProject(string path)
        {
            string folderName = Path.GetFileNameWithoutExtension(path);
            fileManager.outputPath = Path.Combine(tempPath, "to_anotate");
            fileManager.dicomPath = path;
            fileManager.CreateOutputFile(folderName);
            fileManager.ExtractImageFromDicom();
            fileManager.ExtractAndSaveMetadata();

            // bude vracet png
        }


        public void LoadProject(string path)
        {
            // fileManager.pngPath = path;
            // fileManager.outputPath =
            // fileManager.jsonPath =

            //BitmapImage image = fileManager.LoadBitmap();
            // List<Anotace> anotace = fileManager.GetProjectAnotaces();

            // bude vracet png a json
        }

        
        /**
        public FolderState CheckFolderType(string path)
        {
            // Pokud je soubor PNG, označíme ho jako existující
            if (Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return FolderState.Existing;
            }

            // Pokus o načtení souboru DICOM
            try
            {
                DicomFile dicomFile = DicomFile.Open(path);
                // Zkontrolujte, zda se soubor úspěšně načetl
                if (dicomFile != null && dicomFile.Dataset != null)
                {
                    return FolderState.Existing; // Soubor DICOM existuje a je funkční
                }
            }
            catch (DicomFileException)
            {
                // Soubor není platný DICOM soubor
                return FolderState.Nonfunctional;
            }
            catch (Exception)
            {
                // Nějaká obecná chyba při zpracování souboru
                // Zde můžete přidat další zachycení specifických chyb, pokud to potřebujete
                return FolderState.Nonfunctional;
            }

            // Pokud není soubor DICOM a nedošlo k žádné chybě, označíme jej jako nový
            return FolderState.New;
        }
        **/
    }
}
