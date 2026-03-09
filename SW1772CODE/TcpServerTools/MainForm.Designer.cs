namespace SampleTcpServer
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnStartServer = new System.Windows.Forms.Button();
            this.btnStopServer = new System.Windows.Forms.Button();
            this.ListView = new System.Windows.Forms.ListView();
            this.ClientID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.State = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SendCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SendByte = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ReciveCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ReciveByte = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Status = new System.Windows.Forms.StatusStrip();
            this.ConnectCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.PreSecSendBytes = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.PreSecReceBytes = new System.Windows.Forms.ToolStripStatusLabel();
            this.ServerState = new System.Windows.Forms.ToolStripStatusLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.ServerPort = new System.Windows.Forms.TextBox();
            this.FlowTimer = new System.Windows.Forms.Timer(this.components);
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.ListView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnSendMessage = new System.Windows.Forms.Button();
            this.btnSingleScan = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.CHK_LOG = new System.Windows.Forms.CheckBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.Status.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartServer
            // 
            this.btnStartServer.Location = new System.Drawing.Point(219, 6);
            this.btnStartServer.Name = "btnStartServer";
            this.btnStartServer.Size = new System.Drawing.Size(75, 25);
            this.btnStartServer.TabIndex = 0;
            this.btnStartServer.Text = "启动";
            this.btnStartServer.UseVisualStyleBackColor = true;
            this.btnStartServer.Click += new System.EventHandler(this.btnStartServer_Click);
            // 
            // btnStopServer
            // 
            this.btnStopServer.Location = new System.Drawing.Point(323, 6);
            this.btnStopServer.Name = "btnStopServer";
            this.btnStopServer.Size = new System.Drawing.Size(75, 25);
            this.btnStopServer.TabIndex = 1;
            this.btnStopServer.Text = "停止";
            this.btnStopServer.UseVisualStyleBackColor = true;
            this.btnStopServer.Click += new System.EventHandler(this.btnStopServer_Click);
            // 
            // ListView
            // 
            this.ListView.BackColor = System.Drawing.Color.LightCyan;
            this.ListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ClientID,
            this.State,
            this.SendCount,
            this.SendByte,
            this.ReciveCount,
            this.ReciveByte});
            this.ListView.GridLines = true;
            this.ListView.Location = new System.Drawing.Point(6, 6);
            this.ListView.Name = "ListView";
            this.ListView.Size = new System.Drawing.Size(931, 440);
            this.ListView.TabIndex = 3;
            this.ListView.UseCompatibleStateImageBehavior = false;
            this.ListView.View = System.Windows.Forms.View.Details;
            // 
            // ClientID
            // 
            this.ClientID.Text = "ClientID";
            this.ClientID.Width = 108;
            // 
            // State
            // 
            this.State.Text = "State";
            this.State.Width = 115;
            // 
            // SendCount
            // 
            this.SendCount.Text = "SendCount";
            this.SendCount.Width = 139;
            // 
            // SendByte
            // 
            this.SendByte.Text = "SendByte";
            this.SendByte.Width = 141;
            // 
            // ReciveCount
            // 
            this.ReciveCount.Text = "ReciveCount";
            this.ReciveCount.Width = 99;
            // 
            // ReciveByte
            // 
            this.ReciveByte.Text = "ReciveByte";
            this.ReciveByte.Width = 148;
            // 
            // Status
            // 
            this.Status.BackColor = System.Drawing.Color.Bisque;
            this.Status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConnectCount,
            this.toolStripStatusLabel1,
            this.PreSecSendBytes,
            this.toolStripStatusLabel2,
            this.PreSecReceBytes,
            this.ServerState});
            this.Status.Location = new System.Drawing.Point(0, 526);
            this.Status.Name = "Status";
            this.Status.Size = new System.Drawing.Size(948, 25);
            this.Status.TabIndex = 4;
            // 
            // ConnectCount
            // 
            this.ConnectCount.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.ConnectCount.Name = "ConnectCount";
            this.ConnectCount.Size = new System.Drawing.Size(4, 20);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(833, 20);
            this.toolStripStatusLabel1.Spring = true;
            // 
            // PreSecSendBytes
            // 
            this.PreSecSendBytes.Name = "PreSecSendBytes";
            this.PreSecSendBytes.Size = new System.Drawing.Size(30, 20);
            this.PreSecSendBytes.Text = "0B/s";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Image = global::SampleTcpServer.Properties.Resources.communication;
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(16, 20);
            // 
            // PreSecReceBytes
            // 
            this.PreSecReceBytes.Name = "PreSecReceBytes";
            this.PreSecReceBytes.Size = new System.Drawing.Size(30, 20);
            this.PreSecReceBytes.Text = "0B/s";
            // 
            // ServerState
            // 
            this.ServerState.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ServerState.Image = global::SampleTcpServer.Properties.Resources.Close;
            this.ServerState.Name = "ServerState";
            this.ServerState.Size = new System.Drawing.Size(20, 20);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "端口:";
            // 
            // ServerPort
            // 
            this.ServerPort.Location = new System.Drawing.Point(54, 9);
            this.ServerPort.Name = "ServerPort";
            this.ServerPort.Size = new System.Drawing.Size(45, 20);
            this.ServerPort.TabIndex = 6;
            this.ServerPort.Text = "9988";
            // 
            // FlowTimer
            // 
            this.FlowTimer.Interval = 1000;
            this.FlowTimer.Tick += new System.EventHandler(this.FlowTimer_Tick);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(6, 3);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(931, 443);
            this.richTextBox1.TabIndex = 7;
            this.richTextBox1.Text = "";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(0, 39);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(948, 478);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.ListView);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(940, 452);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Client info";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.richTextBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(940, 452);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "log info";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.ListView1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(940, 452);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "scan info";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // ListView1
            // 
            this.ListView1.BackColor = System.Drawing.Color.LightCyan;
            this.ListView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9,
            this.columnHeader10,
            this.columnHeader11,
            this.columnHeader12});
            this.ListView1.GridLines = true;
            this.ListView1.Location = new System.Drawing.Point(0, 0);
            this.ListView1.Name = "ListView1";
            this.ListView1.Size = new System.Drawing.Size(934, 449);
            this.ListView1.TabIndex = 3;
            this.ListView1.UseCompatibleStateImageBehavior = false;
            this.ListView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "SocketID";
            this.columnHeader1.Width = 79;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "SocketGUID";
            this.columnHeader2.Width = 86;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "StartWV";
            this.columnHeader3.Width = 61;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "StopWV";
            this.columnHeader4.Width = 62;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Step";
            this.columnHeader5.Width = 53;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "TLSPower";
            this.columnHeader6.Width = 66;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "PWMPower";
            this.columnHeader7.Width = 70;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "DoPDL";
            this.columnHeader8.Width = 55;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "NumberOfScan";
            this.columnHeader9.Width = 93;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "IndexOfChannels";
            this.columnHeader10.Width = 84;
            // 
            // columnHeader11
            // 
            this.columnHeader11.Text = "EndPoint";
            this.columnHeader11.Width = 104;
            // 
            // columnHeader12
            // 
            this.columnHeader12.Text = "Decrement";
            this.columnHeader12.Width = 92;
            // 
            // btnSendMessage
            // 
            this.btnSendMessage.Location = new System.Drawing.Point(626, 6);
            this.btnSendMessage.Name = "btnSendMessage";
            this.btnSendMessage.Size = new System.Drawing.Size(75, 25);
            this.btnSendMessage.TabIndex = 9;
            this.btnSendMessage.Text = "发送消息";
            this.btnSendMessage.UseVisualStyleBackColor = true;
            this.btnSendMessage.Click += new System.EventHandler(this.btnSendMessage_Click);
            // 
            // btnSingleScan
            // 
            this.btnSingleScan.Location = new System.Drawing.Point(730, 6);
            this.btnSingleScan.Name = "btnSingleScan";
            this.btnSingleScan.Size = new System.Drawing.Size(75, 25);
            this.btnSingleScan.TabIndex = 10;
            this.btnSingleScan.Text = "单次扫描";
            this.btnSingleScan.UseVisualStyleBackColor = true;
            this.btnSingleScan.Click += new System.EventHandler(this.btnSingleScan_Click);
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(522, 6);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 25);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "清空LOG";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // CHK_LOG
            // 
            this.CHK_LOG.AutoSize = true;
            this.CHK_LOG.Location = new System.Drawing.Point(436, 11);
            this.CHK_LOG.Name = "CHK_LOG";
            this.CHK_LOG.Size = new System.Drawing.Size(66, 17);
            this.CHK_LOG.TabIndex = 11;
            this.CHK_LOG.Text = "chkLOG";
            this.CHK_LOG.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(832, 14);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(33, 20);
            this.textBox1.TabIndex = 12;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.ClientSize = new System.Drawing.Size(948, 551);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.CHK_LOG);
            this.Controls.Add(this.btnSingleScan);
            this.Controls.Add(this.btnSendMessage);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.ServerPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Status);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.btnStopServer);
            this.Controls.Add(this.btnStartServer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "SW1772_FSTP_8164B_FSIL_SCAN_SERVER_V1.3.0.1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Status.ResumeLayout(false);
            this.Status.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartServer;
        private System.Windows.Forms.Button btnStopServer;
        public System.Windows.Forms.ListView ListView;
        public System.Windows.Forms.ColumnHeader ClientID;
        public System.Windows.Forms.ColumnHeader SendCount;
        public System.Windows.Forms.ColumnHeader SendByte;
        public System.Windows.Forms.ColumnHeader ReciveCount;
        public System.Windows.Forms.ColumnHeader ReciveByte;
        private System.Windows.Forms.StatusStrip Status;
        private System.Windows.Forms.ToolStripStatusLabel ConnectCount;
        private System.Windows.Forms.ToolStripStatusLabel ServerState;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        public System.Windows.Forms.ColumnHeader State;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ServerPort;
        private System.Windows.Forms.Timer FlowTimer;
        private System.Windows.Forms.ToolStripStatusLabel PreSecSendBytes;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel PreSecReceBytes;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        public System.Windows.Forms.ListView ListView1;
        public System.Windows.Forms.ColumnHeader columnHeader1;
        public System.Windows.Forms.ColumnHeader columnHeader2;
        public System.Windows.Forms.ColumnHeader columnHeader3;
        public System.Windows.Forms.ColumnHeader columnHeader4;
        public System.Windows.Forms.ColumnHeader columnHeader5;
        public System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.Button btnSendMessage;
        private System.Windows.Forms.Button btnSingleScan;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ColumnHeader columnHeader11;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.CheckBox CHK_LOG;
        private System.Windows.Forms.ColumnHeader columnHeader12;
        private System.Windows.Forms.TextBox textBox1;
    }
}

