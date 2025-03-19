using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace VerteMark.ObjectClasses.FolderClasses {
    internal class FolderUtilityManager {
        ZipManager zipManager;
        FileManager fileManager;
        FolderManager folderManager;
        public string tempPath;

        public FolderUtilityManager() {
            zipManager = new ZipManager();
            fileManager = new FileManager();
            folderManager = new FolderManager();
            tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        }


        // saving parameters : 0: to_anotate, 1: to_validate, 2: validated, 3: invalid
        public void Save(User user, bool newProject, BitmapImage image, string jsonString, int savingParameter) {
            switch (savingParameter) {
                //Ulokladani do jednotlivych slozek
                case 0:
                    fileManager.outputPath = Path.Combine(tempPath, "to_anotate");
                    break;
                case 1:
                    fileManager.outputPath = Path.Combine(tempPath, "to_validate");
                    break;
                case 2:
                    fileManager.outputPath = Path.Combine(tempPath, "validated");
                    break;
                case 3:
                    fileManager.outputPath = Path.Combine(tempPath, "invalid");
                    break;
            }

            string oldFolder = fileManager.outputPath;
            fileManager.CreateOutputFile(fileManager.fileName);
            fileManager.TransformPaths();

            if (!newProject) {
                string oldMetaPath = fileManager.metaPath;
                fileManager.metaPath = Path.Combine(fileManager.outputPath, Path.GetFileName(fileManager.metaPath));
                if (oldMetaPath != fileManager.metaPath) {
                    fileManager.CopyMetaFile(oldMetaPath);
                }
                fileManager.AddUserActionToMetadata(user);
            }
            else {
                fileManager.ExtractAndSaveMetadata(user);
            }
            fileManager.SaveJson(jsonString);
            fileManager.SaveCroppedImage(image);
            folderManager.ProcessFolders(); // deletes duplicit folders
            SaveZip();
        }

        public void DeleteTempFolder() {
            folderManager.DeleteTempFolder();
        }


        public bool ExtractZip(string path) {
            try {
                zipManager.LoadZip(path);
                tempPath = zipManager.tempFolderPath;
                folderManager.tempFolderPath = zipManager.tempFolderPath;
                folderManager.CheckTempFolder();
                return true;
            }
            catch {
                return false; }
        }


        void SaveZip() {
            zipManager.UpdateZipFromTempFolder();
        }


        public BitmapImage GetImage() {
            return fileManager.LoadBitmapImage();
        }


        public void CreateNewProject(string path) {
            string folderName = Path.GetFileNameWithoutExtension(path);
            fileManager.outputPath = Path.Combine(tempPath, "to_anotate");
            fileManager.dicomPath = path;
            fileManager.CreateOutputFile(folderName);
            fileManager.ExtractImageFromDicom();
        }


        public string LoadProject(string path) {
            try {
                string[] files = Directory.GetFiles(path);
                string? pngFile = files.FirstOrDefault(f => f.EndsWith(".png"));
                string? jsonFile = files.FirstOrDefault(f => f.EndsWith(".json"));
                string? metaFile = files.FirstOrDefault(f => f.EndsWith(".meta"));
                string fileName = Path.GetFileNameWithoutExtension(pngFile);

                if (pngFile == null || metaFile == null || jsonFile == null) {
                    return "";
                    throw new FileNotFoundException("Chybí png nebo json soubor ve složce.");
                }
                else {
                    fileManager.metaPath = metaFile;
                    fileManager.pngPath = pngFile;
                    fileManager.jsonPath = jsonFile;
                    fileManager.outputPath = path;
                    fileManager.fileName = fileName;

                    string jsonContent = File.ReadAllText(jsonFile);
                    Debug.WriteLine(fileManager.metaPath);
                    Debug.WriteLine(fileManager.pngPath);
                    Debug.WriteLine(fileManager.outputPath);
                    Debug.WriteLine(fileManager.jsonPath);
                    return jsonContent;
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Chyba při načítání projektu: {ex.Message}");
                return "";
            }
        }


        /*
        * =============================
        * Pouziti v FolderbrowserWindow
        * =============================
        */

        public List<string> ChooseNewProject() {
            return folderManager.ChooseNewProject();
        }


        public List<string> ChooseContinueAnotation() {
            return folderManager.ChooseContinueAnotation();
        }


        public List<string> ChooseValidation() {
            return folderManager.ChooseValidation();
        }

        public List<string> InvalidDicoms()
        {
            return folderManager.InvalidDicoms();
        }

        public List<string> ValidatedDicoms()
        {
            return folderManager.ValidatedDicoms();
        }

        /*
        * =============================
        * Kontrola pokracovani v praci
        * =============================
        */

        public bool anyProjectAvailable(bool isValidator) {
            if (isValidator && folderManager.ChooseValidation().Count == 0) {
                return false;
            }
            else if (!isValidator && (folderManager.ChooseContinueAnotation().Count == 0 && folderManager.ChooseNewProject().Count == 0)) {
                return false;
            }
            return true;
        }
    }
}
