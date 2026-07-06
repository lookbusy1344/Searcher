using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace SearcherGui.Services;

public interface IClipboardTextWriter
{
	Task WriteTextAsync(IClipboard? clipboard, string text);
}
