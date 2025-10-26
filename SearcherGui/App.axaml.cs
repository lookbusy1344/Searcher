using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SearcherGui.ViewModels;
using SearcherGui.Views;

namespace SearcherGui;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var options = Program.Options;
			var viewModel = new MainViewModel(options);
			desktop.MainWindow = new MainView {
				DataContext = viewModel
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
