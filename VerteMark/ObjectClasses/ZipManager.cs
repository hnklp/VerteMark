using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace VerteMark.ObjectClasses
{
    internal class ZipManager
    {
        public string? zipPath;
        public string? tempFolderPath;


        public void LoadZip(string zipPath)
        {
            try
            {
                // Určení cesty pro extrakci obsahu ZIP souboru
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                this.zipPath = zipPath;
                this.tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                Directory.CreateDirectory(tempFolderPath);

                // Extrahování obsahu ZIP souboru do cílové složky
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Vytvoření cesty pro každý vnitřní element ZIP souboru
                        string entryExtractPath = Path.Combine(tempFolderPath, entry.FullName);

                        // Pokud se jedná o složku, vytvoříme ji
                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(entryExtractPath);
                        }
                        // Pokud se jedná o soubor, extrahujeme ho
                        else
                        {
                            entry.ExtractToFile(entryExtractPath, true);
                        }
                    }
                }

                Console.WriteLine("Obsah ZIP souboru byl úspěšně extrahován do složky Temp na ploše.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nastala chyba při extrahování obsahu ZIP souboru: {ex.Message}");
            }
        }



        public void UpdateZipFromTempFolder()
        {
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                UpdateZipFromTempFolderRecursive(archive, "", tempFolderPath);
            }
        }

        private void UpdateZipFromTempFolderRecursive(ZipArchive archive, string currentPath, string currentFolderPath)
        {
            foreach (var directory in Directory.GetDirectories(currentFolderPath))
            {
                var directoryName = Path.GetFileName(directory);
                var zipEntry = archive.GetEntry(Path.Combine(currentPath, directoryName));

                if (zipEntry == null)
                {
                    // Složka ve zip neexistuje, vytvoř ji
                    archive.CreateEntry(Path.Combine(currentPath, directoryName));
                }

                // Projdi rekurzivně do podsložky
                UpdateZipFromTempFolderRecursive(archive, Path.Combine(currentPath, directoryName), directory);
            }

            foreach (var file in Directory.GetFiles(currentFolderPath))
            {
                var fileName = Path.GetFileName(file);
                var zipEntry = archive.GetEntry(Path.Combine(currentPath, fileName));
                var tempFilePath = Path.Combine(currentFolderPath, fileName);
                var zipFilePath = Path.Combine(currentPath, fileName);

                if (zipEntry == null)
                {
                    // Soubor ve zip neexistuje, přidej ho
                    if (Path.GetExtension(fileName) == "")
                    {
                        // Skip files without extensions
                        continue;
                    }
                    archive.CreateEntryFromFile(tempFilePath, zipFilePath);
                }
                else
                {
                    if (zipEntry.FullName.EndsWith("/"))
                    {
                        // Složka ve zip existuje, nikdyse nedostaneme sem
                        Console.WriteLine("Error: Zip file contains a directory with the same name as a file in the temp folder.");
                        continue;
                    }

                    if (File.GetLastWriteTimeUtc(file) == zipEntry.LastWriteTime.UtcDateTime)
                    {
                        // Pokud máme stejný soubor, nic neuděláme
                        continue;
                    }

                    // Odstraň stávající soubor z zip
                    zipEntry.Delete();
                    // Přidej aktualizovaný soubor z temp
                    if (Path.GetExtension(fileName) == "")
                    {
                        // Skip files without extensions
                        continue;
                    }
                    archive.CreateEntryFromFile(tempFilePath, zipFilePath);
                }
            }

            // Odstraň složky ve zip, které nejsou ve složce temp
    /*        ZipArchiveEntry[] entriesToRemove = archive.Entries.Where(entry => entry.FullName.StartsWith(currentPath) && entry.FullName.EndsWith("/")).ToArray();
            foreach (ZipArchiveEntry entry in entriesToRemove)
            {
                if (currentPath != currentFolderPath)
                {
                    string? entryPath = entry.FullName.Replace(currentPath, currentFolderPath);

                    if (entry.FullName.EndsWith("/") && Directory.Exists(entryPath))
                    {
                        if (!string.IsNullOrEmpty(entryPath) && !Directory.Exists(entryPath))
                        {
                            entry.Delete();
                        }
                    }
                } 
            }*/
        }
    }
}
