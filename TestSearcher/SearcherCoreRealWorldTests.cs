extern alias SearcherCoreLib;
using System;
using System.IO;
using System.Linq;
using SearcherCoreLib::SearcherCore;
using Xunit;

namespace TestSearcher;

public class SearcherCoreRealWorldTests
{
    [Fact(DisplayName = "Core RealWorld: SearcherCore contains 'class' in *.cs files")]
    [Trait("Category", "Core-RealWorld")]
    public void RealWorld_SearcherCore_Class()
    {
        var searcherCorePath = FindSearcherCorePath(Directory.GetCurrentDirectory());
        var inner = Utils.ProcessInnerPatterns(["*.cs"]);
        var outer = Utils.ProcessOuterPatterns(["*.cs"], includezips: false);
        var files = GlobSearch.ParallelFindFiles(searcherCorePath, outer, Environment.ProcessorCount, null, default);
        Assert.NotEmpty(files);

        var matches = files.Count(f => SearchFile.FileContainsStringWrapper(f, "class", inner, StringComparison.OrdinalIgnoreCase, default) == SearchResult.Found);
        Assert.True(matches > 0);
    }

    private static string FindSearcherCorePath(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null) {
            var probe = Path.Combine(dir.FullName, "SearcherCore");
            if (Directory.Exists(probe)) return probe;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find SearcherCore directory");
    }
}
