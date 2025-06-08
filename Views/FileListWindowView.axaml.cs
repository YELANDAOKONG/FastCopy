// FileListWindow.axaml.cs
namespace FastCopy.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using FastCopy.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class FileListWindow : Window
{
    private FileListWindowViewModel ViewModel => (FileListWindowViewModel)DataContext!;
    
    public FileListWindow()
    {
        InitializeComponent();
        
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }
    
    private void DragOver(object? sender, DragEventArgs e)
    {
        // Only allow file drops
        e.DragEffects = e.DragEffects & (
            DragDropEffects.Copy | 
            DragDropEffects.Link | 
            DragDropEffects.Move);
            
        // Prohibit default behavior
        e.Handled = true;
    }
    
    private async void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var storageItems = await GetStorageItemsFromDrop(e);
            if (storageItems.Any())
            {
                await ViewModel.HandleDropCommand.ExecuteAsync(storageItems);
            }
        }
        
        e.Handled = true;
    }
    
    private async Task<IReadOnlyList<IStorageItem>> GetStorageItemsFromDrop(DragEventArgs e)
    {
        var result = new List<IStorageItem>();
        
        if (e.Data.Contains(DataFormats.Files))
        {
            // Get dropped files
            var files = e.Data.Get(DataFormats.Files);
            if (files is IEnumerable<IStorageItem> storageItems)
            {
                result.AddRange(storageItems);
            }
            else
            {
                // Handle file paths if direct storage items aren't available
                var fileNames = e.Data.GetFileNames();
                if (fileNames != null)
                {
                    foreach (var uri in fileNames)
                    {
                        if (File.Exists(uri))
                        {
                            var file = await StorageProvider.TryGetFileFromPathAsync(uri);
                            if (file != null)
                            {
                                result.Add(file);
                            }
                        }
                    }
                }
            }
        }
        
        return result;
    }
}
