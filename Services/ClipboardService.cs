using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using System;
using System.Threading.Tasks;

namespace FastCopy.Services;


public class ClipboardService : IClipboardService
{
    private IClipboard? _clipboard;
    
    public ClipboardService()
    {
        // In Avalonia 11.3.0+, we need to access clipboard through TopLevel
    }
    
    private IClipboard? GetClipboard()
    {
        if (_clipboard == null)
        {
            // Try to get the clipboard from the active window
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                _clipboard = desktop.MainWindow?.Clipboard;
            }
        }
        
        return _clipboard;
    }
    
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            return await clipboard.GetTextAsync();
        }
        
        return null;
    }
    
    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}