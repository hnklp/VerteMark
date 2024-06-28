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
    public class ZipManager{
        public string? zipPath;
        public string? tempFolderPath;
        public string? zipName;

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


        public void UpdateZipFromTempFolder(){
            try{
                using (ZipArchive archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Update)){
                    UpdateZipFromTempFolderRecursive(archive, "", tempFolderPath);
                }
            }
            catch (Exception ex){ Console.WriteLine(); }
        }

        private void UpdateZipFromTempFolderRecursive(ZipArchive archive, string currentPath, string currentFolderPath){
            foreach (var directory in Directory.GetDirectories(currentFolderPath)){
                var directoryName = Path.GetFileName(directory);
                var zipEntry = archive.GetEntry(Path.Combine(currentPath, directoryName));

                if (zipEntry == null){
                    // Složka ve zip neexistuje, vytvoř ji
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, directoryName) + "/");
                }

                // Projdi rekurzivně do podsložky
                if (directoryName != "dicoms"){
                    UpdateZipFromTempFolderRecursive(archive, Path.Combine(currentPath, directoryName), directory);
                }
            }

            foreach (var file in Directory.GetFiles(currentFolderPath)){
                var fileName = Path.GetFileName(file);
                var zipEntry = archive.GetEntry(Path.Combine(currentPath, fileName));

                if (zipEntry == null){
                    // Soubor ve zip neexistuje, přidej ho
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, fileName));
                }
                else{
                    // Soubor ve zip existuje, smaž ho
                    zipEntry.Delete();
                    // Přidej nový soubor z temp
                    zipEntry = archive.CreateEntry(Path.Combine(currentPath, fileName));
                }

                // Přidej obsah souboru
                using (var entryStream = zipEntry.Open())
                using (var fileStream = File.OpenRead(file)){
                    fileStream.CopyTo(entryStream);
                }
            }
        }
    }
}
