using System;

namespace SolidEdge_FlatExporter
{
    /// <summary>
    /// Model danych reprezentujący pojedynczą część blaszaną znalezioną w złożeniu.
    /// </summary>
    public class SheetMetalPartInfo
    {
        /// <summary>Nazwa pliku PSM (np. "Bracket_01.psm")</summary>
        public string FileName { get; set; }

        /// <summary>Pełna ścieżka do pliku PSM</summary>
        public string FullPath { get; set; }

        /// <summary>Grubość materiału w mm</summary>
        public double Thickness { get; set; }

        /// <summary>Nazwa materiału (np. "DC01", "S235")</summary>
        public string Material { get; set; }

        /// <summary>Czy element jest zaznaczony do eksportu</summary>
        public bool IsSelected { get; set; }

        /// <summary>Nazwa części bez rozszerzenia</summary>
        public string PartName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName)) return string.Empty;
                int dotIndex = FileName.LastIndexOf('.');
                return dotIndex > 0 ? FileName.Substring(0, dotIndex) : FileName;
            }
        }

        public SheetMetalPartInfo()
        {
            IsSelected = true;
            Material = "unknown";
            Thickness = 0;
        }

        public override string ToString()
        {
            return $"{FileName} | {Thickness:F1}mm | {Material}";
        }
    }
}
