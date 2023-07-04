using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Searcher;

public partial class MainForm : Form
{
	private CancellationTokenSource? cts;
	private bool loaded;
	public CliOptions? cliOptions;
	private readonly System.Windows.Forms.Timer timerProgress;
	private readonly Monotonic monotonic = new();
	private long nextProgressUpdate = 0L;

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
		var channel = Channel.CreateUnbounded<SingleResult>();
		this.cts = new CancellationTokenSource();

		var task = Task.Factory.StartNew(
		  () => LongRunningTask(channel.Writer, config, true),
		  cts.Token,
		  TaskCreationOptions.LongRunning,
		  TaskScheduler.Default);

		var count = 0;
		var errors = 0;
		var longestfname = 30;

		// Consume the items from the channel as they arrive
		await foreach (var item in channel.Reader.ReadAllAsync())
		{
			var fname = Path.GetFileName(item.Path);

			if (item.Result == SearchResult.Error)
			{
				++errors;
				if (config.HideErrors) continue;

				// an error occurred, show it in the list
				_ = itemsList.Items.Add(new ListViewItem(new string[] { "ERROR", item.Path })
				{
					ForeColor = Color.Red,
					Tag = SearchResult.Error
				});
			}
			else
			{
				// found a match, add it to the list
				var l = new ListViewItem(new string[] { fname, item.Path }) { Tag = SearchResult.Found };
				_ = itemsList.Items.Add(l);
			}

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

		cancelButton.Text = "Copy";
		cancelButton.Enabled = true;

		if (count > 0)
			ResizeColumns();
		timerProgress.Start();

		if (errors > 0)
		{
			if (config.HideErrors)
				_ = MessageBox.Show($"There were {errors} errors. Remove the -h / --hide-errors switch to see details", "Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
				_ = MessageBox.Show($"There were {errors} errors, indicated in the output list", "Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
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
	private string LongRunningTask(ChannelWriter<SingleResult> writer, CliOptions config, bool allowinvoke)
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
			modulo = Utils.CalculateModulo(filescount);

			// Show how many files need to be searched, now we know
			if (allowinvoke)
				Invoke(() =>
				{
					progressLabel.Text = $"Searching {filescount} files...";
					scanProgress.Maximum = filescount;
				});

			// search the files in parallel
			_ = Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = parallelthreads, CancellationToken = cts!.Token }, file =>
			{
				// Search the file for the search string
				var found = SearchFile.FileContainsStringWrapper(file, config.Search, innerpatterns, config.GetStringComparison(), cts!.Token);
				if (found is SearchResult.Found or SearchResult.Error)
					_ = writer.TryWrite(new SingleResult { Path = file, Result = found });      // put the file path in the channel, to be displayed on the main UI thread

				// when the task completes, update completed counter. This needs to be thread-safe
				var tempcount = Interlocked.Increment(ref count);

				// update progress bar when needed
				if (allowinvoke && (modulo == 1 || tempcount % modulo == 0) && !cts!.Token.IsCancellationRequested)
				{
					// ideally nextProgressUpdate would be checked before the invoke, but that would require a lock
					Invoke(() =>
					{
						if (monotonic.GetMilliseconds() < nextProgressUpdate) return;   // only update every 100ms
						if (cts!.Token.IsCancellationRequested) return;                 // cancelled
						if (tempcount < scanProgress.Value) return;                     // dont go backwards

						nextProgressUpdate = this.monotonic.GetMilliseconds() + 100;    // time for next update

						progressLabel.Text = $"Searching {filescount - tempcount} files...";
						scanProgress.Value = tempcount;
					});
				}
			});
		}
		catch
		{
			// just ignore it
		}

		// Close the channel, we are finished now
		writer.Complete();

		// Update the UI bar to 100%
		if (allowinvoke)
			_ = Invoke(() => scanProgress.Value = filescount);

		// return a string to be displayed on the UI
		if (cts!.Token.IsCancellationRequested)
			return "Cancelled!";
		else
			return $"Finished! {filescount} files scanned in {monotonic.GetSeconds():F1} secs";
	}

	private void CancelButton_Click(object sender, EventArgs e)
	{
		if (this.cts == null)
		{
			// we've finished, so this is a copy button
			var results = GetFoundFilenames();
			Clipboard.SetText(results);
			progressLabel.Text = "Results copied to clipboard";
			return;
		}

		// cancel the search
		this.cts.Cancel();
		progressLabel.Text = "Cancelled";
		cancelButton.Enabled = false;
		scanProgress.Value = 0;
	}

	/// <summary>
	/// Turn the listview items into a string, one per line
	/// </summary>
	private string GetFoundFilenames()
	{
		if (itemsList.Items.Count == 0) return string.Empty;

		var sb = new StringBuilder(itemsList.Items.Count * 80);
		foreach (ListViewItem item in itemsList.Items)
		{
			if (item.Tag is SearchResult result && result == SearchResult.Found)
				_ = sb.AppendLine(item.SubItems[1].Text);
		}
		return sb.ToString();
	}

	private async void MainForm_Load(object sender, EventArgs e)
	{
		try
		{
			if (loaded) return;
			loaded = true;

			var info = GitVersion.VersionInfo.Get();
			if (cliOptions == null || cliOptions.Folder == null)
			{
				this.Text = $"File Search {info.GetVersionHash(12)}";
				return;
			}

			var patterns = cliOptions.GetPatterns();
			var path = cliOptions.Folder.FullName;
			if (path.Length > 20)
				path = $"{path[0..5]}...{path[^14..]}";

			this.Text = $"Searching for: '{cliOptions.Search}' -f '{path}' -p {patterns} (commit: {info.GetHash(8)})";

			// this is a async void method, so we need to catch any exceptions
			await MainAsync(cliOptions);
		}
		catch (Exception ex)
		{
			// Handle the exception
			_ = MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

#pragma warning disable IDE0022 // Use expression body for methods
	private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		this.cts?.Cancel();
	}
#pragma warning restore IDE0022 // Use expression body for methods

#if DEBUG
	/// <summary>
	/// Test harness for running without a GUI
	/// </summary>
	public async Task<IList<SingleResult>> TestHarness(CliOptions config)
	{
		// This just serves as a way for the tests to search for files, and get a IList back from the channel

		var channel = Channel.CreateUnbounded<SingleResult>();
		this.cts = new CancellationTokenSource();

		// start the background task, performs the actual search
		var task = Task.Factory.StartNew(
		  () => LongRunningTask(channel.Writer, config, false),
		  cts.Token,
		  TaskCreationOptions.LongRunning,
		  TaskScheduler.Default);

		var results = new List<SingleResult>();

		// collect the results using an async loop
		await foreach (var item in channel.Reader.ReadAllAsync())
			results.Add(item);

		// wait for the task to finish
		var finalmsg = await task;

		// because the checking is parallel, we need to sort to get deterministic results
		//results.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));

		return results;
	}
#endif

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
		// deal with nulls
		if (x is not ListViewItem xitem || y is not ListViewItem yitem) return 0;
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
