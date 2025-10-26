using System;
using System.IO;
using System.Threading.Tasks;
using SearcherGui;
using SearcherGui.ViewModels;

var searcherCorePath = Path.Combine(Directory.GetCurrentDirectory(), "SearcherCore");
Console.WriteLine($"Searching in: {searcherCorePath}");
Console.WriteLine($"Path exists: {Directory.Exists(searcherCorePath)}");

var options = new GuiCliOptions {
    Folder = new DirectoryInfo(searcherCorePath),
    Search = "class",
    Pattern = new[] { "*.cs" }
};

Console.WriteLine($"Options.Folder: {options.Folder.FullName}");
Console.WriteLine($"Options.Search: {options.Search}");
Console.WriteLine($"Options.Pattern: {string.Join(",", options.Pattern)}");

var vm = new MainViewModel(options);
Console.WriteLine($"VM created. Results count before search: {vm.Results.Count}");

await vm.OnInitializedAsync();

Console.WriteLine($"Search completed.");
Console.WriteLine($"FilesScanned: {vm.FilesScanned}");
Console.WriteLine($"MatchesFound: {vm.MatchesFound}");
Console.WriteLine($"Results count: {vm.Results.Count}");
Console.WriteLine($"StatusMessage: {vm.StatusMessage}");

foreach (var result in vm.Results) {
    Console.WriteLine($"  - {result.FileName}");
}
