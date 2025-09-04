namespace Searcher;

/// <summary>
/// Extension methods for List Views
/// </summary>
public static class ListViewExtensions
{
	// https://stackoverflow.com/questions/442817/c-sharp-flickering-listview-on-update

	/// <summary>
	/// Sets the double buffered property of a list view to the specified value
	/// </summary>
	/// <param name="listView">The List view</param>
	/// <param name="doubleBuffered">Double Buffered or not</param>
	public static void SetDoubleBuffered(this System.Windows.Forms.ListView listView, bool doubleBuffered = true) =>
		listView?.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listView, doubleBuffered, null);
}