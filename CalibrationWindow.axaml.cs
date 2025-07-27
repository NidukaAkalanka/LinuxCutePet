using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace PetViewerLinux
{
    public enum CalibrationEdge
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public partial class CalibrationWindow : Window
    {
        private CalibrationEdge _currentEdge = CalibrationEdge.Top;
        private EdgeCalibration _calibration = new EdgeCalibration();
        private bool _isDragging = false;
        private Point _dragStartPosition;
        private Image? _petImage;
        private TextBlock? _instructionText;
        private Config _config;

        public CalibrationWindow()
        {
            InitializeComponent();
            _config = ConfigManager.LoadConfig();
            
            // Get UI elements
            _petImage = this.FindControl<Image>("PetImage");
            _instructionText = this.FindControl<TextBlock>("InstructionText");
            var skipButton = this.FindControl<Button>("SkipButton");
            
            // Setup event handlers
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                mainGrid.PointerPressed += MainGrid_PointerPressed;
                mainGrid.PointerMoved += MainGrid_PointerMoved;
                mainGrid.PointerReleased += MainGrid_PointerReleased;
            }
            
            if (skipButton != null)
            {
                skipButton.Click += SkipButton_Click;
            }
            
            // Handle keyboard input
            this.KeyDown += CalibrationWindow_KeyDown;
            
            // Start calibration with top edge
            StartEdgeCalibration(CalibrationEdge.Top);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void StartEdgeCalibration(CalibrationEdge edge)
        {
            _currentEdge = edge;
            
            // Load appropriate still frame based on edge
            string framePath = GetFramePathForEdge(edge);
            LoadPetFrame(framePath);
            
            // Update instruction text
            string instruction = GetInstructionForEdge(edge);
            if (_instructionText != null)
            {
                _instructionText.Text = instruction;
            }
            
            // Position pet in center of screen
            if (_petImage != null)
            {
                _petImage.Margin = new Avalonia.Thickness(
                    (this.Bounds.Width - 300) / 2, 
                    (this.Bounds.Height - 300) / 2, 
                    0, 0);
            }
        }

        private string GetFramePathForEdge(CalibrationEdge edge)
        {
            // Use still frames from the movement animation loops
            return edge switch
            {
                CalibrationEdge.Top => "autoTriggered/move/horizontal/top/rightToLeft/swing/loop/001.png",
                CalibrationEdge.Right => "autoTriggered/move/vertical/bottomToTop/climb.right/loop/001.png",
                CalibrationEdge.Bottom => "autoTriggered/move/horizontal/rightToLeft/walk/loop/001.png",
                CalibrationEdge.Left => "autoTriggered/move/vertical/bottomToTop/climb.left/loop/001.png",
                _ => "idle/000.png"
            };
        }

        private string GetInstructionForEdge(CalibrationEdge edge)
        {
            return edge switch
            {
                CalibrationEdge.Top => "Click and drag the pet to the TOP edge of your screen, and press Enter when done.",
                CalibrationEdge.Right => "Click and drag the pet to the RIGHT edge of your screen, and press Enter when done.",
                CalibrationEdge.Bottom => "Click and drag the pet to the BOTTOM edge of your screen, and press Enter when done.",
                CalibrationEdge.Left => "Click and drag the pet to the LEFT edge of your screen, and press Enter when done.",
                _ => "Calibration complete!"
            };
        }

        private void LoadPetFrame(string framePath)
        {
            if (_petImage != null)
            {
                try
                {
                    string resourcePath = $"avares://PetViewerLinux/Assets/{framePath}";
                    var uri = new Uri(resourcePath);
                    _petImage.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load frame {framePath}: {ex.Message}");
                    // Fallback to a basic frame
                    try
                    {
                        string fallbackPath = "avares://PetViewerLinux/Assets/idle/000.png";
                        var uri = new Uri(fallbackPath);
                        _petImage.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
                    }
                    catch
                    {
                        // If even fallback fails, continue without image
                    }
                }
            }
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_petImage != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var position = e.GetPosition(this);
                var margin = _petImage.Margin;
                
                // Check if click is within pet image bounds
                if (position.X >= margin.Left && position.X <= margin.Left + 300 &&
                    position.Y >= margin.Top && position.Y <= margin.Top + 300)
                {
                    _isDragging = true;
                    _dragStartPosition = position;
                    e.Handled = true;
                }
            }
        }

        private void MainGrid_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDragging && _petImage != null)
            {
                var currentPosition = e.GetPosition(this);
                var deltaX = currentPosition.X - _dragStartPosition.X;
                var deltaY = currentPosition.Y - _dragStartPosition.Y;
                
                // Update the pet image position using Margin
                var currentMargin = _petImage.Margin;
                var newLeft = Math.Max(0, Math.Min(this.Bounds.Width - 300, currentMargin.Left + deltaX));
                var newTop = Math.Max(0, Math.Min(this.Bounds.Height - 300, currentMargin.Top + deltaY));
                
                _petImage.Margin = new Avalonia.Thickness(newLeft, newTop, 0, 0);
                
                _dragStartPosition = currentPosition;
            }
        }

        private void MainGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
        }

        private void CalibrationWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveCurrentEdgePosition();
                MoveToNextEdge();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SkipCalibration();
                e.Handled = true;
            }
        }

        private void SkipButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SkipCalibration();
        }

        private void SaveCurrentEdgePosition()
        {
            if (_petImage == null) return;
            
            var margin = _petImage.Margin;
            var petCenter = new Point(
                margin.Left + 150, // Half of 300px width
                margin.Top + 150   // Half of 300px height
            );
            
            switch (_currentEdge)
            {
                case CalibrationEdge.Top:
                    _calibration.TopEdgeY = (int)petCenter.Y;
                    break;
                case CalibrationEdge.Right:
                    _calibration.RightEdgeX = (int)petCenter.X;
                    break;
                case CalibrationEdge.Bottom:
                    _calibration.BottomEdgeY = (int)petCenter.Y;
                    break;
                case CalibrationEdge.Left:
                    _calibration.LeftEdgeX = (int)petCenter.X;
                    break;
            }
        }

        private void MoveToNextEdge()
        {
            switch (_currentEdge)
            {
                case CalibrationEdge.Top:
                    StartEdgeCalibration(CalibrationEdge.Right);
                    break;
                case CalibrationEdge.Right:
                    StartEdgeCalibration(CalibrationEdge.Bottom);
                    break;
                case CalibrationEdge.Bottom:
                    StartEdgeCalibration(CalibrationEdge.Left);
                    break;
                case CalibrationEdge.Left:
                    CompleteCalibration();
                    break;
            }
        }

        private void CompleteCalibration()
        {
            // Save calibration to config
            _config.EdgeCalibration = _calibration;
            ConfigManager.SaveConfig(_config);
            
            // Close calibration window
            this.Close();
        }

        private void SkipCalibration()
        {
            // Create default calibration based on screen bounds
            var screen = this.Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                _calibration.TopEdgeY = bounds.Y + 50;
                _calibration.BottomEdgeY = bounds.Bottom - 50;
                _calibration.LeftEdgeX = bounds.X + 50;
                _calibration.RightEdgeX = bounds.Right - 50;
                
                _config.EdgeCalibration = _calibration;
                ConfigManager.SaveConfig(_config);
            }
            
            this.Close();
        }
    }
}