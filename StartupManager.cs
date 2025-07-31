using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PetViewerLinux;

public class StartupManager
{
    private PreCacheWindow? _preCacheWindow;
    private readonly List<string> _allImagePaths = new();
    private DispatcherTimer? _timer;
    private int _currentIndex = 0;

    public Window StartApplication()
    {
        // Check if assets directory exists
        string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        
        if (!Directory.Exists(assetsPath))
        {
            // If no Assets directory, create a minimal error window
            return CreateAssetMissingWindow();
        }

        // Create and return pre-cache window
        _preCacheWindow = new PreCacheWindow();
        
        // Start the caching process
        StartAnimationPreCaching();
        
        return _preCacheWindow;
    }

    private void StartAnimationPreCaching()
    {
        CollectAllImagePaths();

        if (_allImagePaths.Count == 0)
        {
            _preCacheWindow?.UpdateProgress(0, 1, "No animations found");
            // Wait a moment then create main window anyway
            var errorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            errorTimer.Tick += (s, e) =>
            {
                errorTimer.Stop();
                TransitionToMainWindow();
            };
            errorTimer.Start();
            return;
        }

        Console.WriteLine($"Starting pre-cache of {_allImagePaths.Count} animation frames");

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void CollectAllImagePaths()
    {
        string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        
        if (!Directory.Exists(assetsPath))
            return;

        // Get all PNG files recursively
        var pngFiles = Directory.GetFiles(assetsPath, "*.png", SearchOption.AllDirectories);
        _allImagePaths.AddRange(pngFiles);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_currentIndex >= _allImagePaths.Count)
        {
            _timer?.Stop();
            OnPreCachingComplete();
            return;
        }

        // Process multiple images per tick for faster loading
        int batchSize = Math.Min(5, _allImagePaths.Count - _currentIndex);
        
        for (int i = 0; i < batchSize && _currentIndex < _allImagePaths.Count; i++)
        {
            string imagePath = _allImagePaths[_currentIndex];
            
            try
            {
                // Just verify the file exists for pre-caching
                // Actual loading will be done by MainWindow as needed
                File.ReadAllBytes(imagePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pre-caching {imagePath}: {ex.Message}");
            }

            _currentIndex++;
        }

        // Update progress
        string currentFile = Path.GetFileName(_allImagePaths[Math.Min(_currentIndex - 1, _allImagePaths.Count - 1)]);
        _preCacheWindow?.UpdateProgress(_currentIndex, _allImagePaths.Count, currentFile);
    }

    private void OnPreCachingComplete()
    {
        _preCacheWindow?.SetCompleted();
        Console.WriteLine("Pre-caching complete! Transitioning to main window...");

        // Small delay before creating main window
        var completeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        completeTimer.Tick += (s, e) =>
        {
            completeTimer.Stop();
            TransitionToMainWindow();
        };
        completeTimer.Start();
    }

    private void TransitionToMainWindow()
    {
        try
        {
            // Create the main window with pre-caching already done
            var mainWindow = new MainWindow(skipPreCaching: true);
            
            // Replace the pre-cache window with the main window
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                _preCacheWindow?.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating main window: {ex.Message}");
            _preCacheWindow?.UpdateProgress(0, 1, "Error starting pet application");
        }
    }

    private Window CreateAssetMissingWindow()
    {
        var errorWindow = new Window
        {
            Title = "Linux Cute Pet - Missing Assets",
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var textBlock = new TextBlock
        {
            Text = "Assets folder not found!\n\nPlease extract the complete mod-friendly package\nincluding the Assets directory.",
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(20)
        };

        errorWindow.Content = textBlock;
        return errorWindow;
    }
}
