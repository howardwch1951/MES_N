namespace MES_N
{
    partial class FormMain
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvHistoryLog = new System.Windows.Forms.DataGridView();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tableLayoutPanel11 = new System.Windows.Forms.TableLayoutPanel();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.HistLogSearch_btn = new System.Windows.Forms.Button();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvCurrentLog = new System.Windows.Forms.DataGridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.SID_txt = new System.Windows.Forms.TextBox();
            this.SID_lbl = new System.Windows.Forms.Label();
            this.CurrLogSearch_btn = new System.Windows.Forms.Button();
            this.Note_lbl = new System.Windows.Forms.Label();
            this.Note_txt = new System.Windows.Forms.TextBox();
            this.Static_txt = new System.Windows.Forms.TextBox();
            this.Static_lbl = new System.Windows.Forms.Label();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.TID_lbl = new System.Windows.Forms.Label();
            this.Sclass_cmb = new System.Windows.Forms.ComboBox();
            this.Dline_cmb = new System.Windows.Forms.ComboBox();
            this.Dline_lbl = new System.Windows.Forms.Label();
            this.TID_txt = new System.Windows.Forms.TextBox();
            this.DIP_lbl = new System.Windows.Forms.Label();
            this.DIP_txt = new System.Windows.Forms.TextBox();
            this.Sclass_lbl = new System.Windows.Forms.Label();
            this.dataGridView_Result = new System.Windows.Forms.DataGridView();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.dgvMainView = new System.Windows.Forms.DataGridView();
            this.label2 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tmrUpdateDT = new System.Windows.Forms.Timer(this.components);
            this.tmrCurrentLog = new System.Windows.Forms.Timer(this.components);
            this.tabPage6.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistoryLog)).BeginInit();
            this.tableLayoutPanel11.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCurrentLog)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_Result)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMainView)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.tableLayoutPanel9);
            this.tabPage6.Location = new System.Drawing.Point(4, 25);
            this.tabPage6.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Size = new System.Drawing.Size(1704, 823);
            this.tabPage6.TabIndex = 3;
            this.tabPage6.Text = "歷史警報";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 1;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.dgvHistoryLog, 0, 3);
            this.tableLayoutPanel9.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel9.Controls.Add(this.tableLayoutPanel11, 0, 1);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel9.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 4;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 86F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(1704, 823);
            this.tableLayoutPanel9.TabIndex = 1;
            // 
            // dgvHistoryLog
            // 
            this.dgvHistoryLog.AllowUserToAddRows = false;
            this.dgvHistoryLog.AllowUserToDeleteRows = false;
            this.dgvHistoryLog.AllowUserToResizeColumns = false;
            this.dgvHistoryLog.AllowUserToResizeRows = false;
            this.dgvHistoryLog.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvHistoryLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle11.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvHistoryLog.DefaultCellStyle = dataGridViewCellStyle11;
            this.dgvHistoryLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistoryLog.Location = new System.Drawing.Point(4, 117);
            this.dgvHistoryLog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvHistoryLog.Name = "dgvHistoryLog";
            this.dgvHistoryLog.ReadOnly = true;
            this.dgvHistoryLog.RowHeadersVisible = false;
            this.dgvHistoryLog.RowHeadersWidth = 82;
            this.dgvHistoryLog.RowTemplate.Height = 24;
            this.dgvHistoryLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistoryLog.Size = new System.Drawing.Size(1696, 702);
            this.dgvHistoryLog.TabIndex = 112;
            this.dgvHistoryLog.TabStop = false;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 8);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(1696, 15);
            this.label3.TabIndex = 109;
            this.label3.Text = "搜尋";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 89);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(1696, 15);
            this.label4.TabIndex = 109;
            this.label4.Text = "搜尋結果";
            // 
            // tableLayoutPanel11
            // 
            this.tableLayoutPanel11.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel11.ColumnCount = 6;
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel11.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel11.Controls.Add(this.dtpEnd, 3, 0);
            this.tableLayoutPanel11.Controls.Add(this.label12, 0, 0);
            this.tableLayoutPanel11.Controls.Add(this.label13, 2, 0);
            this.tableLayoutPanel11.Controls.Add(this.HistLogSearch_btn, 4, 0);
            this.tableLayoutPanel11.Controls.Add(this.dtpStart, 1, 0);
            this.tableLayoutPanel11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel11.Location = new System.Drawing.Point(4, 36);
            this.tableLayoutPanel11.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel11.Name = "tableLayoutPanel11";
            this.tableLayoutPanel11.RowCount = 1;
            this.tableLayoutPanel11.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel11.Size = new System.Drawing.Size(1696, 41);
            this.tableLayoutPanel11.TabIndex = 111;
            // 
            // dtpEnd
            // 
            this.dtpEnd.Location = new System.Drawing.Point(681, 5);
            this.dtpEnd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(260, 25);
            this.dtpEnd.TabIndex = 7;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(5, 13);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(160, 15);
            this.label12.TabIndex = 0;
            this.label12.Text = "開始日期：";
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(512, 13);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(160, 15);
            this.label13.TabIndex = 1;
            this.label13.Text = "結束日期：";
            // 
            // HistLogSearch_btn
            // 
            this.HistLogSearch_btn.Location = new System.Drawing.Point(1019, 5);
            this.HistLogSearch_btn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.HistLogSearch_btn.Name = "HistLogSearch_btn";
            this.HistLogSearch_btn.Size = new System.Drawing.Size(84, 28);
            this.HistLogSearch_btn.TabIndex = 5;
            this.HistLogSearch_btn.Text = "Search";
            this.HistLogSearch_btn.UseVisualStyleBackColor = true;
            this.HistLogSearch_btn.Click += new System.EventHandler(this.HistLogSearch_btn_Click);
            // 
            // dtpStart
            // 
            this.dtpStart.Location = new System.Drawing.Point(174, 5);
            this.dtpStart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(260, 25);
            this.dtpStart.TabIndex = 6;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.tableLayoutPanel8);
            this.tabPage5.Location = new System.Drawing.Point(4, 25);
            this.tabPage5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(1704, 823);
            this.tabPage5.TabIndex = 2;
            this.tabPage5.Text = "即時警報";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.ColumnCount = 1;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel8.Controls.Add(this.dgvCurrentLog, 0, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(1704, 823);
            this.tableLayoutPanel8.TabIndex = 0;
            // 
            // dgvCurrentLog
            // 
            this.dgvCurrentLog.AllowUserToAddRows = false;
            this.dgvCurrentLog.AllowUserToDeleteRows = false;
            this.dgvCurrentLog.AllowUserToResizeColumns = false;
            this.dgvCurrentLog.AllowUserToResizeRows = false;
            this.dgvCurrentLog.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvCurrentLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle12.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvCurrentLog.DefaultCellStyle = dataGridViewCellStyle12;
            this.dgvCurrentLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCurrentLog.Location = new System.Drawing.Point(4, 4);
            this.dgvCurrentLog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvCurrentLog.Name = "dgvCurrentLog";
            this.dgvCurrentLog.ReadOnly = true;
            this.dgvCurrentLog.RowHeadersVisible = false;
            this.dgvCurrentLog.RowHeadersWidth = 82;
            this.dgvCurrentLog.RowTemplate.Height = 24;
            this.dgvCurrentLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCurrentLog.Size = new System.Drawing.Size(1696, 815);
            this.dgvCurrentLog.TabIndex = 5;
            this.dgvCurrentLog.TabStop = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.tableLayoutPanel1);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Size = new System.Drawing.Size(1704, 823);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Tools";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1696, 815);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.textBox2, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(1275, 5);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 21F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 71F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(416, 805);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.Location = new System.Drawing.Point(4, 237);
            this.textBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(408, 564);
            this.textBox2.TabIndex = 101;
            this.textBox2.TabStop = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 8);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(408, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "補償設定";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel3.Controls.Add(this.numericUpDown1, 1, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(4, 36);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(408, 161);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.numericUpDown1.DecimalPlaces = 3;
            this.numericUpDown1.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericUpDown1.Location = new System.Drawing.Point(168, 8);
            this.numericUpDown1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(235, 25);
            this.numericUpDown1.TabIndex = 100;
            this.numericUpDown1.TabStop = false;
            this.numericUpDown1.Value = new decimal(new int[] {
            45,
            0,
            0,
            196608});
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(4, 209);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(408, 15);
            this.label11.TabIndex = 1;
            this.label11.Text = "System";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.label10, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel5, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel6, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.dataGridView_Result, 0, 4);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(5, 5);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 5;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(1261, 805);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(4, 136);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(1253, 15);
            this.label10.TabIndex = 3;
            this.label10.Text = "搜尋結果";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 8);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(1253, 15);
            this.label9.TabIndex = 2;
            this.label9.Text = "搜尋";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel5.ColumnCount = 7;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11F));
            this.tableLayoutPanel5.Controls.Add(this.SID_txt, 3, 0);
            this.tableLayoutPanel5.Controls.Add(this.SID_lbl, 2, 0);
            this.tableLayoutPanel5.Controls.Add(this.CurrLogSearch_btn, 6, 0);
            this.tableLayoutPanel5.Controls.Add(this.Note_lbl, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.Note_txt, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.Static_txt, 5, 0);
            this.tableLayoutPanel5.Controls.Add(this.Static_lbl, 4, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(4, 84);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(1253, 40);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // SID_txt
            // 
            this.SID_txt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.SID_txt.Location = new System.Drawing.Point(517, 7);
            this.SID_txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.SID_txt.Name = "SID_txt";
            this.SID_txt.Size = new System.Drawing.Size(178, 25);
            this.SID_txt.TabIndex = 6;
            // 
            // SID_lbl
            // 
            this.SID_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.SID_lbl.AutoSize = true;
            this.SID_lbl.Location = new System.Drawing.Point(417, 12);
            this.SID_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SID_lbl.Name = "SID_lbl";
            this.SID_lbl.Size = new System.Drawing.Size(91, 15);
            this.SID_lbl.TabIndex = 10;
            this.SID_lbl.Text = "SID：";
            // 
            // CurrLogSearch_btn
            // 
            this.CurrLogSearch_btn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.CurrLogSearch_btn.Location = new System.Drawing.Point(1116, 6);
            this.CurrLogSearch_btn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CurrLogSearch_btn.Name = "CurrLogSearch_btn";
            this.CurrLogSearch_btn.Size = new System.Drawing.Size(132, 28);
            this.CurrLogSearch_btn.TabIndex = 8;
            this.CurrLogSearch_btn.Text = "Search";
            this.CurrLogSearch_btn.UseVisualStyleBackColor = true;
            this.CurrLogSearch_btn.Click += new System.EventHandler(this.CurrLogSearch_btn_Click);
            // 
            // Note_lbl
            // 
            this.Note_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Note_lbl.AutoSize = true;
            this.Note_lbl.Location = new System.Drawing.Point(5, 12);
            this.Note_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Note_lbl.Name = "Note_lbl";
            this.Note_lbl.Size = new System.Drawing.Size(91, 15);
            this.Note_lbl.TabIndex = 5;
            this.Note_lbl.Text = "Note：";
            // 
            // Note_txt
            // 
            this.Note_txt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Note_txt.Location = new System.Drawing.Point(105, 7);
            this.Note_txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Note_txt.Name = "Note_txt";
            this.Note_txt.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Note_txt.Size = new System.Drawing.Size(303, 25);
            this.Note_txt.TabIndex = 5;
            // 
            // Static_txt
            // 
            this.Static_txt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Static_txt.Location = new System.Drawing.Point(804, 7);
            this.Static_txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Static_txt.Name = "Static_txt";
            this.Static_txt.Size = new System.Drawing.Size(303, 25);
            this.Static_txt.TabIndex = 7;
            // 
            // Static_lbl
            // 
            this.Static_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Static_lbl.AutoSize = true;
            this.Static_lbl.Location = new System.Drawing.Point(704, 12);
            this.Static_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Static_lbl.Name = "Static_lbl";
            this.Static_lbl.Size = new System.Drawing.Size(91, 15);
            this.Static_lbl.TabIndex = 9;
            this.Static_lbl.Text = "Static：";
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel6.ColumnCount = 8;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel6.Controls.Add(this.TID_lbl, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.Sclass_cmb, 7, 0);
            this.tableLayoutPanel6.Controls.Add(this.Dline_cmb, 3, 0);
            this.tableLayoutPanel6.Controls.Add(this.Dline_lbl, 2, 0);
            this.tableLayoutPanel6.Controls.Add(this.TID_txt, 1, 0);
            this.tableLayoutPanel6.Controls.Add(this.DIP_lbl, 4, 0);
            this.tableLayoutPanel6.Controls.Add(this.DIP_txt, 5, 0);
            this.tableLayoutPanel6.Controls.Add(this.Sclass_lbl, 6, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(4, 36);
            this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(1253, 40);
            this.tableLayoutPanel6.TabIndex = 1;
            // 
            // TID_lbl
            // 
            this.TID_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.TID_lbl.AutoSize = true;
            this.TID_lbl.Location = new System.Drawing.Point(5, 12);
            this.TID_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.TID_lbl.Name = "TID_lbl";
            this.TID_lbl.Size = new System.Drawing.Size(91, 15);
            this.TID_lbl.TabIndex = 0;
            this.TID_lbl.Text = "TID：";
            // 
            // Sclass_cmb
            // 
            this.Sclass_cmb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Sclass_cmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Sclass_cmb.FormattingEnabled = true;
            this.Sclass_cmb.Location = new System.Drawing.Point(1066, 8);
            this.Sclass_cmb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Sclass_cmb.Name = "Sclass_cmb";
            this.Sclass_cmb.Size = new System.Drawing.Size(182, 23);
            this.Sclass_cmb.TabIndex = 4;
            // 
            // Dline_cmb
            // 
            this.Dline_cmb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Dline_cmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Dline_cmb.FormattingEnabled = true;
            this.Dline_cmb.Location = new System.Drawing.Point(392, 8);
            this.Dline_cmb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Dline_cmb.Name = "Dline_cmb";
            this.Dline_cmb.Size = new System.Drawing.Size(178, 23);
            this.Dline_cmb.TabIndex = 2;
            // 
            // Dline_lbl
            // 
            this.Dline_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Dline_lbl.AutoSize = true;
            this.Dline_lbl.Location = new System.Drawing.Point(292, 12);
            this.Dline_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Dline_lbl.Name = "Dline_lbl";
            this.Dline_lbl.Size = new System.Drawing.Size(91, 15);
            this.Dline_lbl.TabIndex = 1;
            this.Dline_lbl.Text = "Dline：";
            // 
            // TID_txt
            // 
            this.TID_txt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.TID_txt.Location = new System.Drawing.Point(105, 7);
            this.TID_txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.TID_txt.Name = "TID_txt";
            this.TID_txt.Size = new System.Drawing.Size(178, 25);
            this.TID_txt.TabIndex = 1;
            // 
            // DIP_lbl
            // 
            this.DIP_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.DIP_lbl.AutoSize = true;
            this.DIP_lbl.Location = new System.Drawing.Point(579, 12);
            this.DIP_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DIP_lbl.Name = "DIP_lbl";
            this.DIP_lbl.Size = new System.Drawing.Size(91, 15);
            this.DIP_lbl.TabIndex = 1;
            this.DIP_lbl.Text = "DIP：";
            // 
            // DIP_txt
            // 
            this.DIP_txt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.DIP_txt.Location = new System.Drawing.Point(679, 7);
            this.DIP_txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.DIP_txt.Name = "DIP_txt";
            this.DIP_txt.Size = new System.Drawing.Size(278, 25);
            this.DIP_txt.TabIndex = 3;
            // 
            // Sclass_lbl
            // 
            this.Sclass_lbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Sclass_lbl.AutoSize = true;
            this.Sclass_lbl.Location = new System.Drawing.Point(966, 12);
            this.Sclass_lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Sclass_lbl.Name = "Sclass_lbl";
            this.Sclass_lbl.Size = new System.Drawing.Size(91, 15);
            this.Sclass_lbl.TabIndex = 4;
            this.Sclass_lbl.Text = "Sclass：";
            // 
            // dataGridView_Result
            // 
            this.dataGridView_Result.AllowUserToAddRows = false;
            this.dataGridView_Result.AllowUserToDeleteRows = false;
            this.dataGridView_Result.AllowUserToResizeColumns = false;
            this.dataGridView_Result.AllowUserToResizeRows = false;
            this.dataGridView_Result.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dataGridView_Result.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle13.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle13.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle13.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView_Result.DefaultCellStyle = dataGridViewCellStyle13;
            this.dataGridView_Result.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView_Result.Location = new System.Drawing.Point(4, 164);
            this.dataGridView_Result.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dataGridView_Result.Name = "dataGridView_Result";
            this.dataGridView_Result.ReadOnly = true;
            this.dataGridView_Result.RowHeadersVisible = false;
            this.dataGridView_Result.RowHeadersWidth = 82;
            this.dataGridView_Result.RowTemplate.Height = 24;
            this.dataGridView_Result.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView_Result.Size = new System.Drawing.Size(1253, 637);
            this.dataGridView_Result.TabIndex = 4;
            this.dataGridView_Result.TabStop = false;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tableLayoutPanel7);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Size = new System.Drawing.Size(1704, 823);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Server List";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel7.ColumnCount = 1;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel7.Controls.Add(this.dgvMainView, 0, 1);
            this.tableLayoutPanel7.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 2;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 96F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(1696, 815);
            this.tableLayoutPanel7.TabIndex = 0;
            // 
            // dgvMainView
            // 
            this.dgvMainView.AllowUserToAddRows = false;
            this.dgvMainView.AllowUserToDeleteRows = false;
            this.dgvMainView.AllowUserToResizeColumns = false;
            this.dgvMainView.AllowUserToResizeRows = false;
            this.dgvMainView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dgvMainView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle14.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle14.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle14.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvMainView.DefaultCellStyle = dataGridViewCellStyle14;
            this.dgvMainView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMainView.Location = new System.Drawing.Point(5, 38);
            this.dgvMainView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvMainView.Name = "dgvMainView";
            this.dgvMainView.ReadOnly = true;
            this.dgvMainView.RowHeadersVisible = false;
            this.dgvMainView.RowHeadersWidth = 82;
            dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvMainView.RowsDefaultCellStyle = dataGridViewCellStyle15;
            this.dgvMainView.RowTemplate.Height = 24;
            this.dgvMainView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMainView.Size = new System.Drawing.Size(1686, 772);
            this.dgvMainView.TabIndex = 11;
            this.dgvMainView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGridView_Threads_DataError);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 9);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(1686, 15);
            this.label2.TabIndex = 10;
            this.label2.Text = "MES";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Controls.Add(this.tabPage6);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1712, 852);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.TabStop = false;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tmrUpdateDT
            // 
            this.tmrUpdateDT.Enabled = true;
            this.tmrUpdateDT.Interval = 1000;
            this.tmrUpdateDT.Tick += new System.EventHandler(this.tmrUpdateDT_Tick);
            // 
            // tmrCurrentLog
            // 
            this.tmrCurrentLog.Tick += new System.EventHandler(this.SetCurrentLog);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1712, 852);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.Text = "MES HW SERVER";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabPage6.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistoryLog)).EndInit();
            this.tableLayoutPanel11.ResumeLayout(false);
            this.tableLayoutPanel11.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCurrentLog)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_Result)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMainView)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.DataGridView dgvHistoryLog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel11;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button HistLogSearch_btn;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.DataGridView dgvCurrentLog;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TextBox SID_txt;
        private System.Windows.Forms.Label SID_lbl;
        private System.Windows.Forms.Button CurrLogSearch_btn;
        private System.Windows.Forms.Label Note_lbl;
        private System.Windows.Forms.TextBox Note_txt;
        private System.Windows.Forms.TextBox Static_txt;
        private System.Windows.Forms.Label Static_lbl;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.Label TID_lbl;
        private System.Windows.Forms.ComboBox Sclass_cmb;
        private System.Windows.Forms.ComboBox Dline_cmb;
        private System.Windows.Forms.Label Dline_lbl;
        private System.Windows.Forms.TextBox TID_txt;
        private System.Windows.Forms.Label DIP_lbl;
        private System.Windows.Forms.TextBox DIP_txt;
        private System.Windows.Forms.Label Sclass_lbl;
        private System.Windows.Forms.DataGridView dataGridView_Result;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.DataGridView dgvMainView;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.Timer tmrUpdateDT;
        private System.Windows.Forms.Timer tmrCurrentLog;
    }
}

