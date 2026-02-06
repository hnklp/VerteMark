using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace VerteMark
{
    /// <summary>
    /// Hlavní třída aplikace WPF. Spravuje inicializaci a životní cyklus aplikace.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Obsluha spuštění aplikace - inicializuje kódování a kontroluje verzi Windows.
        /// </summary>
        /// <param name="e">Argumenty spuštění</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            //kontrola verze win. pouzivame fonty, ktere nejsou (dle dokumentace) dostupne na win starsich nez 8.1. appka by fungovat mela, ale nejspise budou chybet nejake napisy :P
            if (!IsWindows81OrNewer())
            {
                MessageBoxResult result = MessageBox.Show("Tato aplikace je optimalizována pro Windows 8.1 a novější verze operačního systému. Některé funkce nemusí na starších verzích Windows fungovat správně.\n\nChcete pokračovat?", "Upozornění", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                  this.Shutdown();
                }
            }

            if (e.Args.Length > 0 && e.Args[0] == "allDone")
            {
                MessageBox.Show("Projekt uzavřen. Pokračujte otevřením dalšího.");
            }
        }

        /// <summary>
        /// Zjistí, zda je verze Windows 8.1 nebo novější.
        /// </summary>
        /// <returns>True, pokud je Windows 8.1 nebo novější, jinak false</returns>
        private bool IsWindows81OrNewer()
        {
            Version winVersion = Environment.OSVersion.Version;
            return (winVersion >= new Version(6, 3));
        }

        /// <summary>
        /// Restartuje aplikaci a předá parametr "allDone".
        /// </summary>
        public static void RestartApplication()
        {
            var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(currentExecutablePath, "allDone");
            Current.Shutdown();
        }
    }

}
