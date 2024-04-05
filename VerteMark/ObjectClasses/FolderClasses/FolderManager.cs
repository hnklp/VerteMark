using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace VerteMark.ObjectClasses.FolderClasses
{
    // Prace s temp slozkou v behovem prostredi
    // bude nastrojem pro vyber slozek, ze kterych chce uzivatel vybirat

    internal class FolderManager
    {
        public string tempFolderPath;

        public FolderManager()
        {
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        }


        public bool CheckTempFolder()
        // správnost temp složky v běhovém prostředí, pokud nějaká složka chybí, vrátí hodnotu false
        {
            string[] requiredFolders = { "dicoms", "to_validate", "to_anotate", "validated" };

            foreach (string folderName in requiredFolders)
            {
                string folderPath = Path.Combine(tempFolderPath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    return false;
                }
            }
            return true;
        }

        // vrati list dicomu, pro ktere jeste neni vytvoren projekt
        public List<string> ChooseNewProject()
        {
            Debug.WriteLine("VOLA SE CHOOSENEWPROJECT!!");
            List<string> dicomFiles = GetDicomFiles();
            List<string> anotatedProjects = GetSubfolders("to_anotate");
            List<string> validatedProjects = GetSubfolders("validated");
            List<string> toValidateProjects = GetSubfolders("to_validate");

            dicomFiles = dicomFiles.Except(anotatedProjects)
                             .Except(validatedProjects)
                             .Except(toValidateProjects)
                             .ToList();

            return dicomFiles;
        }

        // vrati list rozdelanych projektu k anotaci
        public List<string> ChooseContinueAnotation()
        {
            return GetSubfolders("to_anotate");
        }


        // vrati list pro validatora k validaci
        public List<string> ChooseValidation()
        {
            return GetSubfolders("to_validate");
        }


        private List<string> GetSubfolders(string parentFolderName)
        {
            string parentFolderPath = Path.Combine(tempFolderPath, parentFolderName);
            Debug.WriteLine(parentFolderPath);
            if (!Directory.Exists(parentFolderPath))
            {
                Directory.CreateDirectory(parentFolderPath); // Zajistí vytvoření složky, pokud ještě neexistuje
            }

            List<string?> subfolders = Directory.GetDirectories(parentFolderPath)
                                               .Select(Path.GetFileName)
                                               .ToList();

            if (subfolders.Count == 0) // Pokud je seznam prázdný, vrátí prázdný seznam
            {
                return new List<string>();
            }

            return subfolders;
        }

        private List<string> GetDicomFiles()
        {
            string dicomsFolderPath = Path.Combine(tempFolderPath, "dicoms");
            if (!Directory.Exists(dicomsFolderPath))
            {
                Directory.CreateDirectory(dicomsFolderPath); // Zajistí vytvoření složky, pokud ještě neexistuje
            }

            List<string?> dicomFiles = Directory.GetFiles(dicomsFolderPath, "*.*")
                                               .Select(Path.GetFileNameWithoutExtension)
                                               .ToList();

            if (dicomFiles.Count == 0) // Pokud je seznam prázdný, vrátí prázdný seznam
            {
                return new List<string>();
            }

            return dicomFiles;
        }
    }
}
