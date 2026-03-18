using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SolidEdge_FlatExporter
{
    /// <summary>
    /// Główny formularz aplikacji Sheet Metal Flat Pattern Exporter.
    /// Działa standalone – użytkownik wybiera plik ASM, program skanuje i eksportuje.
    /// </summary>
    public partial class FormMain : Form
    {
        private SolidEdgeConnector _connector;
        private List<SheetMetalPartInfo> _parts;
        private BackgroundWorker _scanWorker;
        private BackgroundWorker _exportWorker;
        private bool _isExporting = false;

        /// <summary>
        /// Konstruktor – przyjmuje opcjonalną ścieżkę pliku (z argumentu linii komend / drag-drop na exe).
        /// </summary>
        public FormMain(string initialFilePath = null)
        {
            _parts = new List<SheetMetalPartInfo>();

            InitializeComponent();
            InitializeWorkers();
            UpdateFileNamePreview();

            // Drag & Drop na okno programu
            this.AllowDrop = true;
            this.DragEnter += FormMain_DragEnter;
            this.DragDrop += FormMain_DragDrop;

            // Jeśli plik został podany (np. przeciągnięty na exe/skrót)
            if (!string.IsNullOrEmpty(initialFilePath) && File.Exists(initialFilePath))
            {
                LoadAssemblyPath(initialFilePath);
            }
        }

        /// <summary>
        /// Ładuje ścieżkę złożenia i automatycznie uruchamia skanowanie.
        /// </summary>
        private void LoadAssemblyPath(string filePath)
        {
            txtAsmPath.Text = filePath;
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                txtOutputFolder.Text = dir;

            // Auto-skan po załadowaniu formularza
            this.Shown += (s, e) =>
            {
                if (File.Exists(filePath))
                    btnScanAsm_Click(this, EventArgs.Empty);
            };
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && files[0].EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && File.Exists(files[0]))
            {
                LoadAssemblyPath(files[0]);
                // Uruchom skanowanie od razu
                btnScanAsm_Click(this, EventArgs.Empty);
            }
        }

        private void InitializeWorkers()
        {
            _scanWorker = new BackgroundWorker();
            _scanWorker.WorkerReportsProgress = true;
            _scanWorker.DoWork += ScanWorker_DoWork;
            _scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;

            _exportWorker = new BackgroundWorker();
            _exportWorker.WorkerReportsProgress = true;
            _exportWorker.WorkerSupportsCancellation = true;
            _exportWorker.DoWork += ExportWorker_DoWork;
            _exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            _exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Zamknij Solid Edge jeśli został uruchomiony przez nas
            if (_connector != null)
            {
                _connector.Dispose();
                _connector = null;
            }
        }

        // ===================================================
        // Panel 0 – Wybór pliku złożenia ASM
        // ===================================================

        private void btnBrowseAsm_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Solid Edge Assembly";
                dlg.Filter = "Solid Edge Assemblies (*.asm)|*.asm|All files (*.*)|*.*";
                dlg.FilterIndex = 1;

                if (!string.IsNullOrEmpty(txtAsmPath.Text))
                    dlg.InitialDirectory = Path.GetDirectoryName(txtAsmPath.Text);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtAsmPath.Text = dlg.FileName;

                    // Ustaw folder docelowy na folder złożenia
                    string asmDir = Path.GetDirectoryName(dlg.FileName);
                    if (!string.IsNullOrEmpty(asmDir))
                        txtOutputFolder.Text = asmDir;
                }
            }
        }

        private void btnScanAsm_Click(object sender, EventArgs e)
        {
            string asmPath = txtAsmPath.Text.Trim();

            if (string.IsNullOrEmpty(asmPath) || !File.Exists(asmPath))
            {
                MessageBox.Show(
                    "Select a valid assembly file (.asm).",
                    "Missing file",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!asmPath.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "The selected file is not an assembly (.asm).",
                    "Invalid file",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Zablokuj UI na czas skanowania
            SetScanningState(true);
            _scanWorker.RunWorkerAsync(asmPath);
        }

        private void SetScanningState(bool scanning)
        {
            btnBrowseAsm.Enabled = !scanning;
            btnScanAsm.Enabled = !scanning;
            btnExport.Enabled = false;
            dgvParts.Rows.Clear();

            if (scanning)
            {
                lblStatus.Text = "Connecting to Solid Edge and scanning assembly...";
                lblStatus.ForeColor = System.Drawing.Color.DarkBlue;
                progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
            }
        }

        // ===================================================
        // Skanowanie w tle
        // ===================================================

        private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string asmPath = (string)e.Argument;

            // Połącz z Solid Edge (lub uruchom nową instancję)
            if (_connector == null)
            {
                _connector = new SolidEdgeConnector();
                _connector.Connect();
            }

            // Skanuj złożenie
            var scanner = new PartScanner();
            var parts = scanner.ScanAssembly(_connector.Application, asmPath);

            e.Result = new ScanResult { Parts = parts, Warnings = scanner.Warnings };
        }

        private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetScanningState(false);

            if (e.Error != null)
            {
                lblStatus.Text = $"Error: {e.Error.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                MessageBox.Show(
                    $"Error during scan:\n\n{e.Error.Message}",
                    "Scan Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var result = (ScanResult)e.Result;
            _parts = result.Parts;

            // Wypełnij DataGridView
            dgvParts.Rows.Clear();
            foreach (var part in _parts)
            {
                int rowIdx = dgvParts.Rows.Add();
                var row = dgvParts.Rows[rowIdx];
                row.Cells["colSelect"].Value = part.IsSelected;
                row.Cells["colFileName"].Value = part.FileName;
                row.Cells["colThickness"].Value = part.Thickness > 0
                    ? part.Thickness.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " mm"
                    : "unknown";
                row.Cells["colMaterial"].Value = part.Material;
                row.Tag = part;
            }

            // Status i ostrzeżenia
            if (_parts.Count == 0)
            {
                lblStatus.Text = "No sheet metal parts (.psm) found in the assembly";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else
            {
                lblStatus.Text = $"Found {_parts.Count} sheet metal parts";
                lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
                btnExport.Enabled = true;
            }

            // Pokaż ostrzeżenia jeśli są
            if (result.Warnings.Count > 0)
            {
                string warnings = string.Join("\n", result.Warnings);
                MessageBox.Show(
                    $"Warnings during scan:\n\n{warnings}",
                    "Warnings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private class ScanResult
        {
            public List<SheetMetalPartInfo> Parts { get; set; }
            public List<string> Warnings { get; set; }
        }

        // ===================================================
        // Panel A – Zaznacz / Odznacz wszystko
        // ===================================================

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            SetAllChecked(true);
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            SetAllChecked(false);
        }

        private void SetAllChecked(bool isChecked)
        {
            foreach (DataGridViewRow row in dgvParts.Rows)
                row.Cells["colSelect"].Value = isChecked;
        }

        // ===================================================
        // Panel B – Przeglądaj folder docelowy
        // ===================================================

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Output Folder for Export";
                dlg.SelectedPath = txtOutputFolder.Text;
                dlg.ShowNewFolderButton = true;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
        }

        // ===================================================
        // Panel C + E – Format / Nazewnictwo changed
        // ===================================================

        private void NamingChanged(object sender, EventArgs e) => UpdateFileNamePreview();

        private void UpdateFileNamePreview()
        {
            string format = GetSelectedFormat();
            string preview = NamingHelper.GetPreview(
                "Bracket_01", 2.0, "DC01",
                chkAddThickness.Checked, chkAddMaterial.Checked, txtCustomPrefix.Text, format);
            lblFileNamePreview.Text = preview;
        }

        private string GetSelectedFormat()
        {
            if (rbDWG.Checked) return "dwg";
            if (rbPDF.Checked) return "pdf";
            return "dxf";
        }

        // ===================================================
        // Panel F – Eksportuj / Anuluj
        // ===================================================

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_isExporting) return;

            SyncSelections();

            var selected = _parts.FindAll(p => p.IsSelected);
            if (selected.Count == 0)
            {
                MessageBox.Show(
                    "No items selected for export.",
                    "No selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string outputFolder = txtOutputFolder.Text.Trim();
            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show(
                    "Specify an output folder.",
                    "Output folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Utwórz folder jeśli nie istnieje
            if (!Directory.Exists(outputFolder))
            {
                try { Directory.CreateDirectory(outputFolder); }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Cannot create folder:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            SetExportingState(true);

            var args = new ExportArgs
            {
                Parts = _parts,
                OutputFolder = outputFolder,
                Format = GetSelectedFormat(),
                ShowBendLines = chkBendLines.Checked,
                IncludeThickness = chkAddThickness.Checked,
                IncludeMaterial = chkAddMaterial.Checked,
                CustomPrefix = txtCustomPrefix.Text
            };

            _exportWorker.RunWorkerAsync(args);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isExporting)
            {
                _exportWorker.CancelAsync();
                lblStatus.Text = "Canceling...";
            }
            else
            {
                this.Close();
            }
        }

        private void SyncSelections()
        {
            for (int i = 0; i < dgvParts.Rows.Count && i < _parts.Count; i++)
            {
                object cellValue = dgvParts.Rows[i].Cells["colSelect"].Value;
                _parts[i].IsSelected = cellValue != null && (bool)cellValue;
            }
        }

        private void SetExportingState(bool exporting)
        {
            _isExporting = exporting;
            btnExport.Enabled = !exporting;
            btnSelectAll.Enabled = !exporting;
            btnDeselectAll.Enabled = !exporting;
            btnBrowse.Enabled = !exporting;
            btnBrowseAsm.Enabled = !exporting;
            btnScanAsm.Enabled = !exporting;
            dgvParts.Enabled = !exporting;
            grpFormat.Enabled = !exporting;
            grpBendLines.Enabled = !exporting;
            grpNaming.Enabled = !exporting;

            if (exporting)
            {
                btnCancel.Text = "Cancel";
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
            }
            else
            {
                btnCancel.Text = "Close";
            }
        }

        // ===================================================
        // BackgroundWorker – eksport w tle
        // ===================================================

        private void ExportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            var args = (ExportArgs)e.Argument;

            if (_connector == null)
            {
                _connector = new SolidEdgeConnector();
            }

            try
            {
                if (_connector.Application == null) _connector.Connect();
            }
            catch (Exception ex)
            {
                e.Result = new List<ExportResult> { new ExportResult { Success = false, Message = "Connection failed: " + ex.Message } };
                return;
            }

            var engine = new ExportEngine();
            var results = new List<ExportResult>();
            var selected = args.Parts.FindAll(p => p.IsSelected);

            for (int i = 0; i < selected.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                bool isConnected = false;
                try
                {
                    if (_connector.Application != null)
                    {
                        string version = _connector.Application.Version;
                        isConnected = true;
                    }
                }
                catch { }

                if (!isConnected)
                {
                    try { _connector.Connect(); } catch { }
                }

                var part = selected[i];
                int progressPercent = (int)((double)i / selected.Count * 100);
                worker.ReportProgress(progressPercent,
                    $"Exporting: {part.FileName} ({i + 1}/{selected.Count})");

                ExportResult result = null;
                int retries = 0;
                while (retries < 2)
                {
                    try
                    {
                        result = engine.ExportPart(_connector.Application, part,
                            args.OutputFolder, args.Format, args.ShowBendLines,
                            args.IncludeThickness, args.IncludeMaterial, args.CustomPrefix);
                        break;
                    }
                    catch (Exception ex) when (ex.Message.Contains("0x800706BA") || ex.Message.Contains("RPC"))
                    {
                        retries++;
                        try { _connector.Connect(); } catch { }
                    }
                    catch (Exception ex)
                    {
                        result = new ExportResult { Success = false, Message = "Export Error: " + ex.Message, PartFileName = part.FileName };
                        break;
                    }
                }
                
                if (result == null) result = new ExportResult { Success = false, Message = "Export failed after retries.", PartFileName = part.FileName };
                results.Add(result);
            }

            e.Result = results;
        }

        private void ExportWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = Math.Min(e.ProgressPercentage, 100);
            lblStatus.Text = (string)e.UserState;
            lblStatus.ForeColor = System.Drawing.Color.DarkBlue;
        }

        private void ExportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetExportingState(false);
            progressBar.Value = 100;

            if (e.Cancelled)
            {
                lblStatus.Text = "Export canceled";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
                return;
            }

            if (e.Error != null)
            {
                lblStatus.Text = $"Export Error: {e.Error.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                MessageBox.Show(
                    $"An error occurred during export:\n{e.Error.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var results = (List<ExportResult>)e.Result;
            ShowExportSummary(results);
        }

        private void ShowExportSummary(List<ExportResult> results)
        {
            int successCount = 0;
            int failCount = 0;
            var sb = new StringBuilder();
            sb.AppendLine("=== EXPORT SUMMARY ===");
            sb.AppendLine();

            var successes = results.FindAll(r => r.Success);
            successCount = successes.Count;
            if (successCount > 0)
            {
                sb.AppendLine($"✓ Successfully exported: {successCount}");
                foreach (var s in successes)
                    sb.AppendLine($"   • {s.OutputPath}");
                sb.AppendLine();
            }

            var failures = results.FindAll(r => !r.Success);
            failCount = failures.Count;
            if (failCount > 0)
            {
                sb.AppendLine($"✗ Skipped / Errors: {failCount}");
                foreach (var f in failures)
                    sb.AppendLine($"   • {f.PartFileName}: {f.Message}");
            }

            if (failCount == 0)
            {
                lblStatus.Text = $"Export completed successfully! ({successCount} files)";
                lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
            }
            else
            {
                lblStatus.Text = $"Export completed: {successCount} OK, {failCount} skipped";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }

            MessageBox.Show(
                sb.ToString(),
                "Export Summary",
                MessageBoxButtons.OK,
                failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private class ExportArgs
        {
            public List<SheetMetalPartInfo> Parts { get; set; }
            public string OutputFolder { get; set; }
            public string Format { get; set; }
            public bool ShowBendLines { get; set; }
            public bool IncludeThickness { get; set; }
            public bool IncludeMaterial { get; set; }
            public string CustomPrefix { get; set; }
        }
    }
}
