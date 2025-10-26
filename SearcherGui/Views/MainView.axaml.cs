using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SearcherGui.Models;
using SearcherGui.ViewModels;

namespace SearcherGui.Views;

public partial class MainView : Window
{
	public MainView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (DataContext is MainViewModel vm) {
			_ = vm.OnInitializedAsync();
		}
	}

	private void OpenFile_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			OpenFile(selected.FilePath);
		}
	}

	private async void CopyPath_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			await CopyToClipboardAsync(selected.FilePath);
		}
	}

	private void ShowInFolder_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			ShowInFolder(selected.FilePath);
		}
	}

	private static bool OpenFile(string filePath)
	{
		try {
			if (!File.Exists(filePath)) {
				return false;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				Process.Start(new ProcessStartInfo {
					FileName = filePath,
					UseShellExecute = true
				});
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				Process.Start("open", filePath);
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				Process.Start("xdg-open", filePath);
			}
			return true;
		}
		catch {
			return false;
		}
	}

	private static bool ShowInFolder(string filePath)
	{
		try {
			var folder = Path.GetDirectoryName(filePath) ?? filePath;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				Process.Start("explorer", $"/select,{filePath}");
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				Process.Start("open", new[] { "-R", filePath });
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				// Try nautilus first, fallback to generic open
				var result = Process.Start("nautilus", folder);
				if (result == null) {
					Process.Start("xdg-open", folder);
				}
			}
			return true;
		}
		catch {
			return false;
		}
	}

	private async Task<bool> CopyToClipboardAsync(string text)
	{
		try {
			var clipboard = Clipboard;
			if (clipboard == null) {
				return false;
			}

			await clipboard.SetTextAsync(text);
			return true;
		}
		catch {
			return false;
		}
	}
}
