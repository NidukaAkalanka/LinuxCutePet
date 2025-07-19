using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes; // Add this for Rectangle
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace PetViewerLinux
{
    public partial class MainWindow : Window
    {
        private Point _startPosition;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private Point _resizeStartPosition;
        private Size _originalSize;

        public MainWindow()
        {
            InitializeComponent();
            
            // Get a reference to UI elements
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var resizeGrip = this.FindControl<Rectangle>("ResizeGrip");
            
            // Add null checks to avoid the CS8602 warning
            if (mainGrid != null)
            {
                // Make the window movable by dragging anywhere
                mainGrid.PointerPressed += (s, e) => 
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    {
                        _isDragging = true;
                        _startPosition = e.GetPosition(this);
                        this.PointerReleased += Window_PointerReleased;
                        this.PointerMoved += Window_PointerMoved;
                    }
                };
                
                // Add double-click to close
                mainGrid.DoubleTapped += (s, e) => this.Close();
            }
            
            // Set up resize functionality
            if (resizeGrip != null)
            {
                resizeGrip.PointerPressed += (s, e) => 
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    {
                        _isResizing = true;
                        _resizeStartPosition = e.GetPosition(this);
                        _originalSize = new Size(this.Width, this.Height);
                        this.PointerReleased += Window_PointerReleased;
                        this.PointerMoved += Window_PointerResized;
                        e.Handled = true;
                    }
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            _isResizing = false;
            this.PointerReleased -= Window_PointerReleased;
            this.PointerMoved -= Window_PointerMoved;
            this.PointerMoved -= Window_PointerResized;
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
        }
        
        private void Window_PointerResized(object? sender, PointerEventArgs e)
        {
            if (_isResizing)
            {
                var currentPosition = e.GetPosition(this);
                var deltaX = currentPosition.X - _resizeStartPosition.X;
                var deltaY = currentPosition.Y - _resizeStartPosition.Y;
                
                this.Width = Math.Max(100, _originalSize.Width + deltaX);
                this.Height = Math.Max(100, _originalSize.Height + deltaY);
            }
        }
    }
}