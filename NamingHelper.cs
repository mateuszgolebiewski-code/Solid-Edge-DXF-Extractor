using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SolidEdge_FlatExporter
{
    /// <summary>
    /// Budowanie nazw plików wynikowych wg schematu:
    /// [GrubośćMm_][Materiał_]NazwaCzęści.format
    /// </summary>
    public static class NamingHelper
    {
        /// <summary>
        /// Buduje nazwę pliku wynikowego na podstawie opcji użytkownika.
        /// </summary>
        /// <param name="partInfo">Informacje o części blaszanej</param>
        /// <param name="includeThickness">Czy dodać grubość do nazwy</param>
        /// <param name="includeMaterial">Czy dodać materiał do nazwy</param>
        /// <param name="format">Format pliku: "dxf" lub "dwg"</param>
        /// <returns>Nazwa pliku (bez ścieżki folderu)</returns>
        public static string BuildFileName(SheetMetalPartInfo partInfo, bool includeThickness, bool includeMaterial, string customPrefix, string format)
        {
            string name = "";

            // Dodaj prefix użytkownika
            if (!string.IsNullOrWhiteSpace(customPrefix))
            {
                name += customPrefix;
            }

            // Dodaj grubość (np. "2.0mm_")
            if (includeThickness)
            {
                string thicknessStr = partInfo.Thickness.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                name += thicknessStr + "mm_";
            }

            // Dodaj materiał (np. "DC01_")
            if (includeMaterial)
            {
                string materialClean = SanitizeMaterial(partInfo.Material);
                name += materialClean + "_";
            }

            // Dodaj nazwę części
            name += partInfo.PartName;

            // Dodaj rozszerzenie
            name += "." + format.ToLowerInvariant();

            // Oczyść z niedozwolonych znaków
            name = SanitizeFileName(name);

            return name;
        }

        /// <summary>
        /// Generuje podgląd przykładowej nazwy pliku.
        /// </summary>
        public static string GetPreview(string examplePartName, double exampleThickness, string exampleMaterial,
                                         bool includeThickness, bool includeMaterial, string customPrefix, string format)
        {
            var fakeInfo = new SheetMetalPartInfo
            {
                FileName = examplePartName + ".psm",
                Thickness = exampleThickness,
                Material = exampleMaterial
            };
            return BuildFileName(fakeInfo, includeThickness, includeMaterial, customPrefix, format);
        }

        /// <summary>
        /// Czyści nazwę materiału – usuwa spacje, zamienia na podkreślniki.
        /// </summary>
        private static string SanitizeMaterial(string material)
        {
            if (string.IsNullOrWhiteSpace(material))
                return "unknown";

            // Zamień spacje na podkreślniki
            string result = material.Trim().Replace(" ", "_");

            // Usuń znaki niedozwolone w nazwach plików
            result = Regex.Replace(result, @"[<>:""/\\|?*]", "");

            return result;
        }

        /// <summary>
        /// Usuwa niedozwolone znaki z nazwy pliku.
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }
            return fileName;
        }
    }
}
