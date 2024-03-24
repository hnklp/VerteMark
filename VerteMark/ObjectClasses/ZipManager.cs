using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace VerteMark.ObjectClasses
{
    public class ZipManager
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
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, directoryName) + "/");
                }

                // Projdi rekurzivně do podsložky
                UpdateZipFromTempFolderRecursive(archive, Path.Combine(currentPath, directoryName), directory);
            }

            foreach (var file in Directory.GetFiles(currentFolderPath))
            {
                var fileName = Path.GetFileName(file);
                var zipEntry = archive.GetEntry(Path.Combine(currentPath, fileName));

                if (zipEntry == null)
                {
                    // Soubor ve zip neexistuje, přidej ho
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, fileName));
                }
                else
                {
                    // Soubor ve zip existuje, smaž ho
                    zipEntry.Delete();
                    // Přidej nový soubor z temp
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, fileName));
                }

                // Přidej obsah souboru
                using (var entryStream = zipEntry.Open())
                using (var fileStream = File.OpenRead(file))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }
    }
}
