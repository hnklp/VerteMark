using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;

namespace VerteMark.ObjectClasses.FolderClasses{
    /// <summary>
    /// Správce operací s ZIP kontejnerem projektu (.vmk soubory).
    /// Zajišťuje extrakci a aktualizaci ZIP archivu.
    /// </summary>
    public class ZipManager{
        /// <summary>Cesta k ZIP souboru projektu</summary>
        public string? zipPath;
        /// <summary>Cesta k dočasné složce pro extrakci</summary>
        public string? tempFolderPath;
        /// <summary>Název ZIP souboru bez přípony</summary>
        public string? zipName;

        /// <summary>
        /// Načte a extrahuje ZIP soubor projektu do dočasné složky.
        /// </summary>
        /// <param name="zipPath">Cesta k ZIP souboru projektu</param>
        public void LoadZip(string zipPath){
                this.zipPath = zipPath;
                this.zipName = Path.GetFileNameWithoutExtension(zipPath);
                tempFolderPath = Path.GetTempPath() + "/VerteMark/" + zipName ;

			try {
				Console.WriteLine($"Extrahování souboru: {zipName}");

				// Kontrola a vytvoření cílové složky
				if (Directory.Exists(tempFolderPath)) {
					Directory.Delete(tempFolderPath, true);
				}
				Directory.CreateDirectory(tempFolderPath);

				// Použití SharpZipLib pro otevření a extrakci ZIP souboru
				using (var zipStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
				using (var zipInputStream = new ZipInputStream(zipStream)) {
					ZipEntry entry;
					while ((entry = zipInputStream.GetNextEntry()) != null) {
						string entryExtractPath = Path.Combine(tempFolderPath, entry.Name);

						// Pokud je položka složka, vytvoříme ji
						if (entry.IsDirectory) {
							Directory.CreateDirectory(entryExtractPath);
						}
						else {
							// Vytvoření potřebných složek, pokud nejsou
							string directoryPath = Path.GetDirectoryName(entryExtractPath);
							if (!Directory.Exists(directoryPath)) {
								Directory.CreateDirectory(directoryPath);
							}

							// Extrakce souboru
							using (var entryStream = File.Create(entryExtractPath)) {
								zipInputStream.CopyTo(entryStream);
							}
						}
					}
				}

				Console.WriteLine("Extrakce dokončena.");
			}
			catch (Exception ex) {
				Console.WriteLine("Chyba při extrakci ZIP souboru: " + ex.Message);
			}

		}


        /// <summary>
        /// Aktualizuje ZIP archiv ze změn v dočasné složce.
        /// Odstraní soubory, které už nejsou na disku, a přidá/aktualizuje změněné soubory.
        /// </summary>
        public void UpdateZipFromTempFolder(){
            try{
                using (ZipArchive archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Update)){
                    UpdateZipFromTempFolderRecursive(archive, "", tempFolderPath);
                }
            }
            catch (Exception ex){}
        }

        /// <summary>
        /// Rekurzivně aktualizuje ZIP archiv ze změn v dočasné složce.
        /// </summary>
        /// <param name="archive">ZIP archiv k aktualizaci</param>
        /// <param name="currentPath">Aktuální cesta v ZIP archivu</param>
        /// <param name="currentFolderPath">Aktuální cesta na disku</param>
        private void UpdateZipFromTempFolderRecursive(ZipArchive archive, string currentPath, string currentFolderPath)
        {
            // Získání aktuální cesty jako prefix pro ZIP entry
            string pathPrefix = string.IsNullOrEmpty(currentPath) ? "" : currentPath.Replace("\\", "/").TrimEnd('/') + "/";

            // Všechny soubory a složky ve zdrojové složce
            var diskDirectories = new HashSet<string>(Directory.GetDirectories(currentFolderPath)
                .Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);

            var diskFiles = new HashSet<string>(Directory.GetFiles(currentFolderPath)
                .Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);

            // Smaž ze ZIPu složky/soubory, které už na disku nejsou
            var entriesToDelete = archive.Entries
                .Where(e => e.FullName.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(e =>
                {
                    var relativePath = e.FullName.Substring(pathPrefix.Length).TrimEnd('/');
                    if (string.IsNullOrEmpty(relativePath)) return false;
                    if (relativePath.Contains("/"))
                    {
                        var topDir = relativePath.Substring(0, relativePath.IndexOf("/"));
                        return !diskDirectories.Contains(topDir);
                    }
                    else
                    {
                        return !diskFiles.Contains(relativePath);
                    }
                }).ToList();

            foreach (var entry in entriesToDelete)
            {
                entry.Delete();
            }

            // Rekurzivně zpracuj složky
            foreach (var directory in Directory.GetDirectories(currentFolderPath))
            {
                var directoryName = Path.GetFileName(directory);
                var zipEntryPath = Path.Combine(currentPath, directoryName).Replace("\\", "/") + "/";

                // Pokud složka neexistuje ve ZIPu, vytvoř ji (prázdný entry pro složku)
                if (!archive.Entries.Any(e => e.FullName.Equals(zipEntryPath, StringComparison.OrdinalIgnoreCase)))
                {
                    archive.CreateEntry(zipEntryPath);
                }

                if (directoryName != "dicoms")
                {
                    UpdateZipFromTempFolderRecursive(archive, Path.Combine(currentPath, directoryName), directory);
                }
            }

            // Zpracuj soubory
            foreach (var file in Directory.GetFiles(currentFolderPath))
            {
                var fileName = Path.GetFileName(file);
                var zipEntryPath = Path.Combine(currentPath, fileName).Replace("\\", "/");

                var existingEntry = archive.GetEntry(zipEntryPath);
                if (existingEntry != null)
                {
                    existingEntry.Delete();
                }

                var newEntry = archive.CreateEntry(zipEntryPath);

                using (var entryStream = newEntry.Open())
                using (var fileStream = File.OpenRead(file))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }
    }
}
