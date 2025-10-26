using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SearcherGui.Services;
using Avalonia.Input.Platform;
using Moq;

namespace TestSearcher.SearcherGui;

public class ResultInteractionServiceTests
{
	[Fact(DisplayName = "GUI: OpenFile validates path before opening")]
	[Trait("Category", "GUI")]
	public void OpenFile_WithValidPath_AttempsToOpen()
	{
		// Create a temporary file to test
		var tempFile = Path.GetTempFileName();
		try {
			// This will attempt to open the file with default app
			// On headless environments, this may fail gracefully
			var result = Record.Exception(() => ResultInteractionService.OpenFile(tempFile));

			// Should not throw exception (may silently fail on headless systems)
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
		var nonExistentPath = "/this/path/does/not/exist/file.txt";

		// Should not throw exception even if file doesn't exist
		var result = Record.Exception(() => ResultInteractionService.OpenFile(nonExistentPath));

		// Implementation should handle this gracefully
		// (may log error but not throw)
		Assert.Null(result);
	}

	[Fact(DisplayName = "GUI: ShowInFolder with missing path returns gracefully", Skip = "Skipped to prevent opening file manager during tests")]
	[Trait("Category", "GUI")]
	public void ShowInFolder_WithValidPath_AttempsToOpen()
	{
		var tempDir = Path.GetTempPath();

		var result = Record.Exception(() => ResultInteractionService.ShowInFolder(tempDir));

		// Should not throw exception
		Assert.Null(result);
	}

	[Fact(DisplayName = "GUI: ShowInFolder returns gracefully for non-existent paths")]
	[Trait("Category", "GUI")]
	public void ShowInFolder_WithNonExistentPath_DoesNotThrow()
	{
		var nonExistentPath = "/this/does/not/exist/file.txt";

		// Should not throw exception even with non-existent path
		var result = Record.Exception(() => ResultInteractionService.ShowInFolder(nonExistentPath));

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
