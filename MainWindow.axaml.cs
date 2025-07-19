using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes; // Add this for Rectangle
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PetViewerLinux
{
    public partial class MainWindow : Window
    {
        private Point _startPosition;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private PixelPoint _resizeStartPosition;
        private Size _originalSize;
        
        // Animation-related fields
        private DispatcherTimer _animationTimer = null!;
        private List<string> _animationFrames = null!;
        private int _currentFrameIndex = 0;
        private Image? _petImage;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize animation
            InitializeAnimation();
            
            // Get a reference to UI elements
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var resizeGrip = this.FindControl<Rectangle>("ResizeGrip");
            _petImage = this.FindControl<Image>("PetImage");
            
            // Add null checks to avoid the CS8602 warning
            if (mainGrid != null)
            {
                // Make the window movable by dragging anywhere
                mainGrid.PointerPressed += MainGrid_PointerPressed;
                
                // Add double-click to close
                mainGrid.DoubleTapped += (s, e) => this.Close();
            }
            
            // Set up resize functionality
            if (resizeGrip != null)
            {
                resizeGrip.PointerPressed += ResizeGrip_PointerPressed;
            }
            
            // Add window-level pointer events
            this.PointerReleased += Window_PointerReleased;
            this.PointerMoved += Window_PointerMoved;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeAnimation()
        {
            // Find all animation frames
            _animationFrames = new List<string>();
            
            // Look for numbered frames starting from 000
            for (int i = 0; i < 100; i++)  // Reduced from 1000 to 100 for efficiency
            {
                string frameName = $"{i:D3}.png";
                string resourcePath = $"avares://PetViewerLinux/Assets/{frameName}";
                
                // Check if the resource exists by trying to access it
                try
                {
                    var uri = new Uri(resourcePath);
                    using var asset = Avalonia.Platform.AssetLoader.Open(uri);
                    if (asset != null)
                    {
                        _animationFrames.Add(resourcePath);
                    }
                }
                catch
                {
                    // If we can't load this frame, we've reached the end
                    break;
                }
            }
            
            // If no numbered frames found, fall back to pet.png
            if (_animationFrames.Count == 0)
            {
                _animationFrames.Add("avares://PetViewerLinux/Assets/pet.png");
            }
            
            // Initialize the timer
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 500ms between frames
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            
            // Start animation if we have multiple frames
            if (_animationFrames.Count > 1)
            {
                _animationTimer.Start();
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_petImage != null && _animationFrames.Count > 0)
            {
                // Update to next frame
                _currentFrameIndex = (_currentFrameIndex + 1) % _animationFrames.Count;
                
                // Load and set the new frame
                var uri = new Uri(_animationFrames[_currentFrameIndex]);
                _petImage.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
            }
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _startPosition = e.GetPosition(this);
                e.Handled = true;
            }
        }

        private void ResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isResizing = true;
                _resizeStartPosition = this.PointToScreen(e.GetPosition(this));
                _originalSize = new Size(this.Width, this.Height);
                e.Handled = true;
            }
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging || _isResizing)
            {
                _isDragging = false;
                _isResizing = false;
            }
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _startPosition;
                
                this.Position = new PixelPoint(
                    this.Position.X + (int)delta.X,
                    this.Position.Y + (int)delta.Y
                );
            }
            else if (_isResizing)
            {
                var currentScreenPosition = this.PointToScreen(e.GetPosition(this));
                var deltaX = currentScreenPosition.X - _resizeStartPosition.X;
                var deltaY = currentScreenPosition.Y - _resizeStartPosition.Y;
                
                var newWidth = Math.Max(100, _originalSize.Width + deltaX);
                var newHeight = Math.Max(100, _originalSize.Height + deltaY);
                
                this.Width = newWidth;
                this.Height = newHeight;
            }
        }
    }
}