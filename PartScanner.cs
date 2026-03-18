using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SolidEdge_FlatExporter
{
    /// <summary>
    /// Odpowiada za uruchomienie / połączenie z Solid Edge i zarządzanie jego cyklem życia.
    /// </summary>
    public class SolidEdgeConnector : IDisposable
    {
        private dynamic _seApp;
        private bool _startedByUs = false;
        private bool _disposed = false;

        // P/Invoke: Marshal.GetActiveObject usunięty w .NET 5+
        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [DllImport("ole32.dll")]
        private static extern int CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

        /// <summary>
        /// Obiekt Application Solid Edge (dynamic / late-binding).
        /// </summary>
        public dynamic Application => _seApp;

        /// <summary>
        /// Czy Solid Edge został uruchomiony przez nasz program.
        /// </summary>
        public bool StartedByUs => _startedByUs;

        /// <summary>
        /// Łączy się z istniejącą instancją Solid Edge lub uruchamia nową w tle.
        /// </summary>
        public void Connect()
        {
            // Najpierw próbuj połączyć się z istniejącą instancją
            try
            {
                _seApp = GetActiveComObject("SolidEdge.Application");
                _startedByUs = false;
                return;
            }
            catch { }

            // Jeśli nie ma uruchomionej instancji – uruchom nową w tle
            try
            {
                Type seType = Type.GetTypeFromProgID("SolidEdge.Application");
                if (seType == null)
                    throw new InvalidOperationException(
                        "Solid Edge is not installed on this computer.\n" +
                        "Install Solid Edge and try again.");

                _seApp = Activator.CreateInstance(seType);
                _startedByUs = true;

                // Ukryj okno i wyłącz alerty
                try { _seApp.Visible = false; } catch { }
                try { _seApp.DisplayAlerts = false; } catch { }
            }
            catch (InvalidOperationException)
            {
                throw; // Rethrow nasz własny wyjątek
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot start Solid Edge:\n{ex.Message}");
            }
        }

        /// <summary>
        /// Odpowiednik Marshal.GetActiveObject dla .NET 5+/8+
        /// </summary>
        private static object GetActiveComObject(string progId)
        {
            Guid clsid;
            int hr = CLSIDFromProgID(progId, out clsid);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            object obj;
            GetActiveObject(ref clsid, IntPtr.Zero, out obj);
            return obj;
        }

        /// <summary>
        /// Zamyka Solid Edge jeśli został uruchomiony przez nas.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_seApp != null)
            {
                if (_startedByUs)
                {
                    try { _seApp.Quit(); } catch { }
                }

                try { Marshal.ReleaseComObject(_seApp); } catch { }
                _seApp = null;
            }
        }
    }

    /// <summary>
    /// Skanuje złożenie Solid Edge rekurencyjnie w poszukiwaniu części blaszanych (.psm).
    /// Używa late-binding (dynamic).
    /// </summary>
    public class PartScanner
    {
        private readonly HashSet<string> _processedPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _warnings = new List<string>();

        /// <summary>
        /// Ostrzeżenia zgłoszone podczas skanowania.
        /// </summary>
        public List<string> Warnings => _warnings;

        /// <summary>
        /// Skanuje złożenie i zwraca listę części blaszanych.
        /// </summary>
        /// <param name="seApp">Solid Edge Application</param>
        /// <param name="asmFilePath">Ścieżka do pliku złożenia .asm</param>
        /// <returns>Lista znalezionych części blaszanych</returns>
        public List<SheetMetalPartInfo> ScanAssembly(dynamic seApp, string asmFilePath)
        {
            var parts = new List<SheetMetalPartInfo>();
            _processedPaths.Clear();
            _warnings.Clear();

            ScanAssemblyRecursive(seApp, asmFilePath, parts);

            return parts;
        }

        private void ScanAssemblyRecursive(dynamic seApp, string asmFilePath, List<SheetMetalPartInfo> parts)
        {
            if (string.IsNullOrEmpty(asmFilePath)) return;
            
            // Unikaj zapętlenia skanowania tego samego pliku ASM
            if (_processedPaths.Contains(asmFilePath)) return;
            _processedPaths.Add(asmFilePath);

            // Otwórz złożenie
            dynamic assemblyDoc = null;
            bool asmOpenedByUs = false;

            try
            {
                assemblyDoc = FindOpenDocument(seApp, asmFilePath);
                if (assemblyDoc == null)
                {
                    assemblyDoc = seApp.Documents.Open(asmFilePath);
                    asmOpenedByUs = true;
                }

                // Skanuj wystąpienia
                dynamic occurrences = assemblyDoc.Occurrences;
                ScanOccurrences(seApp, occurrences, parts);
            }
            catch (Exception ex)
            {
                _warnings.Add($"Assembly open error ({Path.GetFileName(asmFilePath)}): {ex.Message}");
            }
            finally
            {
                if (asmOpenedByUs && assemblyDoc != null)
                {
                    try { assemblyDoc.Close(false); } catch { }
                    try { Marshal.ReleaseComObject(assemblyDoc); } catch { }
                }
            }
        }

        private void ScanOccurrences(dynamic seApp, dynamic occurrences, List<SheetMetalPartInfo> parts)
        {
            int count = 0;
            try { count = occurrences.Count; } catch { return; }

            for (int i = 1; i <= count; i++)
            {
                try
                {
                    dynamic occurrence = occurrences.Item(i);
                    ProcessOccurrence(seApp, occurrence, parts);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[PartScanner] Occurrence error {i}: {ex.Message}");
                }
            }
        }

        private void ProcessOccurrence(dynamic seApp, dynamic occurrence, List<SheetMetalPartInfo> parts)
        {
            try
            {
                string filePath = "";

                try { filePath = (string)occurrence.OccurrenceFileName; }
                catch
                {
                    try
                    {
                        dynamic occDoc = occurrence.OccurrenceDocument;
                        filePath = (string)occDoc.FullName;
                    }
                    catch { return; }
                }

                if (string.IsNullOrEmpty(filePath)) return;

                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                // Podzłożenie – skanuj rekurencyjnie upewniając się, że wejdzie we wszystkie podpoziomy
                if (extension == ".asm")
                {
                    try
                    {
                        ScanAssemblyRecursive(seApp, filePath, parts);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PartScanner] Failed to scan Subassembly {filePath}: {ex.Message}");
                    }
                    return;
                }

                // Część blaszana
                if (extension == ".psm")
                {
                    if (_processedPaths.Contains(filePath)) return;
                    _processedPaths.Add(filePath);

                    var partInfo = ExtractPartInfo(seApp, filePath);
                    if (partInfo != null)
                        parts.Add(partInfo);
                }
            }
            catch { }
        }

        /// <summary>
        /// Wyciąga informacje o części – grubość i materiał.
        /// WAŻNE: otwiera dokument READ-ONLY i NIGDY nie zapisuje zmian.
        /// </summary>
        private SheetMetalPartInfo ExtractPartInfo(dynamic seApp, string filePath)
        {
            var info = new SheetMetalPartInfo
            {
                FileName = Path.GetFileName(filePath),
                FullPath = filePath,
                Material = "unknown",
                Thickness = 0,
                IsSelected = true
            };

            dynamic doc = null;
            bool openedByUs = false;

            try
            {
                doc = FindOpenDocument(seApp, filePath);

                if (doc == null)
                {
                    doc = seApp.Documents.Open(filePath);
                    openedByUs = true;
                }

                // Pobierz grubość – priorytetowo przez Variables
                info.Thickness = GetThickness(doc);

                // Pobierz materiał
                info.Material = GetMaterial(doc);

                if (info.Thickness == 0)
                {
                    _warnings.Add($"{info.FileName}: failed to read thickness");
                }
            }
            catch (Exception ex)
            {
                _warnings.Add($"{info.FileName}: read error - {ex.Message}");
            }
            finally
            {
                if (openedByUs && doc != null)
                {
                    try { doc.Close(false); } catch { } // false = NIE zapisuj
                    try { Marshal.ReleaseComObject(doc); } catch { }
                }
            }

            return info;
        }

        /// <summary>
        /// Pobiera grubość materiału z dokumentu SheetMetal.
        /// Próbuje wielu metod aż jedna zadziała.
        /// </summary>
        private double GetThickness(dynamic doc)
        {
            // ============================================================
            // Metoda 1: Przez Variables – najskuteczniejsza dla Solid Edge
            // ============================================================
            try
            {
                dynamic variables = doc.Variables;
                double t = FindThicknessInVariables(variables);
                if (t > 0) return t;
            }
            catch { }

            // ============================================================
            // Metoda 2: Przez Models.Item(1) – SheetMetalModel.Thickness
            // ============================================================
            try
            {
                dynamic models = doc.Models;
                if (models != null && models.Count > 0)
                {
                    dynamic model = models.Item(1);
                    try
                    {
                        double t = (double)model.Thickness;
                        return ConvertToMm(t);
                    }
                    catch { }
                }
            }
            catch { }

            // ============================================================
            // Metoda 3: Przez doc.Thickness (bezpośrednio)
            // ============================================================
            try
            {
                double t = (double)doc.Thickness;
                return ConvertToMm(t);
            }
            catch { }

            // ============================================================
            // Metoda 4: Przez PropertySets – szukanie "Thickness" property
            // ============================================================
            try
            {
                double t = FindThicknessInProperties(doc);
                if (t > 0) return t;
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Szuka zmiennej grubości w kolekcji Variables.
        /// </summary>
        private double FindThicknessInVariables(dynamic variables)
        {
            // Szukaj w zmiennych wymiarowych (DimensionVariables)
            try
            {
                dynamic dimVars = null;
                try { dimVars = variables.DimensionVariables; } catch { }

                if (dimVars != null)
                {
                    int count = 0;
                    try { count = dimVars.Count; } catch { }

                    for (int i = 1; i <= count; i++)
                    {
                        try
                        {
                            dynamic v = dimVars.Item(i);
                            string name = "";
                            try { name = (string)v.Name; } catch { continue; }

                            if (IsThicknessVariable(name))
                            {
                                double val = (double)v.Value;
                                return ConvertToMm(val);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Szukaj we wszystkich zmiennych
            try
            {
                int count = 0;
                try { count = variables.Count; } catch { }

                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        dynamic v = variables.Item(i);
                        string name = "";
                        try { name = (string)v.Name; } catch { continue; }

                        if (IsThicknessVariable(name))
                        {
                            double val = (double)v.Value;
                            return ConvertToMm(val);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Sprawdza czy nazwa zmiennej odpowiada grubości.
        /// </summary>
        private bool IsThicknessVariable(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string nameLower = name.ToLowerInvariant();

            return nameLower.Contains("thickness")
                || nameLower.Contains("grubość")
                || nameLower.Contains("grubosc")
                || nameLower.Contains("material_thickness")
                || nameLower.Contains("sheet_metal_thickness")
                || nameLower.Contains("sheet metal gage")
                || nameLower.Contains("sheetmetalthickness")
                || nameLower.Contains("t_global")
                || nameLower == "t";
        }

        /// <summary>
        /// Szuka grubości w PropertySets dokumentu.
        /// </summary>
        private double FindThicknessInProperties(dynamic doc)
        {
            try
            {
                dynamic propSets = doc.Properties;
                int psCount = 0;
                try { psCount = propSets.Count; } catch { return 0; }

                for (int ps = 1; ps <= psCount; ps++)
                {
                    try
                    {
                        dynamic propSet = propSets.Item(ps);
                        int pCount = 0;
                        try { pCount = propSet.Count; } catch { continue; }

                        for (int p = 1; p <= pCount; p++)
                        {
                            try
                            {
                                dynamic prop = propSet.Item(p);
                                string propName = "";
                                try { propName = (string)prop.Name; } catch { continue; }

                                if (IsThicknessVariable(propName))
                                {
                                    object val = prop.Value;
                                    if (val != null)
                                    {
                                        double t = Convert.ToDouble(val);
                                        return ConvertToMm(t);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Konwertuje wartość na milimetry (Solid Edge przechowuje w metrach).
        /// </summary>
        private double ConvertToMm(double value)
        {
            if (value <= 0) return 0;

            // Solid Edge przechowuje wymiary w metrach
            // Typowe grubości blach: 0.5mm – 20mm
            // W metrach: 0.0005 – 0.020
            if (value < 0.5)
            {
                // Prawdopodobnie w metrach → konwertuj na mm
                return Math.Round(value * 1000.0, 2);
            }
            else
            {
                // Prawdopodobnie już w milimetrach
                return Math.Round(value, 2);
            }
        }

        /// <summary>
        /// Pobiera nazwę materiału z dokumentu.
        /// </summary>
        private string GetMaterial(dynamic doc)
        {
            // Metoda 1: Przez Properties – "Material"
            try
            {
                dynamic propSets = doc.Properties;
                int psCount = 0;
                try { psCount = propSets.Count; } catch { }

                for (int ps = 1; ps <= psCount; ps++)
                {
                    try
                    {
                        dynamic propSet = propSets.Item(ps);
                        int pCount = 0;
                        try { pCount = propSet.Count; } catch { continue; }

                        for (int p = 1; p <= pCount; p++)
                        {
                            try
                            {
                                dynamic prop = propSet.Item(p);
                                string propName = "";
                                try { propName = (string)prop.Name; } catch { continue; }

                                if (propName.Equals("Material", StringComparison.OrdinalIgnoreCase) ||
                                    propName.Equals("Materiał", StringComparison.OrdinalIgnoreCase))
                                {
                                    object val = prop.Value;
                                    if (val != null)
                                    {
                                        string mat = val.ToString().Trim();
                                        if (!string.IsNullOrEmpty(mat))
                                            return mat;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Metoda 2: Przez ActiveMaterial / MaterialName
            string[] materialProps = { "ActiveMaterial", "MaterialName", "MaterialTable" };
            foreach (string propName in materialProps)
            {
                try
                {
                    object val = doc.GetType().InvokeMember(propName,
                        System.Reflection.BindingFlags.GetProperty, null, doc, null);
                    if (val != null)
                    {
                        string mat = val.ToString().Trim();
                        if (!string.IsNullOrEmpty(mat))
                            return mat;
                    }
                }
                catch { }
            }

            return "unknown";
        }

        private dynamic FindOpenDocument(dynamic seApp, string filePath)
        {
            try
            {
                dynamic documents = seApp.Documents;
                int docCount = documents.Count;

                for (int i = 1; i <= docCount; i++)
                {
                    try
                    {
                        dynamic doc = documents.Item(i);
                        string docPath = (string)doc.FullName;
                        if (string.Equals(docPath, filePath, StringComparison.OrdinalIgnoreCase))
                            return doc;
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }
    }
}
