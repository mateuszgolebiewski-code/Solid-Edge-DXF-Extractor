using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SolidEdge_FlatExporter
{
    /// <summary>
    /// Punkt wejścia aplikacji.
    /// Program działa standalone – uruchamia Solid Edge w tle (niewidocznie)
    /// i pozwala użytkownikowi wybrać plik złożenia ASM do przetworzenia.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Uruchom formularz główny – bez wstępnego połączenia z Solid Edge
            // SE będzie uruchamiane w tle dopiero gdy użytkownik wybierze plik ASM
            try
            {
                // Sprawdź czy plik został przeciągnięty na exe/skrót
            string[] args = Environment.GetCommandLineArgs();
            string droppedFile = null;
            if (args.Length > 1 && File.Exists(args[1]))
                droppedFile = args[1];

            Application.Run(new FormMain(droppedFile));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nieoczekiwany błąd:\n{ex.Message}\n\n{ex.StackTrace}",
                    "Sheet Metal Flat Pattern Exporter – Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
