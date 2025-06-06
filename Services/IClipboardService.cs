using System.Threading.Tasks;

namespace FastCopy.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task<string?> GetTextAsync();
}