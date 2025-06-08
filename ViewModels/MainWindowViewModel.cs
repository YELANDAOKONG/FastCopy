// MainWindowViewModel.cs
namespace FastCopy.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastCopy.Services;
using FastCopy.Views;
using Microsoft.Extensions.DependencyInjection;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "拖拽文件到此处以复制内容";
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private int _filesCopied;
    
    [ObservableProperty]
    private AvaloniaList<string> _recentFiles = new();
    
    [ObservableProperty]
    private bool _includeFilePaths = true;
    
    [ObservableProperty]
    private bool _appendToClipboard;
    
    [ObservableProperty] 
    private bool _isSuccessMessageVisible;
    
    private readonly IClipboardService _clipboardService;
    private readonly IServiceProvider _serviceProvider;
    
    public MainWindowViewModel(IClipboardService clipboardService, IServiceProvider serviceProvider)
    {
        _clipboardService = clipboardService;
        _serviceProvider = serviceProvider;
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
            
            var markdownBlocks = new List<string>();
            var processedPaths = new HashSet<string>();
            
            foreach (var file in fileItems)
            {
                var filePath = file.Path.LocalPath; // Changed from AbsolutePath to LocalPath
                
                if (processedPaths.Contains(filePath))
                    continue;
                    
                processedPaths.Add(filePath);
                
                try
                {
                    var content = await ReadFileContentAsync(file);
                    if (string.IsNullOrEmpty(content))
                        continue;
                    
                    var codeBlock = FormatAsMarkdown(content, filePath);
                    markdownBlocks.Add(codeBlock);
                    
                    AddToRecentFiles(filePath);
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
    
    private async Task<string> ReadFileContentAsync(IStorageFile file)
    {
        using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
    
    private string FormatAsMarkdown(string content, string filePath)
    {
        var codeIdentifier = IncludeFilePaths ? filePath : Path.GetExtension(filePath);
        
        return $"```{codeIdentifier}\n{content.TrimEnd()}\n```\n";
    }
    
    private void AddToRecentFiles(string filePath)
    {
        if (RecentFiles.Count >= 10)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
            
        if (!RecentFiles.Contains(filePath))
            RecentFiles.Insert(0, filePath);
    }
    
    [RelayCommand]
    private async Task CopyRecentFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            StatusMessage = "文件不存在";
            return;
        }
        
        IsBusy = true;
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var markdown = FormatAsMarkdown(content, filePath);
            
            await _clipboardService.SetTextAsync(markdown);
            
            StatusMessage = $"已复制文件内容到剪贴板: {Path.GetFileName(filePath)}";
            ShowSuccessMessage();
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private void ClearRecentFiles()
    {
        RecentFiles.Clear();
        StatusMessage = "已清空最近文件列表";
    }
    
    [RelayCommand]
    private void OpenFileListWindow()
    {
        var fileListViewModel = _serviceProvider.GetRequiredService<FileListWindowViewModel>();
        var fileListWindow = new FileListWindow
        {
            DataContext = fileListViewModel
        };
        
        fileListWindow.Show();
    }
    
    private async void ShowSuccessMessage()
    {
        IsSuccessMessageVisible = true;
        await Task.Delay(3000);
        IsSuccessMessageVisible = false;
    }
}
