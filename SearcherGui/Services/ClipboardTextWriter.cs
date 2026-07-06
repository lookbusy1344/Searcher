using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace SearcherGui.Services;

public sealed class ClipboardTextWriter : IClipboardTextWriter
{
	public Task WriteTextAsync(IClipboard? clipboard, string text)
	{
		ArgumentNullException.ThrowIfNull(clipboard);
		return clipboard.SetTextAsync(text);
	}
}
