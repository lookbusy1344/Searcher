using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearcherGui;
using SearcherGui.ViewModels;
using Xunit;

namespace TestSearcher.SearcherGui;

public class GuiSmartQuotesTests
{
	private static readonly string[] options = new[] { "*.cs" };

	[Fact(DisplayName = "GUI Systematic: ASCII quotes find results in SearcherCore")]
	[Trait("Category", "GUI-Systematic")]
	public async Task AsciiQuotes_FindResults()
	{
		var searcherCorePath = FindSearcherCorePath(Directory.GetCurrentDirectory());
		var vm = new MainViewModel(new GuiCliOptions {
			Folder = new DirectoryInfo(searcherCorePath),
			Pattern = options,
			Search = "class"
		});

		await vm.OnInitializedAsync();
		Assert.True(vm.Results.Count > 0);
		Assert.True(vm.MatchesFound > 0);
	}

	[Fact(DisplayName = "GUI Systematic: Smart quote U+201D causes zero matches (diagnostic)")]
	[Trait("Category", "GUI-Systematic")]
	public async Task SmartQuote_YieldsNoMatches()
	{
		var searcherCorePath = FindSearcherCorePath(Directory.GetCurrentDirectory());
		var vm = new MainViewModel(new GuiCliOptions {
			Folder = new DirectoryInfo(searcherCorePath),
			Pattern = options,
			Search = "class\u201D" // trailing right double smart quote
		});

		await vm.OnInitializedAsync();
		Assert.Empty(vm.Results);
		Assert.Equal(0, vm.MatchesFound);
	}

	private static string FindSearcherCorePath(string startPath)
	{
		var dir = new DirectoryInfo(startPath);
		while (dir != null) {
			var probe = Path.Combine(dir.FullName, "SearcherCore");
			if (Directory.Exists(probe)) {
				return probe;
			}

			dir = dir.Parent;
		}
		throw new DirectoryNotFoundException("Could not find SearcherCore directory");
	}
}
