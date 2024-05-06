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

namespace VerteMark.ObjectClasses.FolderClasses{
    internal class FolderUtilityManager{
        ZipManager zipManager;
        FileManager fileManager;
        FolderManager folderManager;
        public string tempPath;

        public FolderUtilityManager(){
            zipManager = new ZipManager();
            fileManager = new FileManager();
            folderManager = new FolderManager();
            tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        }

        public void Save(User user, bool newProject, BitmapImage image) {
            fileManager.SaveCroppedImage(image);
            if (!newProject) {
                fileManager.AddUserActionToMetadata(user);
            }
            else {
                fileManager.ExtractAndSaveMetadata(user);
            }
            SaveZip();
        }

        public bool ExtractZip(string path){
            try {
                zipManager.LoadZip(path);
                folderManager.CheckTempFolder();
                return true;
            }
            catch {
                return false; }
        }


        void SaveZip(){
            zipManager.UpdateZipFromTempFolder();
        }


        public BitmapImage GetImage(){
            return fileManager.LoadBitmapImage();
        }


        public void CreateNewProject(string path){
            string folderName = Path.GetFileNameWithoutExtension(path);
            fileManager.outputPath = Path.Combine(tempPath, "to_anotate");
            fileManager.dicomPath = path;
            fileManager.CreateOutputFile(folderName);
            fileManager.ExtractImageFromDicom();
        }


        public string LoadProject(string path){
            try {
                string[] files = Directory.GetFiles(path);
                string? pngFile = files.FirstOrDefault(f => f.EndsWith(".png"));
                string? jsonFile = files.FirstOrDefault(f => f.EndsWith(".json"));
                string? metaFile = files.FirstOrDefault(f => f.EndsWith(".meta"));

                if (pngFile == null || metaFile == null || jsonFile == null) {
                    return "";
                    throw new FileNotFoundException("Chybí png nebo json soubor ve složce.");
                }
                else {
                    fileManager.metaPath = metaFile;
                    fileManager.pngPath = pngFile;
                    fileManager.jsonPath = jsonFile;
                    fileManager.outputPath = path;

                    string jsonContent = File.ReadAllText(jsonFile);
                    Debug.WriteLine(jsonContent);
                    Debug.WriteLine("JSON CONTENT FROM FOL UT MAN -------------");
                    return jsonContent;
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"Chyba při načítání projektu: {ex.Message}");
                return "";
            }
        }


        public void SaveJson(string neco) {
            string? path = fileManager.jsonPath;
            if (path != null) {
                using (StreamWriter sw = new StreamWriter(path)) {
                    sw.Write(neco);
                }
            }
        }

        /*
        * =============================
        * Pouziti v FolderbrowserWindow
        * =============================
        */

        public List<string> ChooseNewProject(){
            return folderManager.ChooseNewProject();
        }


        public List<string> ChooseContinueAnotation(){
            return folderManager.ChooseContinueAnotation();
        }


        public List<string> ChooseValidation(){
            return folderManager.ChooseValidation();
        }
    }
}
