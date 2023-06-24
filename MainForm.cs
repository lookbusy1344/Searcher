using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Searcher;

public partial class MainForm : Form
{
	private CancellationTokenSource? cts;
	private bool loaded;
	public CliOptions? cliOptions;
	private readonly System.Windows.Forms.Timer timerProgress;
	private readonly Monotonic monotonic = new();

	// this is here to allow the console output to work in a WinForms app
	[LibraryImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool AttachConsole(int dwProcessId);
	public const int ATTACH_PARENT_PROCESS = -1;

	public MainForm()
	{
		InitializeComponent();

		timerProgress = new System.Windows.Forms.Timer { Interval = 1500 };
		timerProgress.Tick += TimerProgress_Tick;

		// this is to reduce flicker
		itemsList.SetDoubleBuffered(true);
	}

	private void TimerProgress_Tick(object? sender, EventArgs e)
	{
		scanProgress.Visible = false;
		timerProgress!.Stop();
	}

	private async Task MainAsync(CliOptions config)
	{
		scanProgress.Value = 0;
		scanProgress.Maximum = 100;
		scanProgress.Visible = true;
		itemsList.Items.Clear();
		progressLabel.Text = "Searching...";
		timerProgress.Stop();

		// Create a new unbounded channel
		var channel = Channel.CreateUnbounded<string>();
		this.cts = new CancellationTokenSource();

		var task = Task.Factory.StartNew(
		  () => LongRunningTask(channel.Writer, config),
		  cts.Token,
		  TaskCreationOptions.LongRunning,
		  TaskScheduler.Default);

		var count = 0;
		var longestfname = 30;

		// Consume the items from the channel as they arrive
		await foreach (var item in channel.Reader.ReadAllAsync())
		{
			var fname = Path.GetFileName(item);
			var l = new ListViewItem(new string[] { fname, item });
			itemsList.Items.Add(l);

			// resize the columns as needed
			++count;
			if (fname.Length > longestfname)
			{
				// this filename is longer than any we've seen so far, so resize the columns
				longestfname = fname.Length;
				ResizeColumns();
			}
			else if (count % 10 == 0)
				itemsList.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		// Wait for the long-running task to complete and get its result
		var result = await task;
		progressLabel.Text = result;
		this.cts = null;
		cancelButton.Enabled = false;

		ResizeColumns();
		timerProgress.Start();
	}

	/// <summary>
	/// Auto-resize the columns to fit the content
	/// </summary>
	public void ResizeColumns()
	{
		itemsList.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		itemsList.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
	}

	/// <summary>
	/// This is a blocking method that will run on a background thread
	/// Its more like a compute-bound task
	/// </summary>
	/// <param name="writer"></param>
	/// <returns></returns>
	private string LongRunningTask(ChannelWriter<string> writer, CliOptions config)
	{
		var parallelthreads = config.DegreeOfParallelism();
		var count = 0;
		var filescount = 0;
		var modulo = 20;

		try
		{
			if (string.IsNullOrEmpty(config.Search))
				throw new OperationCanceledException();

			// outerpatterns are the physical files to find, and may include .zip files even if not given as a pattern, when -z is selected
			// innerpatterns are for searching inside zip files. May be an empty list for everything, but explicitly doesnt include .zip
			var innerpatterns = Utils.ProcessInnerPatterns(config.Pattern!);
			var outerpatterns = Utils.ProcessOuterPatterns(config.Pattern!, config.InsideZips);
			if (outerpatterns.Count == 0) throw new Exception("No pattern specified");

			// Parallel routine for searching folders
			var sw = Stopwatch.StartNew();
			var files = GlobSearch.ParallelFindFiles(config.Folder!.FullName, outerpatterns, parallelthreads, null, cts!.Token);
			Debug.WriteLine($"Found {files.Length} files in {sw.ElapsedMilliseconds}ms");

			// Original routine - doesnt use globs but is faster for small numbers of files
			// we search for files matching outerpatterns, eg including zip files if -z switch was given
			//var sw = Stopwatch.StartNew();
			//var files = GlobSearch.RecursiveFindFiles(config.Folder!.FullName, outerpatterns, parallelthreads, cts!.Token);
			//Debug.WriteLine($"Found {files.Length} files in {sw.ElapsedMilliseconds}ms");

			if (cts!.Token.IsCancellationRequested)
				throw new OperationCanceledException();

			// Work out a reasonable update frequency for the progress bar
			filescount = files.Length;
			if (filescount < 100)
			{
				modulo = 5; // small number of files, so update progress bar every 5 checks
			}
			else
			{
				// 100 or more files, so update progress bar every 1% of files
				modulo = filescount / 100;
				if (modulo < 5) modulo = 5;
			}

			// Show how many files need to be searched, now we know
			Invoke(() =>
			{
				progressLabel.Text = $"Searching {filescount} files...";
				scanProgress.Maximum = filescount;
			});

			// search the files in parallel
			Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = parallelthreads, CancellationToken = cts!.Token }, file =>
			{
				// Search the file for the search string
				if (SearchFile.FileContainsStringWrapper(file, config.Search, innerpatterns, config.GetStringComparison(), cts!.Token))
					writer.TryWrite(file);      // put the file path in the channel, to be displayed on the main UI thread

				// when the task completes, update completed counter. This needs to be thread-safe
				var tempcount = Interlocked.Increment(ref count);

				// update progress bar when needed
				if (tempcount % modulo == 0 && !cts!.Token.IsCancellationRequested)
				{
					Invoke(() =>
					{
						if (cts!.Token.IsCancellationRequested) return;
						if (tempcount >= scanProgress.Value) return;    // dont go backwards

						progressLabel.Text = $"Searching {filescount - tempcount} files...";
						scanProgress.Value = tempcount;
					});
				}
			});
		}
		catch // (OperationCanceledException)
		{
			// just ignore it
		}

		// Close the channel, we are finished now
		writer.Complete();

		// Update the UI bar to 100%
		Invoke(() =>
		{
			scanProgress.Value = filescount;
		});

		// return a string to be displayed on the UI
		if (cts!.Token.IsCancellationRequested)
			return "Cancelled!";
		else
			return $"Finished! {filescount} files scanned in {monotonic.GetSeconds():F1} secs";
	}

	private void CancelButton_Click(object sender, EventArgs e)
	{
		if (this.cts == null) return;
		this.cts.Cancel();
		progressLabel.Text = "Cancelled";
		cancelButton.Enabled = false;
		scanProgress.Value = 0;
	}

	private async void MainForm_Load(object sender, EventArgs e)
	{
		try
		{
			if (loaded) return;
			loaded = true;

			var info = GitVersion.VersionInfo.Get();
			this.Text = $"File Search {info.GetVersionHash(12)}";
			if (cliOptions == null) return;

			// this is a async void method, so we need to catch any exceptions
			await MainAsync(cliOptions);
		}
		catch (Exception ex)
		{
			// Handle the exception
			MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void ItemList_DoubleClick(object sender, EventArgs e)
	{
		var item = itemsList.SelectedItems;
		if (item.Count != 1) return;

		var row = item[0];
		var path = row.SubItems[1].Text;
		if (string.IsNullOrEmpty(path)) return;

		Utils.OpenFile(path);
	}

	// Handle the ColumnClick event to sort items by the clicked column
	private void ItemList_ColumnClick(object sender, ColumnClickEventArgs e)
	{
		if (itemsList.ListViewItemSorter is ListViewItemComparer comparer && comparer.ColumnNumber == e.Column)
		{
			// Change the sort order in the existing ListViewItemComparer object
			if (comparer.Order == SortOrder.Ascending)
				comparer.Order = SortOrder.Descending;
			else
				comparer.Order = SortOrder.Ascending;
		}
		else
			itemsList.ListViewItemSorter = new ListViewItemComparer(e.Column, SortOrder.Ascending);

		itemsList.Sort();
	}

	private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		this.cts?.Cancel();
	}
}

/// <summary>
/// Comparer for ListView column sorting
/// </summary>
public class ListViewItemComparer : IComparer
{
	public int ColumnNumber { get; init; }
	public SortOrder Order { get; set; }

	public ListViewItemComparer()
	{
		ColumnNumber = 0;
		Order = SortOrder.Ascending;
	}

	public ListViewItemComparer(int column, SortOrder order)
	{
		ColumnNumber = column;
		this.Order = order;
	}

	public int Compare(object? x, object? y)
	{
		var xitem = x as ListViewItem;
		var yitem = y as ListViewItem;

		// deal with nulls
		if (xitem == null || yitem == null) return 0;
		if (xitem == null) return -1;
		if (yitem == null) return 1;

		var xText = xitem.SubItems[ColumnNumber].Text;
		var yText = yitem.SubItems[ColumnNumber].Text;

		var returnVal = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);

		// Determine whether the sort order is descending
		if (Order == SortOrder.Descending)
			returnVal *= -1;

		return returnVal;
	}
}
