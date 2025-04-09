using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace VerteMark.ObjectClasses.FolderClasses {
    // Prace s temp slozkou v behovem prostredi
    // bude nastrojem pro vyber slozek, ze kterych chce uzivatel vybirat

    internal class FolderManager {
        public string? tempFolderPath;

        
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

        public void ProcessFolders()
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
                foreach (var toValidateDir in toValidateDirectories)
                {
                    var directoryName = Path.GetFileName(toValidateDir);
                    var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, directoryName);

                    if (Directory.Exists(matchingDirInToAnnotate))
                    {
                        Directory.Delete(matchingDirInToAnnotate, true);
                    }
                }

                // Nové zpracování složek invalid - odstranění shodujících se složek z to_anotate
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
            catch (Exception ex)
            {
            }
        }

        public void DeleteCurrentEdits(string name)
        {
            string toAnnotatePath = Path.Combine(tempFolderPath, "to_anotate");
            var matchingDirInToAnnotate = Path.Combine(toAnnotatePath, name);
            Directory.Delete(matchingDirInToAnnotate, true);

        }

        // vrati list dicomu, pro ktere jeste neni vytvoren projekt
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

        // vrati list rozdelanych projektu k anotaci
        public List<string> ChooseContinueAnotation() {
            List<string> toAnotateFiles = GetSubfolders("to_anotate");
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");
            List<string> invalidProjects = GetSubfolders("invalid");
            return toAnotateFiles.Except(validatedProjects).Except(toValidateProjects).Except(invalidProjects).ToList();
        }


        // vrati list pro validatora k validaci
        public List<string> ChooseValidation() {
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");
            List<string> invalidProjects = GetSubfolders("invalid");
            return toValidateProjects.Except(validatedProjects).Except(invalidProjects).ToList();
        }
        //vrati list nevalidnich snimku
        public List<string> InvalidDicoms()
        {
            List<string> invalidDicoms = GetSubfolders("invalid");
            
            return invalidDicoms.ToList();
        }

        public List<string> ValidatedDicoms()
        {
            List<string> validatedDicoms = GetSubfolders("validated");

            return validatedDicoms.ToList();
        }


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
