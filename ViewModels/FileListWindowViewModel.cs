// FileListWindowViewModel.cs
namespace FastCopy.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastCopy.Services;

public partial class FileListWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "拖拽文件列表到此处或直接输入文件路径";
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private int _filesCopied;
    
    [ObservableProperty]
    private string _fileListText = "";
    
    [ObservableProperty]
    private bool _includeFilePaths = true;
    
    [ObservableProperty]
    private bool _appendToClipboard;
    
    [ObservableProperty] 
    private bool _isSuccessMessageVisible;
    
    private readonly IClipboardService _clipboardService;
    
    public FileListWindowViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }
    
    [RelayCommand]
    private async Task HandleDropAsync(IReadOnlyList<IStorageItem> items)
    {
        if (items == null || items.Count == 0)
            return;
            
        IsBusy = true;
        FilesCopied = 0;
        
        try
        {
            var fileItems = items.OfType<IStorageFile>().ToList();
            
            if (fileItems.Count == 0)
            {
                StatusMessage = "未找到有效文件";
                return;
            }
            
            foreach (var file in fileItems)
            {
                var filePath = file.Path.LocalPath;
                
                if (Path.GetExtension(filePath).Equals(".fl", StringComparison.OrdinalIgnoreCase))
                {
                    // This is a file list, read it
                    var content = await ReadFileContentAsync(file);
                    FileListText = content;
                    StatusMessage = "已加载文件列表";
                }
                else
                {
                    // Individual file, add to the list
                    FileListText += (string.IsNullOrEmpty(FileListText) ? "" : Environment.NewLine) + file.Path.ToString();
                    StatusMessage = "已添加文件到列表";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理文件列表失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task<string> ReadFileContentAsync(IStorageFile file)
    {
        using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
    
    [RelayCommand]
    private async Task ProcessFileListAsync()
    {
        if (string.IsNullOrWhiteSpace(FileListText))
        {
            StatusMessage = "文件列表为空";
            return;
        }
        
        IsBusy = true;
        FilesCopied = 0;
        
        try
        {
            var lines = FileListText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filePaths = new List<string>();
            
            // Parse file paths from different formats
            foreach (var line in lines)
            {
                string path = line.Trim();
                
                // Skip comments or empty lines
                if (string.IsNullOrWhiteSpace(path) || path.StartsWith("#"))
                    continue;
                
                // Handle different URI formats
                if (path.StartsWith("file:/"))
                {
                    // URI format: file:/path/to/file or file:///path/to/file
                    try
                    {
                        var uri = new Uri(path);
                        path = Uri.UnescapeDataString(uri.LocalPath);
                    }
                    catch (UriFormatException)
                    {
                        // If URI parsing fails, try to use the path as is
                        path = path.Replace("file:/", "").Replace("file:///", "");
                        path = Uri.UnescapeDataString(path);
                    }
                }
                
                if (File.Exists(path))
                {
                    filePaths.Add(path);
                }
            }
            
            if (filePaths.Count == 0)
            {
                StatusMessage = "未找到有效文件";
                return;
            }
            
            var markdownBlocks = new List<string>();
            
            foreach (var filePath in filePaths)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    if (string.IsNullOrEmpty(content))
                        continue;
                    
                    var codeBlock = FormatAsMarkdown(content, filePath);
                    markdownBlocks.Add(codeBlock);
                    
                    FilesCopied++;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"读取失败 [{filePath}]: {ex.Message}";
                }
            }
            
            if (markdownBlocks.Count > 0)
            {
                var finalMarkdown = string.Join("\n", markdownBlocks);
                
                if (AppendToClipboard)
                {
                    var currentClipboard = await _clipboardService.GetTextAsync() ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentClipboard))
                    {
                        finalMarkdown = currentClipboard + "\n\n" + finalMarkdown;
                    }
                }
                
                await _clipboardService.SetTextAsync(finalMarkdown);
                StatusMessage = $"已复制 {FilesCopied} 个文件内容到剪贴板";
                ShowSuccessMessage();
            }
            else
            {
                StatusMessage = "没有有效内容可复制";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private string FormatAsMarkdown(string content, string filePath)
    {
        var codeIdentifier = IncludeFilePaths ? filePath : Path.GetExtension(filePath);
        
        return $"```{codeIdentifier}\n{content.TrimEnd()}\n```\n";
    }
    
    [RelayCommand]
    private void ClearFileList()
    {
        FileListText = string.Empty;
        StatusMessage = "已清空文件列表";
    }
    
    private async void ShowSuccessMessage()
    {
        IsSuccessMessageVisible = true;
        await Task.Delay(3000);
        IsSuccessMessageVisible = false;
    }
}
