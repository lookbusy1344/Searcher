namespace Searcher;

partial class MainForm
{
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		progressLabel = new Label();
		cancelButton = new Button();
		itemsList = new ListView();
		columnHeaderFile = new ColumnHeader();
		columnHeaderPath = new ColumnHeader();
		scanProgress = new ProgressBar();
		SuspendLayout();
		// 
		// progressLabel
		// 
		progressLabel.AutoSize = true;
		progressLabel.Location = new Point(93, 16);
		progressLabel.Name = "progressLabel";
		progressLabel.Size = new Size(38, 15);
		progressLabel.TabIndex = 1;
		progressLabel.Text = "label1";
		// 
		// cancelButton
		// 
		cancelButton.Location = new Point(12, 12);
		cancelButton.Name = "cancelButton";
		cancelButton.Size = new Size(75, 23);
		cancelButton.TabIndex = 3;
		cancelButton.Text = "Cancel";
		cancelButton.UseVisualStyleBackColor = true;
		cancelButton.Click += CancelButton_Click;
		// 
		// itemsList
		// 
		itemsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		itemsList.Columns.AddRange(new ColumnHeader[] { columnHeaderFile, columnHeaderPath });
		itemsList.FullRowSelect = true;
		itemsList.Location = new Point(12, 41);
		itemsList.MultiSelect = false;
		itemsList.Name = "itemsList";
		itemsList.ShowGroups = false;
		itemsList.ShowItemToolTips = true;
		itemsList.Size = new Size(694, 392);
		itemsList.TabIndex = 4;
		itemsList.UseCompatibleStateImageBehavior = false;
		itemsList.View = View.Details;
		itemsList.ColumnClick += ItemList_ColumnClick;
		itemsList.DoubleClick += ItemList_DoubleClick;
		// 
		// columnHeaderFile
		// 
		columnHeaderFile.Text = "File";
		columnHeaderFile.Width = 180;
		// 
		// columnHeaderPath
		// 
		columnHeaderPath.Text = "Path";
		columnHeaderPath.Width = 480;
		// 
		// scanProgress
		// 
		scanProgress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		scanProgress.Location = new Point(511, 12);
		scanProgress.Name = "scanProgress";
		scanProgress.Size = new Size(195, 23);
		scanProgress.TabIndex = 5;
		// 
		// MainForm
		// 
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.ClientSize = new Size(718, 445);
		this.Controls.Add(scanProgress);
		this.Controls.Add(itemsList);
		this.Controls.Add(cancelButton);
		this.Controls.Add(progressLabel);
		this.Name = "MainForm";
		this.Text = "File searching";
		FormClosing += MainForm_FormClosing;
		Load += MainForm_Load;
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion
	private Label progressLabel;
	private Button cancelButton;
	private ListView itemsList;
	private ColumnHeader columnHeaderFile;
	private ColumnHeader columnHeaderPath;
	private ProgressBar scanProgress;
}
