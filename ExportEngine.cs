using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolidEdge_FlatExporter
{
    public class ExportResult
    {
        public string PartFileName { get; set; }
        public string OutputPath { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ExportEngine
    {
        private const BindingFlags BF_METHOD = BindingFlags.InvokeMethod;
        private const BindingFlags BF_GET = BindingFlags.GetProperty;
        private const BindingFlags BF_SET = BindingFlags.SetProperty;

        public ExportResult ExportPart(dynamic seApp, SheetMetalPartInfo partInfo,
            string outputFolder, string format, bool showBendLines,
            bool includeThickness, bool includeMaterial, string customPrefix)
        {
            var result = new ExportResult
            {
                PartFileName = partInfo.FileName,
                Success = false,
                Message = ""
            };

            string outputFileName = NamingHelper.BuildFileName(
                partInfo, includeThickness, includeMaterial, customPrefix, format);
            string outputPath = Path.Combine(outputFolder, outputFileName);
            result.OutputPath = outputPath;

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }

            if (!File.Exists(partInfo.FullPath))
            {
                result.Message = $"File does not exist: {partInfo.FullPath}";
                return result;
            }

            string lastError = "";
            bool exported = false;
            List<string> debugLog = new List<string>();

            try
            {
                debugLog.Add("Export started.");

                // Pokaż Solid Edge aby operacje na widokach (PDF) i translatorze (DWG) działały poprawnie
                try { seApp.Visible = true; } catch { }

                if (format.ToLowerInvariant() == "pdf")
                {
                    debugLog.Add("Exporting to PDF (via Draft)...");
                    exported = ExportViaDraft(seApp, partInfo.FullPath, outputPath, "pdf", debugLog);
                }
                else if (format.ToLowerInvariant() == "dwg")
                {
                    debugLog.Add("Exporting to DWG (via Draft)...");
                    exported = ExportViaDraft(seApp, partInfo.FullPath, outputPath, "dwg", debugLog);
                }
                else
                {
                    debugLog.Add($"Exporting to {format.ToUpperInvariant()}...");
                    exported = ExportViaSaveAsFlatDXFEx(seApp, partInfo.FullPath, outputPath, debugLog);
                    if (exported && !showBendLines)
                    {
                        debugLog.Add("Removing bend lines from DXF...");
                        RemoveBendLinesFromDxf(outputPath);
                    }
                }

                if (exported)
                    result.Message = $"Successfully exported: {Path.GetFileName(outputPath)}";
            }
            catch (Exception ex)
            {
                lastError = GetInnerMessage(ex);
                debugLog.Add("Exception: " + lastError);
            }

            if (exported)
            {
                result.Success = true;
            }
            else
            {
                result.Message = $"Error. Log:\n" + string.Join("\n", debugLog.ToArray());
                if (!string.IsNullOrEmpty(lastError)) result.Message += "\nException: " + lastError;
                
                try
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                catch { }
            }

            return result;
        }

        private bool ExportViaDraft(dynamic seApp, string psmPath, string outputPath, string format, List<string> log)
        {
            dynamic draftDoc = null;
            bool exported = false;
            dynamic psmDoc = null;
            bool psmOpenedByUs = false;

            try
            {
                // 1. Ensure flat pattern exists in PSM
                log.Add("Checking if PSM has flat pattern...");
                psmDoc = FindOpenDocument(seApp, psmPath);
                if (psmDoc == null)
                {
                    psmDoc = InvokeMethod(GetProperty(seApp, "Documents"), "Open", new object[] { psmPath });
                    psmOpenedByUs = true;
                }

                if (!EnsureFlatPatternExists(psmDoc, log))
                {
                    log.Add("Warning: Could not ensure flat pattern. Export might fail.");
                }
                else if (psmOpenedByUs)
                {
                    log.Add("Saving PSM with new flat pattern...");
                    InvokeMethod(psmDoc, "Save", new object[] { });
                }

                // 2. Proceed with Draft
                log.Add($"Creating Draft for {format.ToUpper()}...");
                draftDoc = InvokeMethod(GetProperty(seApp, "Documents"), "Add", new object[] { "SolidEdge.DraftDocument" });

                
                log.Add("Getting ActiveSheet...");
                dynamic sheet = GetProperty(draftDoc, "ActiveSheet");
                
                log.Add("Setting BackgroundVisible...");
                try { sheet.GetType().InvokeMember("BackgroundVisible", BF_SET, null, sheet, new object[] { false }); }
                catch { try { sheet.GetType().InvokeMember("ShowBackground", BF_SET, null, sheet, new object[] { false }); } catch { } }
                
                log.Add("Configuring SheetSetup...");
                try
                {
                    dynamic setup = GetProperty(sheet, "SheetSetup");
                    setup.GetType().InvokeMember("SheetWidth", BF_SET, null, setup, new object[] { 2.0 });
                    setup.GetType().InvokeMember("SheetHeight", BF_SET, null, setup, new object[] { 2.0 });
                }
                catch (Exception ex) { log.Add("Setup Warning: " + ex.Message); }
                
                log.Add("Adding ModelLink...");
                dynamic modelLinks = GetProperty(draftDoc, "ModelLinks");
                dynamic modelLink = InvokeMethod(modelLinks, "Add", new object[] { psmPath });
                
                // Ensure the link is up to date
                try { InvokeMethod(modelLink, "Update", new object[] { }); } catch { }

                log.Add("Adding SheetMetalView...");
                dynamic views = GetProperty(sheet, "DrawingViews");
                
                // AddSheetMetalView(ModelLink, Orientation, Scale, X, Y, ViewType)
                // Using Type.Missing for X, Y and Orientation to let SE decide defaults if 1.0/1.0 fails
                // seSheetMetalFlatPatternView = 2
                object[] viewArgs = new object[] { modelLink, Type.Missing, 1.0, Type.Missing, Type.Missing, 2 };
                InvokeMethod(views, "AddSheetMetalView", viewArgs);
                
                log.Add($"Saving Draft as {format.ToUpper()}...");
                InvokeMethod(draftDoc, "SaveAs", new object[] { outputPath });


                
                exported = File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
            }
            catch (Exception ex)
            {
                log.Add($"Draft {format.ToUpper()} Error: " + GetInnerMessage(ex));
            }
            finally
            {
                if (draftDoc != null)
                {
                    try { InvokeMethod(draftDoc, "Close", new object[] { false }); Marshal.ReleaseComObject(draftDoc); } catch { }
                }
                if (psmOpenedByUs && psmDoc != null)
                {
                    try { InvokeMethod(psmDoc, "Close", new object[] { true }); Marshal.ReleaseComObject(psmDoc); } catch { }
                }
            }
            return exported;
        }

        private bool EnsureFlatPatternExists(dynamic doc, List<string> log)
        {
            try
            {
                dynamic fpModels = GetProperty(doc, "FlatPatternModels");
                int count = (int)GetProperty(fpModels, "Count");
                if (count > 0)
                {
                    log.Add("Flat pattern already exists.");
                    return true;
                }

                log.Add("Flat pattern missing. Attempting to create...");
                
                dynamic models = GetProperty(doc, "Models");
                if ((int)GetProperty(models, "Count") == 0) return false;
                dynamic model = InvokeMethod(models, "Item", new object[] { 1 });
                
                var faceEdge = FindLargestFace(model);
                if (faceEdge.Face == null) return false;

                // Add(pRefFace, pRefEdge, pVertex, OrientationType)
                // OrientationType: 0 = seFlatPatternOrientationFace
                InvokeMethod(fpModels, "Add", new object[] { faceEdge.Face, faceEdge.Edge, Type.Missing, 0 });
                log.Add("Flat pattern created successfully.");
                return true;
            }
            catch (Exception ex)
            {
                log.Add("Error creating flat pattern: " + ex.Message);
                return false;
            }
        }

        private (object Face, object Edge) FindLargestFace(object model)
        {
            try
            {
                object body = GetProperty(model, "Body");
                object faces = body.GetType().InvokeMember("Faces", BF_GET, null, body, new object[] { 1 }); // 1=igQueryAll
                int faceCount = (int)GetProperty(faces, "Count");
                
                double maxArea = -1;
                object bestFace = null;
                
                for (int i = 1; i <= faceCount; i++)
                {
                    try
                    {
                        object f = InvokeMethod(faces, "Item", new object[] { i });
                        double area = (double)GetProperty(f, "Area");
                        if (area > maxArea)
                        {
                            maxArea = area;
                            bestFace = f;
                        }
                    }
                    catch { }
                }

                if (bestFace != null)
                {
                    object edges = GetProperty(bestFace, "Edges");
                    object firstEdge = InvokeMethod(edges, "Item", new object[] { 1 });
                    return (bestFace, firstEdge);
                }
            }
            catch { }
            return (null, null);
        }






        private bool ExportViaSaveAsFlatDXFEx(dynamic seApp, string psmPath, string outputPath, List<string> log)
        {
            dynamic doc = null;
            bool openedByUs = false;

            try
            {
                doc = FindOpenDocument(seApp, psmPath);
                if (doc == null)
                {
                    log.Add("Opening PSM...");
                    doc = seApp.Documents.Open(psmPath);
                    openedByUs = true;
                }
                else { log.Add("PSM was already open."); }

                try
                {
                    log.Add("Opening PSM for DXF flattening...");
                doc = FindOpenDocument(seApp, psmPath);
                if (doc == null)
                {
                    doc = InvokeMethod(GetProperty(seApp, "Documents"), "Open", new object[] { psmPath });
                    openedByUs = true;
                }

                if (!EnsureFlatPatternExists(doc, log))
                {
                    log.Add("Could not ensure flat pattern exists.");
                }

                object models = GetProperty(doc, "Models");
                if ((int)GetProperty(models, "Count") == 0) return false;
                object model = InvokeMethod(models, "Item", new object[] { 1 });

                var faceEdge = FindLargestFace(model);
                object face = faceEdge.Face;
                object edge = faceEdge.Edge;

                bool useFP = false;
                try
                {
                    object fpModels = GetProperty(doc, "FlatPatternModels");
                    int fpCount = (int)GetProperty(fpModels, "Count");
                    if (fpCount > 0)
                    {
                        for (int i = 1; i <= fpCount; i++)
                        {
                            try
                            {
                                object fpm = InvokeMethod(fpModels, "Item", new object[] { i });
                                InvokeMethod(fpm, "MakeActive", null);
                                InvokeMethod(fpm, "Update", null);
                                if ((bool)GetProperty(fpm, "IsUpToDate")) { useFP = true; break; }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                bool exported = false;
                if (face != null && edge != null)
                {
                    log.Add($"Exporting DXF (useFP={useFP})...");
                    try
                    {
                        InvokeMethod(models, "SaveAsFlatDXFEx", new object[] { outputPath, face, edge, new DispatchWrapper(null), useFP });
                        exported = FileHasContent(outputPath, log);
                    }
                    catch { }
                }


                if (!exported && useFP)
                {
                    log.Add($"Attempt 2 (DispatchWrapper Null, useFP=True)...");
                    try
                    {
                        var nil = new DispatchWrapper(null);
                        models.GetType().InvokeMember("SaveAsFlatDXFEx", BF_METHOD, null, models,
                            new object[] { outputPath, nil, nil, nil, true });
                        exported = FileHasContent(outputPath, log);
                    }
                    catch { }
                }

                if (!exported)
                {
                    log.Add($"Attempt 3 (SaveCopyAs)...");
                    try
                    {
                        doc.GetType().InvokeMember("SaveCopyAs", BF_METHOD, null, doc, new object[] { outputPath });
                        exported = FileHasContent(outputPath, log);
                    }
                    catch { }
                }

                return exported;
            }
            finally
            {
                if (openedByUs && doc != null)
                {
                    try { doc.Close(false); Marshal.ReleaseComObject(doc); } catch { }
                }
            }
        }

        private void RemoveBendLinesFromDxf(string dxfPath)
        {
            if (!File.Exists(dxfPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(dxfPath);
                var newLines = new List<string>(lines.Length);
                List<string> currentBlock = new List<string>();
                bool dropBlock = false;
                
                for (int i = 0; i < lines.Length - 1; i += 2)
                {
                    string codeLine = lines[i]; string valueLine = lines[i + 1];
                    string codeTrimmed = codeLine.Trim(); string valueTrimmed = valueLine.Trim();

                    if (codeTrimmed == "0")
                    {
                        if (currentBlock.Count > 0 && !dropBlock) newLines.AddRange(currentBlock);
                        currentBlock.Clear(); dropBlock = false;
                        string upperValue = valueTrimmed.ToUpperInvariant();
                        if (upperValue == "SECTION" || upperValue == "ENDSEC" || upperValue == "TABLE" || upperValue == "ENDTAB" || upperValue == "EOF")
                        {
                            newLines.Add(codeLine); newLines.Add(valueLine);
                        }
                        else { currentBlock.Add(codeLine); currentBlock.Add(valueLine); }
                    }
                    else
                    {
                        if (currentBlock.Count == 0) { newLines.Add(codeLine); newLines.Add(valueLine); }
                        else
                        {
                            currentBlock.Add(codeLine); currentBlock.Add(valueLine);
                            string lowerValue = valueTrimmed.ToLowerInvariant();
                            if (codeTrimmed == "8" && IsBendLayer(lowerValue)) dropBlock = true;
                            if (codeTrimmed == "2" && currentBlock.Count >= 2 && currentBlock[1].Trim().ToUpperInvariant() == "LAYER" && IsBendLayer(lowerValue)) dropBlock = true;
                        }
                    }
                }
                if (currentBlock.Count > 0 && !dropBlock) newLines.AddRange(currentBlock);
                if (lines.Length % 2 != 0) newLines.Add(lines[lines.Length - 1]);
                File.WriteAllLines(dxfPath, newLines);
            }
            catch { }
        }

        private bool IsBendLayer(string layerName)
        {
            if (layerName == "up" || layerName == "down" || layerName.Contains("bend") || layerName.Contains("gięc") || layerName.Contains("giec") || layerName.Contains("center")) return true;
            return false;
        }

        private bool FileHasContent(string path, List<string> log)
        {
            if (!File.Exists(path)) return false;
            var fi = new FileInfo(path);
            if (fi.Length < 100) return false;
            return true;
        }

        private static object GetProperty(object comObj, string name)
        {
            return comObj.GetType().InvokeMember(name, BF_GET, null, comObj, null);
        }

        private static object InvokeMethod(object comObj, string name, object[] args)
        {
            return comObj.GetType().InvokeMember(name, BF_METHOD, null, comObj, args);
        }

        private static string GetInnerMessage(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message;
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
                        if (string.Equals((string)doc.FullName, filePath, StringComparison.OrdinalIgnoreCase)) return doc;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        public List<ExportResult> ExportAll(dynamic seApp, List<SheetMetalPartInfo> parts,
            string outputFolder, string format, bool showBendLines,
            bool includeThickness, bool includeMaterial, string customPrefix,
            Action<int, int, string> progressCallback = null)
        {
            var results = new List<ExportResult>();
            var selectedParts = parts.FindAll(p => p.IsSelected);

            for (int i = 0; i < selectedParts.Count; i++)
            {
                var part = selectedParts[i];
                progressCallback?.Invoke(i, selectedParts.Count, part.FileName);
                var result = ExportPart(seApp, part, outputFolder, format, showBendLines,
                    includeThickness, includeMaterial, customPrefix);
                results.Add(result);
            }

            progressCallback?.Invoke(selectedParts.Count, selectedParts.Count, "Finished");
            return results;
        }
    }
}
