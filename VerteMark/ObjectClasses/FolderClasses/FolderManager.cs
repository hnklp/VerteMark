using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace VerteMark.ObjectClasses.FolderClasses {
    /// <summary>
    /// Správce struktury složek v běhovém prostředí.
    /// Zajišťuje správnost temp složky a poskytuje nástroje pro výběr složek.
    /// </summary>
    internal class FolderManager {
        /// <summary>Cesta k dočasné složce projektu</summary>
        public string? tempFolderPath;

        /// <summary>
        /// Zkontroluje správnost temp složky v běhovém prostředí.
        /// Pokud nějaká složka chybí, vytvoří ji.
        /// </summary>
        public void CheckTempFolder() {
        // správnost temp složky v běhovém prostředí, pokud nějaká složka chybí, vrátí hodnotu false
            string[] requiredFolders = { "dicoms", "to_validate", "to_anotate", "validated" };

            foreach (string folderName in requiredFolders) {
                string folderPath = Path.Combine(tempFolderPath, folderName);
                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }
            }
        }

        /// <summary>
        /// Smaže dočasnou složku projektu a její obsah.
        /// </summary>
        public void DeleteTempFolder() {
            {
                try {
                    // Získání nadřazené cesty
                    string parentDirectory = Path.GetDirectoryName(tempFolderPath);

                    if (string.IsNullOrEmpty(parentDirectory)) {
                        return;
                    }

                    // Zkontrolování existence cílové složky
                    if (Directory.Exists(tempFolderPath)) {
                        // Smazání složky a jejího obsahu
                        Directory.Delete(tempFolderPath, true);
                    }
                    else {
                        Console.WriteLine($"Složka '{tempFolderPath}' neexistuje.");
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Při mazání složky nastala chyba: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Zpracuje složky a odstraní duplicitní složky mezi různými adresáři.
        /// Logika závisí na názvu tlačítka, které spustilo akci.
        /// </summary>
        /// <param name="button">Název tlačítka, které spustilo akci (určuje logiku mazání)</param>
        public void ProcessFolders(string button)
        {
            string validatedPath = Path.Combine(tempFolderPath, "validated");
            string toValidatePath = Path.Combine(tempFolderPath, "to_validate");
            string toAnnotatePath = Path.Combine(tempFolderPath, "to_anotate");
            string invalidPath = Path.Combine(tempFolderPath, "invalid");  // Přidání invalidPath

            try
            {
                // Získání všech adresářů
                var validatedDirectories = Directory.GetDirectories(validatedPath);
                var toValidateDirectories = Directory.GetDirectories(toValidatePath);
                var invalidDirectories = Directory.GetDirectories(invalidPath);  // Nové zpracování invalid složky

                // Vymazání shodujících se složek mezi validated a to_validate
                foreach (var validatedDir in validatedDirectories)
                {
                    var directoryName = Path.GetFileName(validatedDir);
                    var matchingDirInToValidate = Path.Combine(toValidatePath, directoryName);

                    if (Directory.Exists(matchingDirInToValidate))
                    {
                        Directory.Delete(matchingDirInToValidate, true);
                    }
                }

                // Získání aktualizovaných seznamů složek
                toValidateDirectories = Directory.GetDirectories(toValidatePath);
                var toAnnotateDirectories = Directory.GetDirectories(toAnnotatePath);

                // Vymazání shodujících se složek mezi to_validate a to_anotate
                if (button == "SendForValidationButton")
                {
                    foreach (var toValidateDir in toValidateDirectories)
                    {
                        var directoryName = Path.GetFileName(toValidateDir);
                        var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, directoryName);

                        if (Directory.Exists(matchingDirInToAnnotate))
                        {
                            Directory.Delete(matchingDirInToAnnotate, true);
                        }
                    }
                }

                else
                {
                    foreach (var toAnotateDir in toAnnotateDirectories)
                    {
                        var directoryName = Path.GetFileName(toAnotateDir);
                        var matchingDirInToValidate = Path.Combine(toValidatePath, directoryName);

                        if (Directory.Exists(matchingDirInToValidate))
                        {
                            Directory.Delete(matchingDirInToValidate, true);
                        }
                    }
                }
                
                if (button == "SaveWIPButton")
                {
                    foreach (var toAnnotateDir in toAnnotateDirectories)
                    {
                        var directoryName = Path.GetFileName(toAnnotateDir);
                        var matchingDirInInvalid = Path.Combine(invalidPath, directoryName);

                        if (Directory.Exists(matchingDirInInvalid))
                        {
                            Directory.Delete(matchingDirInInvalid, true);
                        }
                    }
                }
                
                else if (button == "SendForValidationButton")
                {
                    foreach (var toValidateDir in toValidateDirectories)
                    {
                        var directoryName = Path.GetFileName(toValidateDir);
                        var matchingDirInInvalid = Path.Combine(invalidPath, directoryName);
                        if (Directory.Exists(matchingDirInInvalid))
                        {
                            Directory.Delete(matchingDirInInvalid, true);
                        }
                    }
                }

                else if (button == "ValidateButton")
                {
                    foreach (var validatedDir in validatedDirectories)
                    {
                        var directoryName = Path.GetFileName(validatedDir);
                        var matchingDirInInvalid = Path.Combine(invalidPath, directoryName);
                        if (Directory.Exists(matchingDirInInvalid))
                        {
                            Directory.Delete(matchingDirInInvalid, true);
                        }
                    }
                }

                else
                {
                    foreach (var invalidDir in invalidDirectories)
                    {
                        var directoryName = Path.GetFileName(invalidDir);
                        var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, directoryName);

                        if (Directory.Exists(matchingDirInToAnnotate))
                        {
                            Directory.Delete(matchingDirInToAnnotate, true);
                        }
                    }
                }
               
                if (button == "SaveWIPButton")
                {
                    foreach (var toAnnotateDir in toAnnotateDirectories)
                    {
                        var directoryName = Path.GetFileName(toAnnotateDir);
                        var matchingDirInValidated = Path.Combine(validatedPath, directoryName);
                        if (Directory.Exists(matchingDirInValidated))
                        {
                            Directory.Delete(matchingDirInValidated, true);
                        }
                    }
                }

                else
                {
                    foreach (var validatedDir in validatedDirectories)
                    {
                        var directoryName = Path.GetFileName(validatedDir);
                        var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, directoryName);

                        if (Directory.Exists(matchingDirInToAnnotate))
                        {
                            Directory.Delete(matchingDirInToAnnotate, true);
                        }
                    }
                }

                
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Smaže aktuální úpravy projektu ze složky to_anotate.
        /// </summary>
        /// <param name="name">Název projektu ke smazání</param>
        public void DeleteCurrentEdits(string name)
        {
            string toAnnotatePath = Path.Combine(tempFolderPath, "to_anotate");
            var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, name);
            Directory.Delete(matchingDirInToAnnotate, true);

        }

        /// <summary>
        /// Vrátí seznam DICOM souborů, pro které ještě není vytvořen projekt.
        /// </summary>
        /// <returns>Seznam názvů DICOM souborů dostupných pro vytvoření nového projektu</returns>
        public List<string> ChooseNewProject() {
            List<string> dicomFiles = GetDicomFiles();
            List<string> anotatedProjects = GetSubfolders("to_anotate");
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");
            List<string> invalidProjects = GetSubfolders("invalid");

            dicomFiles = dicomFiles.Except(anotatedProjects)
                             .Except(validatedProjects)
                             .Except(toValidateProjects)
                             .Except(invalidProjects)
                             .ToList();

            return dicomFiles;
        }

        /// <summary>
        /// Vrátí seznam rozdaných projektů k anotaci (projekty, které ještě nebyly validovány).
        /// </summary>
        /// <returns>Seznam názvů projektů dostupných pro pokračování v anotaci</returns>
        public List<string> ChooseContinueAnotation() {
            List<string> toAnotateFiles = GetSubfolders("to_anotate");
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");
            List<string> invalidProjects = GetSubfolders("invalid");
            return toAnotateFiles.Except(validatedProjects).Except(toValidateProjects).Except(invalidProjects).ToList();
        }


        /// <summary>
        /// Vrátí seznam projektů pro validátora k validaci.
        /// </summary>
        /// <returns>Seznam názvů projektů dostupných pro validaci</returns>
        public List<string> ChooseValidation() {
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");
            List<string> invalidProjects = GetSubfolders("invalid");
            return toValidateProjects.Except(validatedProjects).Except(invalidProjects).ToList();
        }
        /// <summary>
        /// Vrátí seznam neplatných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů neplatných DICOM souborů</returns>
        public List<string> InvalidDicoms()
        {
            List<string> invalidDicoms = GetSubfolders("invalid");
            
            return invalidDicoms.ToList();
        }

        /// <summary>
        /// Vrátí seznam validovaných DICOM souborů.
        /// </summary>
        /// <returns>Seznam názvů validovaných DICOM souborů</returns>
        public List<string> ValidatedDicoms()
        {
            List<string> validatedDicoms = GetSubfolders("validated");

            return validatedDicoms.ToList();
        }

        /// <summary>
        /// Získá seznam podsložek v zadané nadřazené složce.
        /// </summary>
        /// <param name="parentFolderName">Název nadřazené složky</param>
        /// <returns>Seznam názvů podsložek</returns>
        private List<string> GetSubfolders(string parentFolderName) {
            string parentFolderPath = Path.Combine(tempFolderPath, parentFolderName);
            if (!Directory.Exists(parentFolderPath)){
                Directory.CreateDirectory(parentFolderPath); // Zajistí vytvoření složky, pokud ještě neexistuje
            }

            List<string?> subfolders = Directory.GetDirectories(parentFolderPath)
                                               .Select(Path.GetFileName)
                                               .ToList();

            if (subfolders.Count == 0) { // Pokud je seznam prázdný, vrátí prázdný seznam
                return new List<string>();
            }
            return subfolders;
        }

        /// <summary>
        /// Získá seznam DICOM souborů ze složky dicoms.
        /// </summary>
        /// <returns>Seznam názvů DICOM souborů</returns>
        private List<string> GetDicomFiles() {
            string dicomsFolderPath = Path.Combine(tempFolderPath, "dicoms");
            if (!Directory.Exists(dicomsFolderPath)) {
                Directory.CreateDirectory(dicomsFolderPath); // Zajistí vytvoření složky, pokud ještě neexistuje
            }
            List<string?> dicomFiles = Directory.GetFiles(dicomsFolderPath, "*.*")
                                               .Select(Path.GetFileName)
                                               .ToList();

            if (dicomFiles.Count == 0) { // Pokud je seznam prázdný, vrátí prázdný sezna{
                return new List<string>();
            }
            return dicomFiles;
        }
    }
}
