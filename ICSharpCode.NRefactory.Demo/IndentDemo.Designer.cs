namespace ICSharpCode.NRefactory.Demo
{
	partial class IndentDemo
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}
		
		#region Component Designer generated code
		
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBoxIndent = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.lblThisLineIndent = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.lblNextLineIndent = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.lblNeedsReindent = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.lblCurrentState = new System.Windows.Forms.Label();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.lblIsLineStart = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.btnReset = new System.Windows.Forms.Button();
			this.lblCurrentIndent = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.lblLineNo = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// textBoxIndent
			// 
			this.textBoxIndent.AcceptsReturn = true;
			this.textBoxIndent.AcceptsTab = true;
			this.textBoxIndent.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxIndent.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxIndent.Location = new System.Drawing.Point(0, 0);
			this.textBoxIndent.MaxLength = 99999;
			this.textBoxIndent.Multiline = true;
			this.textBoxIndent.Name = "textBoxIndent";
			this.textBoxIndent.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxIndent.Size = new System.Drawing.Size(668, 558);
			this.textBoxIndent.TabIndex = 0;
			this.textBoxIndent.WordWrap = false;
			this.textBoxIndent.TextChanged += new System.EventHandler(this.textBoxIndent_TextChanged);
			this.textBoxIndent.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxIndent_KeyDown);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(17, 38);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(5);
			this.label1.Size = new System.Drawing.Size(122, 27);
			this.label1.TabIndex = 1;
			this.label1.Text = "This line indent: ";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblThisLineIndent
			// 
			this.lblThisLineIndent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblThisLineIndent.AutoSize = true;
			this.lblThisLineIndent.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblThisLineIndent.Location = new System.Drawing.Point(135, 38);
			this.lblThisLineIndent.Name = "lblThisLineIndent";
			this.lblThisLineIndent.Padding = new System.Windows.Forms.Padding(5);
			this.lblThisLineIndent.Size = new System.Drawing.Size(27, 27);
			this.lblThisLineIndent.TabIndex = 2;
			this.lblThisLineIndent.Text = "0";
			this.lblThisLineIndent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(17, 65);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(5);
			this.label2.Size = new System.Drawing.Size(123, 27);
			this.label2.TabIndex = 3;
			this.label2.Text = "Next line indent: ";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblNextLineIndent
			// 
			this.lblNextLineIndent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblNextLineIndent.AutoSize = true;
			this.lblNextLineIndent.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblNextLineIndent.Location = new System.Drawing.Point(135, 65);
			this.lblNextLineIndent.Name = "lblNextLineIndent";
			this.lblNextLineIndent.Padding = new System.Windows.Forms.Padding(5);
			this.lblNextLineIndent.Size = new System.Drawing.Size(27, 27);
			this.lblNextLineIndent.TabIndex = 4;
			this.lblNextLineIndent.Text = "0";
			this.lblNextLineIndent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(268, 38);
			this.label3.Name = "label3";
			this.label3.Padding = new System.Windows.Forms.Padding(5);
			this.label3.Size = new System.Drawing.Size(123, 27);
			this.label3.TabIndex = 5;
			this.label3.Text = "Needs reindent: ";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblNeedsReindent
			// 
			this.lblNeedsReindent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblNeedsReindent.AutoSize = true;
			this.lblNeedsReindent.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblNeedsReindent.Location = new System.Drawing.Point(387, 38);
			this.lblNeedsReindent.Name = "lblNeedsReindent";
			this.lblNeedsReindent.Padding = new System.Windows.Forms.Padding(5);
			this.lblNeedsReindent.Size = new System.Drawing.Size(57, 27);
			this.lblNeedsReindent.TabIndex = 6;
			this.lblNeedsReindent.Text = "False";
			this.lblNeedsReindent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.label4.Location = new System.Drawing.Point(17, 12);
			this.label4.Name = "label4";
			this.label4.Padding = new System.Windows.Forms.Padding(5);
			this.label4.Size = new System.Drawing.Size(108, 27);
			this.label4.TabIndex = 7;
			this.label4.Text = "Current state: ";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblCurrentState
			// 
			this.lblCurrentState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblCurrentState.AutoSize = true;
			this.lblCurrentState.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblCurrentState.Location = new System.Drawing.Point(135, 12);
			this.lblCurrentState.Name = "lblCurrentState";
			this.lblCurrentState.Padding = new System.Windows.Forms.Padding(5);
			this.lblCurrentState.Size = new System.Drawing.Size(101, 27);
			this.lblCurrentState.TabIndex = 8;
			this.lblCurrentState.Text = "GlobalBody";
			this.lblCurrentState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.textBoxIndent);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.lblIsLineStart);
			this.splitContainer1.Panel2.Controls.Add(this.label6);
			this.splitContainer1.Panel2.Controls.Add(this.btnReset);
			this.splitContainer1.Panel2.Controls.Add(this.lblCurrentIndent);
			this.splitContainer1.Panel2.Controls.Add(this.label5);
			this.splitContainer1.Panel2.Controls.Add(this.lblLineNo);
			this.splitContainer1.Panel2.Controls.Add(this.lblCurrentState);
			this.splitContainer1.Panel2.Controls.Add(this.label4);
			this.splitContainer1.Panel2.Controls.Add(this.label1);
			this.splitContainer1.Panel2.Controls.Add(this.lblNeedsReindent);
			this.splitContainer1.Panel2.Controls.Add(this.lblThisLineIndent);
			this.splitContainer1.Panel2.Controls.Add(this.label3);
			this.splitContainer1.Panel2.Controls.Add(this.label2);
			this.splitContainer1.Panel2.Controls.Add(this.lblNextLineIndent);
			this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(5);
			this.splitContainer1.Size = new System.Drawing.Size(668, 673);
			this.splitContainer1.SplitterDistance = 558;
			this.splitContainer1.TabIndex = 9;
			// 
			// lblIsLineStart
			// 
			this.lblIsLineStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblIsLineStart.AutoSize = true;
			this.lblIsLineStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblIsLineStart.Location = new System.Drawing.Point(387, 12);
			this.lblIsLineStart.Name = "lblIsLineStart";
			this.lblIsLineStart.Padding = new System.Windows.Forms.Padding(5);
			this.lblIsLineStart.Size = new System.Drawing.Size(45, 27);
			this.lblIsLineStart.TabIndex = 15;
			this.lblIsLineStart.Text = "Yes";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(268, 12);
			this.label6.Name = "label6";
			this.label6.Padding = new System.Windows.Forms.Padding(5);
			this.label6.Size = new System.Drawing.Size(94, 27);
			this.label6.TabIndex = 14;
			this.label6.Text = "Is line start: ";
			// 
			// btnReset
			// 
			this.btnReset.Location = new System.Drawing.Point(533, 65);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(119, 27);
			this.btnReset.TabIndex = 13;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// lblCurrentIndent
			// 
			this.lblCurrentIndent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblCurrentIndent.AutoSize = true;
			this.lblCurrentIndent.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblCurrentIndent.Location = new System.Drawing.Point(387, 65);
			this.lblCurrentIndent.Name = "lblCurrentIndent";
			this.lblCurrentIndent.Padding = new System.Windows.Forms.Padding(5);
			this.lblCurrentIndent.Size = new System.Drawing.Size(27, 27);
			this.lblCurrentIndent.TabIndex = 12;
			this.lblCurrentIndent.Text = "0";
			this.lblCurrentIndent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(268, 65);
			this.label5.Name = "label5";
			this.label5.Padding = new System.Windows.Forms.Padding(5);
			this.label5.Size = new System.Drawing.Size(116, 27);
			this.label5.TabIndex = 11;
			this.label5.Text = "Current indent: ";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblLineNo
			// 
			this.lblLineNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblLineNo.AutoSize = true;
			this.lblLineNo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.lblLineNo.Location = new System.Drawing.Point(530, 12);
			this.lblLineNo.Name = "lblLineNo";
			this.lblLineNo.Padding = new System.Windows.Forms.Padding(5);
			this.lblLineNo.Size = new System.Drawing.Size(122, 27);
			this.lblLineNo.TabIndex = 10;
			this.lblLineNo.Text = "(Line 1, Col 1)";
			this.lblLineNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// IndentDemo
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "IndentDemo";
			this.Size = new System.Drawing.Size(668, 673);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		
		#endregion
		
		private System.Windows.Forms.TextBox textBoxIndent;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label lblThisLineIndent;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblNextLineIndent;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label lblNeedsReindent;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label lblCurrentState;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Label lblLineNo;
		private System.Windows.Forms.Label lblCurrentIndent;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.Label lblIsLineStart;
		private System.Windows.Forms.Label label6;
	}
}
