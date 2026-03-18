namespace SolidEdge_FlatExporter
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.Text = "SolidEdge DXF Extractor by Mateusz Gołębiewski";
            this.Size = new System.Drawing.Size(750, 740);
            this.MinimumSize = new System.Drawing.Size(700, 720);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);

            // ===================================================
            // Panel 0 – Wybór pliku złożenia ASM
            // ===================================================
            this.grpAssembly = new System.Windows.Forms.GroupBox();
            this.grpAssembly.Text = "Assembly (.asm file)";
            this.grpAssembly.Location = new System.Drawing.Point(12, 12);
            this.grpAssembly.Size = new System.Drawing.Size(710, 55);

            this.txtAsmPath = new System.Windows.Forms.TextBox();
            this.txtAsmPath.Location = new System.Drawing.Point(10, 22);
            this.txtAsmPath.Size = new System.Drawing.Size(500, 23);
            this.txtAsmPath.ReadOnly = true;
            this.txtAsmPath.PlaceholderText = "Select .asm assembly file...";

            this.btnBrowseAsm = new System.Windows.Forms.Button();
            this.btnBrowseAsm.Text = "Browse...";
            this.btnBrowseAsm.Location = new System.Drawing.Point(518, 21);
            this.btnBrowseAsm.Size = new System.Drawing.Size(92, 25);
            this.btnBrowseAsm.Click += new System.EventHandler(this.btnBrowseAsm_Click);

            this.btnScanAsm = new System.Windows.Forms.Button();
            this.btnScanAsm.Text = "Scan";
            this.btnScanAsm.Location = new System.Drawing.Point(618, 21);
            this.btnScanAsm.Size = new System.Drawing.Size(82, 25);
            this.btnScanAsm.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnScanAsm.Click += new System.EventHandler(this.btnScanAsm_Click);

            this.grpAssembly.Controls.Add(this.txtAsmPath);
            this.grpAssembly.Controls.Add(this.btnBrowseAsm);
            this.grpAssembly.Controls.Add(this.btnScanAsm);

            // ===================================================
            // Panel A – Lista elementów blaszanych
            // ===================================================
            this.grpPartsList = new System.Windows.Forms.GroupBox();
            this.grpPartsList.Text = "Sheet Metal Parts in Assembly";
            this.grpPartsList.Location = new System.Drawing.Point(12, 73);
            this.grpPartsList.Size = new System.Drawing.Size(710, 260);

            this.dgvParts = new System.Windows.Forms.DataGridView();
            this.dgvParts.Location = new System.Drawing.Point(10, 22);
            this.dgvParts.Size = new System.Drawing.Size(690, 195);
            this.dgvParts.AllowUserToAddRows = false;
            this.dgvParts.AllowUserToDeleteRows = false;
            this.dgvParts.AllowUserToResizeRows = false;
            this.dgvParts.RowHeadersVisible = false;
            this.dgvParts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvParts.MultiSelect = false;
            this.dgvParts.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvParts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvParts.ReadOnly = false;

            var colSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colSelect.Name = "colSelect";
            colSelect.HeaderText = "✓";
            colSelect.Width = 35;
            colSelect.FillWeight = 10;
            colSelect.ReadOnly = false;

            var colFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colFileName.Name = "colFileName";
            colFileName.HeaderText = "File Name";
            colFileName.FillWeight = 50;
            colFileName.ReadOnly = true;

            var colThickness = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colThickness.Name = "colThickness";
            colThickness.HeaderText = "Thickness [mm]";
            colThickness.FillWeight = 20;
            colThickness.ReadOnly = true;

            var colMaterial = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaterial.Name = "colMaterial";
            colMaterial.HeaderText = "Material";
            colMaterial.FillWeight = 20;
            colMaterial.ReadOnly = true;

            this.dgvParts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                colSelect, colFileName, colThickness, colMaterial
            });

            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnSelectAll.Text = "Select All";
            this.btnSelectAll.Location = new System.Drawing.Point(10, 224);
            this.btnSelectAll.Size = new System.Drawing.Size(130, 28);
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);

            this.btnDeselectAll = new System.Windows.Forms.Button();
            this.btnDeselectAll.Text = "Deselect All";
            this.btnDeselectAll.Location = new System.Drawing.Point(148, 224);
            this.btnDeselectAll.Size = new System.Drawing.Size(130, 28);
            this.btnDeselectAll.Click += new System.EventHandler(this.btnDeselectAll_Click);

            this.grpPartsList.Controls.Add(this.dgvParts);
            this.grpPartsList.Controls.Add(this.btnSelectAll);
            this.grpPartsList.Controls.Add(this.btnDeselectAll);

            // ===================================================
            // Panel B – Folder docelowy
            // ===================================================
            this.grpOutputFolder = new System.Windows.Forms.GroupBox();
            this.grpOutputFolder.Text = "Output Folder";
            this.grpOutputFolder.Location = new System.Drawing.Point(12, 339);
            this.grpOutputFolder.Size = new System.Drawing.Size(710, 55);

            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.txtOutputFolder.Location = new System.Drawing.Point(10, 22);
            this.txtOutputFolder.Size = new System.Drawing.Size(590, 23);
            this.txtOutputFolder.ReadOnly = true;

            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Location = new System.Drawing.Point(608, 21);
            this.btnBrowse.Size = new System.Drawing.Size(92, 25);
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);

            this.grpOutputFolder.Controls.Add(this.txtOutputFolder);
            this.grpOutputFolder.Controls.Add(this.btnBrowse);

            // ===================================================
            // Panel C – Format eksportu
            // ===================================================
            this.grpFormat = new System.Windows.Forms.GroupBox();
            this.grpFormat.Text = "Export Format";
            this.grpFormat.Location = new System.Drawing.Point(12, 400);
            this.grpFormat.Size = new System.Drawing.Size(438, 55);

            this.rbDXF = new System.Windows.Forms.RadioButton();
            this.rbDXF.Text = "DXF";
            this.rbDXF.Location = new System.Drawing.Point(15, 22);
            this.rbDXF.Size = new System.Drawing.Size(60, 24);
            this.rbDXF.Checked = true;
            this.rbDXF.CheckedChanged += new System.EventHandler(this.NamingChanged);

            this.rbDWG = new System.Windows.Forms.RadioButton();
            this.rbDWG.Text = "DWG";
            this.rbDWG.Location = new System.Drawing.Point(85, 22);
            this.rbDWG.Size = new System.Drawing.Size(60, 24);
            this.rbDWG.CheckedChanged += new System.EventHandler(this.NamingChanged);

            this.rbPDF = new System.Windows.Forms.RadioButton();
            this.rbPDF.Text = "PDF (flat pattern view)";
            this.rbPDF.Location = new System.Drawing.Point(155, 22);
            this.rbPDF.Size = new System.Drawing.Size(180, 24);
            this.rbPDF.CheckedChanged += new System.EventHandler(this.NamingChanged);

            this.grpFormat.Controls.Add(this.rbDXF);
            this.grpFormat.Controls.Add(this.rbDWG);
            this.grpFormat.Controls.Add(this.rbPDF);

            // ===================================================
            // Panel D – Linie gięć
            // ===================================================
            this.grpBendLines = new System.Windows.Forms.GroupBox();
            this.grpBendLines.Text = "Flat Pattern Content";
            this.grpBendLines.Location = new System.Drawing.Point(462, 400);
            this.grpBendLines.Size = new System.Drawing.Size(260, 55);

            this.chkBendLines = new System.Windows.Forms.CheckBox();
            this.chkBendLines.Text = "Show Bend Lines";
            this.chkBendLines.Location = new System.Drawing.Point(15, 22);
            this.chkBendLines.Size = new System.Drawing.Size(200, 24);
            this.chkBendLines.Checked = true;

            this.grpBendLines.Controls.Add(this.chkBendLines);

            // ===================================================
            // Panel E – Schemat nazewnictwa
            // ===================================================
            this.grpNaming = new System.Windows.Forms.GroupBox();
            this.grpNaming.Text = "File Naming Scheme";
            this.grpNaming.Location = new System.Drawing.Point(12, 461);
            this.grpNaming.Size = new System.Drawing.Size(710, 110);

            this.lblCustomPrefix = new System.Windows.Forms.Label();
            this.lblCustomPrefix.Text = "Custom Prefix:";
            this.lblCustomPrefix.Location = new System.Drawing.Point(15, 25);
            this.lblCustomPrefix.Size = new System.Drawing.Size(90, 20);

            this.txtCustomPrefix = new System.Windows.Forms.TextBox();
            this.txtCustomPrefix.Location = new System.Drawing.Point(110, 22);
            this.txtCustomPrefix.Size = new System.Drawing.Size(200, 23);
            this.txtCustomPrefix.TextChanged += new System.EventHandler(this.NamingChanged);

            this.chkAddThickness = new System.Windows.Forms.CheckBox();
            this.chkAddThickness.Text = "Add Thickness";
            this.chkAddThickness.Location = new System.Drawing.Point(340, 22);
            this.chkAddThickness.Size = new System.Drawing.Size(120, 24);
            this.chkAddThickness.Checked = true;
            this.chkAddThickness.CheckedChanged += new System.EventHandler(this.NamingChanged);

            this.chkAddMaterial = new System.Windows.Forms.CheckBox();
            this.chkAddMaterial.Text = "Add Material";
            this.chkAddMaterial.Location = new System.Drawing.Point(470, 22);
            this.chkAddMaterial.Size = new System.Drawing.Size(120, 24);
            this.chkAddMaterial.Checked = true;
            this.chkAddMaterial.CheckedChanged += new System.EventHandler(this.NamingChanged);

            this.lblPreviewCaption = new System.Windows.Forms.Label();
            this.lblPreviewCaption.Text = "Preview:";
            this.lblPreviewCaption.Location = new System.Drawing.Point(12, 65);
            this.lblPreviewCaption.Size = new System.Drawing.Size(60, 20);

            this.lblFileNamePreview = new System.Windows.Forms.Label();
            this.lblFileNamePreview.Text = "2.0mm_DC01_Bracket_01.dxf";
            this.lblFileNamePreview.Location = new System.Drawing.Point(75, 65);
            this.lblFileNamePreview.Size = new System.Drawing.Size(620, 20);
            this.lblFileNamePreview.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblFileNamePreview.ForeColor = System.Drawing.Color.DarkBlue;

            this.grpNaming.Controls.Add(this.lblCustomPrefix);
            this.grpNaming.Controls.Add(this.txtCustomPrefix);

            this.grpNaming.Controls.Add(this.chkAddThickness);
            this.grpNaming.Controls.Add(this.chkAddMaterial);
            this.grpNaming.Controls.Add(this.lblPreviewCaption);
            this.grpNaming.Controls.Add(this.lblFileNamePreview);

            // ===================================================
            // Panel F – Przyciski akcji + ProgressBar
            // ===================================================
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.grpActions.Text = "Export";
            this.grpActions.Location = new System.Drawing.Point(12, 577);
            this.grpActions.Size = new System.Drawing.Size(710, 145);

            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressBar.Location = new System.Drawing.Point(10, 25);
            this.progressBar.Size = new System.Drawing.Size(690, 25);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatus.Text = "Select an .asm file and click 'Scan'";
            this.lblStatus.Location = new System.Drawing.Point(10, 56);
            this.lblStatus.Size = new System.Drawing.Size(690, 22);
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;

            this.btnExport = new System.Windows.Forms.Button();
            this.btnExport.Text = "Export";
            this.btnExport.Location = new System.Drawing.Point(480, 85);
            this.btnExport.Size = new System.Drawing.Size(105, 35);
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Enabled = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCancel.Text = "Close";
            this.btnCancel.Location = new System.Drawing.Point(595, 85);
            this.btnCancel.Size = new System.Drawing.Size(105, 35);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.grpActions.Controls.Add(this.progressBar);
            this.grpActions.Controls.Add(this.lblStatus);
            this.grpActions.Controls.Add(this.btnExport);
            this.grpActions.Controls.Add(this.btnCancel);

            // === Dodaj panele do formularza ===
            this.Controls.Add(this.grpAssembly);
            this.Controls.Add(this.grpPartsList);
            this.Controls.Add(this.grpOutputFolder);
            this.Controls.Add(this.grpFormat);
            this.Controls.Add(this.grpBendLines);
            this.Controls.Add(this.grpNaming);
            this.Controls.Add(this.grpActions);
        }

        #endregion

        // Panel 0 – Złożenie
        private System.Windows.Forms.GroupBox grpAssembly;
        private System.Windows.Forms.TextBox txtAsmPath;
        private System.Windows.Forms.Button btnBrowseAsm;
        private System.Windows.Forms.Button btnScanAsm;

        // Panel A
        private System.Windows.Forms.GroupBox grpPartsList;
        private System.Windows.Forms.DataGridView dgvParts;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnDeselectAll;

        // Panel B
        private System.Windows.Forms.GroupBox grpOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnBrowse;

        // Panel C
        private System.Windows.Forms.GroupBox grpFormat;
        private System.Windows.Forms.RadioButton rbDXF;
        private System.Windows.Forms.RadioButton rbDWG;
        private System.Windows.Forms.RadioButton rbPDF;

        // Panel D
        private System.Windows.Forms.GroupBox grpBendLines;
        private System.Windows.Forms.CheckBox chkBendLines;

        // Panel E
        private System.Windows.Forms.GroupBox grpNaming;
        private System.Windows.Forms.CheckBox chkAddThickness;
        private System.Windows.Forms.CheckBox chkAddMaterial;
        private System.Windows.Forms.Label lblCustomPrefix;
        private System.Windows.Forms.TextBox txtCustomPrefix;
        private System.Windows.Forms.Label lblPreviewCaption;
        private System.Windows.Forms.Label lblFileNamePreview;

        // Panel F
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCancel;
    }
}
