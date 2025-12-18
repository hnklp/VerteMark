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
using System.Xml.Linq;

namespace VerteMark.ObjectClasses.FolderClasses {
    /// <summary>
    /// Správce operací se složkami na vysoké úrovni.
    /// Koordinuje práci mezi ZipManager, FileManager a FolderManager.
    /// </summary>
    internal class FolderUtilityManager {
        ZipManager zipManager;
        /// <summary>Správce souborových operací</summary>
        public FileManager fileManager;
        FolderManager folderManager;
        /// <summary>Cesta k dočasné složce projektu</summary>
        public string tempPath;

        /// <summary>
        /// Vytvoří novou instanci FolderUtilityManager a inicializuje správce.
        /// </summary>
        public FolderUtilityManager() {
            zipManager = new ZipManager();
            fileManager = new FileManager();
            folderManager = new FolderManager();
            tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        }

        /// <summary>
        /// Zruší změny a odstraní extrahované soubory.
        /// </summary>
        /// <param name="button">Název tlačítka, které spustilo akci (pro metadata)</param>
        public void Discard(string button)
        {
            folderManager.ProcessFolders(button);
            Directory.Delete(fileManager.outputPath, true);
            SaveZip();
        }

        /// <summary>
        /// Uloží projekt se všemi anotacemi, obrázky a metadaty.
        /// </summary>
        /// <param name="user">Uživatel provádějící uložení</param>
        /// <param name="newProject">True, pokud se jedná o nový projekt, jinak false</param>
        /// <param name="image">Obrázek k uložení</param>
        /// <param name="jsonString">JSON řetězec s anotacemi</param>
        /// <param name="savingParameter">Parametr určující cílovou složku: 0 = to_anotate, 1 = to_validate, 2 = validated, 3 = invalid</param>
        /// <param name="button">Název tlačítka, které spustilo uložení (pro metadata)</param>
        public void Save(User user, bool newProject, BitmapImage image, string jsonString, int savingParameter, string button) {
            switch (savingParameter) {
                //Ukladani do jednotlivych slozek
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

            fileManager.CreateOutputFile(fileManager.fileName);
            fileManager.TransformPaths();

            if (!newProject) {
                string oldMetaPath = fileManager.metaPath;
                fileManager.metaPath = Path.Combine(fileManager.outputPath, Path.GetFileName(fileManager.metaPath));
                if (oldMetaPath != fileManager.metaPath) {
                    fileManager.CopyMetaFile(oldMetaPath);

                    string oldJsonDirectory = Path.GetDirectoryName(oldMetaPath); // předpokládáme, že meta byla ve stejné složce
                    fileManager.CopyAllJsonFiles(oldJsonDirectory);
                }
                fileManager.AddUserActionToMetadata(user);
            }
            else {
                fileManager.ExtractAndSaveMetadata(user);
            }
                fileManager.SaveJson(jsonString, user);
            fileManager.SaveCroppedImage(image);
            folderManager.ProcessFolders(button); // deletes duplicit folders
            SaveZip();
        }

        /// <summary>
        /// Smaže dočasnou složku projektu.
        /// </summary>
        public void DeleteTempFolder() {
            folderManager.DeleteTempFolder();
        }

        /// <summary>
        /// Extrahuje ZIP soubor projektu do dočasné složky.
        /// </summary>
        /// <param name="path">Cesta k ZIP souboru projektu (.vmk)</param>
        /// <returns>True, pokud byla extrakce úspěšná, jinak false</returns>
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


        /// <summary>
        /// Získá načtený obrázek z projektu.
        /// </summary>
        /// <returns>Bitmapa obrázku nebo null, pokud není načten</returns>
        public BitmapImage GetImage() {
            return fileManager.LoadBitmapImage();
        }

        /// <summary>
        /// Vytvoří nový projekt z DICOM souboru.
        /// </summary>
        /// <param name="path">Cesta k DICOM souboru</param>
        public void CreateNewProject(string path) {
            string folderName = Path.GetFileName(path);
            fileManager.outputPath = Path.Combine(tempPath, "to_anotate");
            fileManager.dicomPath = path;
            fileManager.CreateOutputFile(folderName);
            fileManager.ExtractImageFromDicom();
        }


        /// <summary>
        /// Načte existující projekt ze zadané cesty.
        /// </summary>
        /// <param name="path">Cesta k projektu</param>
        /// <returns>JSON řetězec s anotacemi nebo prázdný řetězec při chybě</returns>
        public string LoadProject(string path) {
            try {
                string[] files = Directory.GetFiles(path);
                string? pngFile = files.FirstOrDefault(f => f.EndsWith(".png"));
                string? metaFile = files.FirstOrDefault(f => f.EndsWith(".meta"));
                string fileName = Path.GetFileNameWithoutExtension(pngFile);

                // Prioritně validátor, pak anotátor
                string? jsonFile = files.FirstOrDefault(f => f.EndsWith(".json") && Path.GetFileName(f).StartsWith("v_"))
                                ?? files.FirstOrDefault(f => f.EndsWith(".json") && Path.GetFileName(f).StartsWith("a_"));

                if (pngFile == null || metaFile == null || jsonFile == null) {
                    return "";
                }
                else {
                    fileManager.metaPath = metaFile;
                    fileManager.pngPath = pngFile;
                    fileManager.jsonPath = jsonFile;
                    fileManager.outputPath = path;
                    fileManager.fileName = fileName;

                    string jsonContent = File.ReadAllText(jsonFile);
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

        /// <summary>
        /// Získá seznam DICOM souborů dostupných pro vytvoření nového projektu.
        /// </summary>
        /// <returns>Seznam názvů DICOM souborů</returns>
        public List<string> ChooseNewProject() {
            return folderManager.ChooseNewProject();
        }

        /// <summary>
        /// Získá seznam projektů dostupných pro pokračování v anotaci.
        /// </summary>
        /// <returns>Seznam názvů projektů</returns>
        public List<string> ChooseContinueAnotation() {
            return folderManager.ChooseContinueAnotation();
        }

        /// <summary>
        /// Získá seznam projektů dostupných pro validaci.
        /// </summary>
        /// <returns>Seznam názvů projektů</returns>
        public List<string> ChooseValidation() {
            return folderManager.ChooseValidation();
        }

        /// <summary>
        /// Získá seznam neplatných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů neplatných DICOM souborů</returns>
        public List<string> InvalidDicoms()
        {
            return folderManager.InvalidDicoms();
        }

        /// <summary>
        /// Získá seznam validovaných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů validovaných DICOM souborů</returns>
        public List<string> ValidatedDicoms()
        {
            return folderManager.ValidatedDicoms();
        }

        /*
        * =============================
        * Kontrola pokracovani v praci
        * =============================
        */

        /// <summary>
        /// Zjistí, zda je k dispozici nějaký projekt pro aktuálního uživatele.
        /// </summary>
        /// <param name="isValidator">True, pokud je uživatel validátor, jinak false</param>
        /// <returns>True, pokud je k dispozici alespoň jeden projekt, jinak false</returns>
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
