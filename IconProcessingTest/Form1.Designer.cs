namespace IconProcessingTest
{
	partial class Form1
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.topPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.buttonSelect = new System.Windows.Forms.Button();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.labelStatus = new System.Windows.Forms.Label();
			this.pictureBoxSource = new System.Windows.Forms.PictureBox();
			this.pictureBoxResult = new System.Windows.Forms.PictureBox();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.topPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSource)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxResult)).BeginInit();
			this.SuspendLayout();
			//
			// topPanel
			//
			this.topPanel.Controls.Add(this.buttonSelect);
			this.topPanel.Controls.Add(this.buttonRefresh);
			this.topPanel.Controls.Add(this.labelStatus);
			this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.topPanel.Location = new System.Drawing.Point(0, 0);
			this.topPanel.Name = "topPanel";
			this.topPanel.Padding = new System.Windows.Forms.Padding(4);
			this.topPanel.Size = new System.Drawing.Size(984, 40);
			this.topPanel.TabIndex = 0;
			//
			// buttonSelect
			//
			this.buttonSelect.Location = new System.Drawing.Point(7, 7);
			this.buttonSelect.Name = "buttonSelect";
			this.buttonSelect.Size = new System.Drawing.Size(130, 26);
			this.buttonSelect.TabIndex = 0;
			this.buttonSelect.Text = "Select Screenshot...";
			this.buttonSelect.UseVisualStyleBackColor = true;
			this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
			//
			// buttonRefresh
			//
			this.buttonRefresh.Enabled = false;
			this.buttonRefresh.Location = new System.Drawing.Point(143, 7);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(90, 26);
			this.buttonRefresh.TabIndex = 1;
			this.buttonRefresh.Text = "Refresh";
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			//
			// labelStatus
			//
			this.labelStatus.Location = new System.Drawing.Point(239, 4);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(500, 32);
			this.labelStatus.TabIndex = 2;
			this.labelStatus.Text = "Select a screenshot to begin";
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// pictureBoxSource
			//
			this.pictureBoxSource.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
			this.pictureBoxSource.Dock = System.Windows.Forms.DockStyle.Left;
			this.pictureBoxSource.Location = new System.Drawing.Point(0, 40);
			this.pictureBoxSource.Name = "pictureBoxSource";
			this.pictureBoxSource.Size = new System.Drawing.Size(280, 521);
			this.pictureBoxSource.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBoxSource.TabIndex = 1;
			this.pictureBoxSource.TabStop = false;
			//
			// pictureBoxResult
			//
			this.pictureBoxResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
			this.pictureBoxResult.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBoxResult.Location = new System.Drawing.Point(280, 40);
			this.pictureBoxResult.Name = "pictureBoxResult";
			this.pictureBoxResult.Size = new System.Drawing.Size(704, 521);
			this.pictureBoxResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBoxResult.TabIndex = 2;
			this.pictureBoxResult.TabStop = false;
			//
			// openFileDialog
			//
			this.openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg";
			this.openFileDialog.Title = "Select Source Screenshot";
			//
			// Form1
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(984, 561);
			this.ShowIcon = false;
			this.Controls.Add(this.pictureBoxResult);
			this.Controls.Add(this.pictureBoxSource);
			this.Controls.Add(this.topPanel);
			this.Name = "Form1";
			this.Text = "Icon Processing Test";
			this.topPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSource)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxResult)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel topPanel;
		private System.Windows.Forms.Button buttonSelect;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.PictureBox pictureBoxSource;
		private System.Windows.Forms.PictureBox pictureBoxResult;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
	}
}
