using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SearcherGui.Models;
using SearcherGui.Services;
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

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		if (DataContext is MainViewModel vm) {
			_ = InitializeViewModelAsync(vm);
		}
	}

	private void OpenFile_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			_ = ResultInteractionService.OpenFile(selected.FilePath);
		}
	}

	private void CopyPath_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			_ = CopyPathToClipboardAsync(selected.FilePath);
		}
	}

	private void ShowInFolder_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			_ = ResultInteractionService.ShowInFolder(selected.FilePath);
		}
	}

	private void OnResultDoubleClick(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			_ = ResultInteractionService.OpenFile(selected.FilePath);
		}
	}

	private static async Task InitializeViewModelAsync(MainViewModel vm)
	{
		try {
			await vm.OnInitializedAsync();
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Failed to initialize main view model: {ex.Message}");
		}
	}

	private async Task CopyPathToClipboardAsync(string filePath)
	{
		try {
			_ = await ResultInteractionService.CopyToClipboardAsync(Clipboard, filePath);
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Failed to copy path to clipboard: {ex.Message}");
		}
	}
}
