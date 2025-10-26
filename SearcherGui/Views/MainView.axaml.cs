using System;
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
			_ = vm.OnInitializedAsync();
		}
	}

	private void OpenFile_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			ResultInteractionService.OpenFile(selected.FilePath);
		}
	}

	private async void CopyPath_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			await ResultInteractionService.CopyToClipboardAsync(Clipboard, selected.FilePath);
		}
	}

	private void ShowInFolder_Click(object? sender, RoutedEventArgs e)
	{
		var grid = this.FindControl<DataGrid>("ResultsGrid");
		if (grid?.SelectedItem is SearchResultDisplay selected) {
			ResultInteractionService.ShowInFolder(selected.FilePath);
		}
	}
}
