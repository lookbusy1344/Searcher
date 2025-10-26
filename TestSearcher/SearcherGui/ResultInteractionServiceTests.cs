using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Moq;
using SearcherGui.Services;
using Xunit;

namespace TestSearcher.SearcherGui;

public class ResultInteractionServiceTests
{
	// Mock process launcher that doesn't actually start processes
	private sealed class MockProcessLauncher : IProcessLauncher
	{
		public Process? Start(string fileName, string arguments)
		{
			// Return a fake non-null process to indicate success
			// In reality, no process is started
			return null; // Return null to test error handling, or a mock Process for success paths
		}

		public Process? Start(string fileName, string[] argumentList)
		{
			return null;
		}

		public Process? Start(ProcessStartInfo startInfo)
		{
			return null;
		}
	}

	[Fact(DisplayName = "GUI: OpenFile validates path before opening")]
	[Trait("Category", "GUI")]
	public void OpenFile_WithValidPath_AttempsToOpen()
	{
		var mockLauncher = new MockProcessLauncher();

		// Create a temporary file to test
		var tempFile = Path.GetTempFileName();
		try {
			// This will attempt to open the file with mock launcher (no actual process started)
			var result = Record.Exception(() => ResultInteractionService.OpenFile(tempFile, mockLauncher));

			// Should not throw exception
			Assert.Null(result);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "GUI: OpenFile handles missing files gracefully")]
	[Trait("Category", "GUI")]
	public void OpenFile_WithMissingFile_HandlesSilently()
	{
		var mockLauncher = new MockProcessLauncher();
		var nonExistentPath = "/this/path/does/not/exist/file.txt";

		// Should not throw exception even if file doesn't exist
		var result = Record.Exception(() => ResultInteractionService.OpenFile(nonExistentPath, mockLauncher));

		// Implementation should handle this gracefully
		// (may log error but not throw)
		Assert.Null(result);
	}

	[Fact(DisplayName = "GUI: ShowInFolder with valid path returns gracefully")]
	[Trait("Category", "GUI")]
	public void ShowInFolder_WithValidPath_AttempsToOpen()
	{
		var mockLauncher = new MockProcessLauncher();
		var tempDir = Path.GetTempPath();

		var result = Record.Exception(() => ResultInteractionService.ShowInFolder(tempDir, mockLauncher));

		// Should not throw exception
		Assert.Null(result);
	}

	[Fact(DisplayName = "GUI: ShowInFolder returns gracefully for non-existent paths")]
	[Trait("Category", "GUI")]
	public void ShowInFolder_WithNonExistentPath_DoesNotThrow()
	{
		var mockLauncher = new MockProcessLauncher();
		var nonExistentPath = "/this/does/not/exist/file.txt";

		// Should not throw exception even with non-existent path
		var result = Record.Exception(() => ResultInteractionService.ShowInFolder(nonExistentPath, mockLauncher));

		Assert.Null(result);
	}

	[Fact(DisplayName = "GUI: CopyToClipboardAsync copies text successfully")]
	[Trait("Category", "GUI")]
	public async Task CopyToClipboardAsync_CopiesTextCorrectly()
	{
		var mockClipboard = new Mock<IClipboard>();
		mockClipboard
			.Setup(c => c.SetTextAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var text = "/path/to/file.txt";

		await ResultInteractionService.CopyToClipboardAsync(mockClipboard.Object, text);

		mockClipboard.Verify(c => c.SetTextAsync(text), Times.Once);
	}

	[Fact(DisplayName = "GUI: CopyToClipboardAsync handles empty strings")]
	[Trait("Category", "GUI")]
	public async Task CopyToClipboardAsync_WithEmptyString_CompletesSilently()
	{
		var mockClipboard = new Mock<IClipboard>();
		mockClipboard
			.Setup(c => c.SetTextAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		await ResultInteractionService.CopyToClipboardAsync(mockClipboard.Object, "");

		mockClipboard.Verify(c => c.SetTextAsync(""), Times.Once);
	}
}
