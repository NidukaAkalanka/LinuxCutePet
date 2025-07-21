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
    public enum AnimationState
    {
        Startup,
        Idle,
        AutoTriggered,
        ClickTriggered,
        DragTriggered,
        DragLoop,
        DragEnd,
        Sleep,
        SleepLoop,
        SleepEnd,
        Shutdown
    }

    public partial class MainWindow : Window
    {
        private Point _startPosition;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private PixelPoint _resizeStartPosition;
        private Size _originalSize;
        
        // Animation-related fields
        private DispatcherTimer _animationTimer = null!;
        private DispatcherTimer _autoTriggerTimer = null!;
        private DispatcherTimer _clickDragTimer = null!;
        private List<string> _currentAnimationFrames = null!;
        private int _currentFrameIndex = 0;
        private Image? _petImage;
        private AnimationState _currentState = AnimationState.Startup;
        private AnimationState _nextState = AnimationState.Idle;
        private bool _isLooping = false;
        private Random _random = new Random();
        private bool _isSleeping = false;
        
        // Click detection
        private Point _clickPosition;
        private bool _isWaitingForDragOrClick = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize animation system
            InitializeAnimationSystem();
            
            // Get a reference to UI elements
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var resizeGrip = this.FindControl<Rectangle>("ResizeGrip");
            _petImage = this.FindControl<Image>("PetImage");
            
            // Add null checks to avoid the CS8602 warning
            if (mainGrid != null)
            {
                // Make the window movable by dragging anywhere
                mainGrid.PointerPressed += MainGrid_PointerPressed;
                mainGrid.PointerReleased += MainGrid_PointerReleased;
                
                // Add right-click context menu
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
            
            // Start with startup animation
            StartAnimation(AnimationState.Startup);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeAnimationSystem()
        {
            // Initialize the main animation timer
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // 100ms between frames for smoother animation
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            
            // Initialize auto-trigger timer for random events during idle
            _autoTriggerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(GetRandomIdleTime())
            };
            _autoTriggerTimer.Tick += AutoTriggerTimer_Tick;
            
            // Initialize click/drag detection timer
            _clickDragTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 500ms delay to detect drag vs click
            };
            _clickDragTimer.Tick += ClickDragTimer_Tick;
        }
        
        private double GetRandomIdleTime()
        {
            // Random idle time between 5-15 seconds
            return _random.NextDouble() * 10 + 5;
        }
        
        private List<string> LoadAnimationFrames(string animationPath)
        {
            var frames = new List<string>();
            
            // Look for numbered frames starting from 000
            for (int i = 0; i < 1000; i++)
            {
                string frameName = $"{i:D3}.png";
                string resourcePath = $"avares://PetViewerLinux/Assets/{animationPath}/{frameName}";
                
                // Check if the resource exists by trying to access it
                try
                {
                    var uri = new Uri(resourcePath);
                    using var asset = Avalonia.Platform.AssetLoader.Open(uri);
                    if (asset != null)
                    {
                        frames.Add(resourcePath);
                    }
                }
                catch
                {
                    // If we can't load this frame, we've reached the end
                    break;
                }
            }
            
            return frames;
        }
        
        private void StartAnimation(AnimationState state, string? specificPath = null)
        {
            _currentState = state;
            string animationPath = "";
            _isLooping = false;
            
            switch (state)
            {
                case AnimationState.Startup:
                    animationPath = "startup";
                    _nextState = AnimationState.Idle;
                    break;
                    
                case AnimationState.Idle:
                    animationPath = "idle";
                    _isLooping = true;
                    // Reset auto-trigger timer with new random interval
                    _autoTriggerTimer.Interval = TimeSpan.FromSeconds(GetRandomIdleTime());
                    _autoTriggerTimer.Start();
                    break;
                    
                case AnimationState.AutoTriggered:
                    if (specificPath != null)
                        animationPath = specificPath;
                    else
                        animationPath = GetRandomAutoTriggeredPath();
                    _nextState = AnimationState.Idle;
                    break;
                    
                case AnimationState.ClickTriggered:
                    if (specificPath != null)
                        animationPath = specificPath;
                    _nextState = AnimationState.Idle;
                    break;
                    
                case AnimationState.DragTriggered:
                    // Show the initial drag frame
                    ShowSingleFrame("dragTriggered/000.png");
                    return;
                    
                case AnimationState.DragLoop:
                    animationPath = "dragTriggered/loop";
                    _isLooping = true;
                    break;
                    
                case AnimationState.DragEnd:
                    animationPath = "dragTriggered/loopOut";
                    _nextState = AnimationState.Idle;
                    break;
                case AnimationState.Sleep:
                    animationPath = "menuTriggered/sleep";
                    _nextState = AnimationState.SleepLoop;
                    break;
    
                case AnimationState.SleepLoop:
                    animationPath = "menuTriggered/sleep/loop";
                    _isLooping = true;
                    _isSleeping = true;
                    break;
                    
                case AnimationState.SleepEnd:
                    animationPath = "menuTriggered/sleep/loopOut";
                    _nextState = AnimationState.Idle;
                    _isSleeping = false;
                    break;
                    
                case AnimationState.Shutdown:
                    animationPath = "shutdown";
                    break;
            }
            
            _currentAnimationFrames = LoadAnimationFrames(animationPath);
            _currentFrameIndex = 0;
            
            if (_currentAnimationFrames.Count > 0)
            {
                DisplayCurrentFrame();
                _animationTimer.Start();
            }
        }
        
        private void ShowSingleFrame(string framePath)
        {
            if (_petImage != null)
            {
                string resourcePath = $"avares://PetViewerLinux/Assets/{framePath}";
                try
                {
                    var uri = new Uri(resourcePath);
                    _petImage.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
                }
                catch
                {
                    // Frame not found, ignore
                }
            }
        }
        
        private string GetRandomAutoTriggeredPath()
        {
            var autoTriggeredFolders = new[]
            {
                "autoTriggered/aside",
                "autoTriggered/boring", 
                "autoTriggered/meow",
                "autoTriggered/squat",
                "autoTriggered/tennis",
                "autoTriggered/think",
                "autoTriggered/yawning",
                "autoTriggered/down"
            };
            
            return autoTriggeredFolders[_random.Next(autoTriggeredFolders.Length)];
        }
        
        private string GetRandomBodyClickPath()
        {
            var bodyClickFolders = new[]
            {
                "clickTriggered/click_BODY/0",
                "clickTriggered/click_BODY/1",
                "clickTriggered/click_BODY/2"
            };
            
            return bodyClickFolders[_random.Next(bodyClickFolders.Length)];
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentAnimationFrames.Count == 0) return;
            
            DisplayCurrentFrame();
            _currentFrameIndex++;
            
            // Check if animation is complete
            if (_currentFrameIndex >= _currentAnimationFrames.Count)
            {
                if (_isLooping)
                {
                    _currentFrameIndex = 0; // Reset to beginning for loop
                    // Don't display here - let the next timer tick handle frame 0 display
                    // This ensures consistent timing between all frames including loop transition
                }
                else
                {
                    // Animation complete, stop timer and transition to next state
                    _animationTimer.Stop();
                    
                    if (_currentState == AnimationState.Shutdown)
                    {
                        this.Close();
                        return;
                    }
                    
                    // Transition to next state
                    if (_nextState != _currentState)
                    {
                        StartAnimation(_nextState);
                    }
                }
            }
        }
        
        private void DisplayCurrentFrame()
        {
            if (_petImage != null && _currentFrameIndex < _currentAnimationFrames.Count)
            {
                try
                {
                    var uri = new Uri(_currentAnimationFrames[_currentFrameIndex]);
                    _petImage.Source = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
                }
                catch
                {
                    // Frame loading failed, skip
                }
            }
        }
        
        private void AutoTriggerTimer_Tick(object? sender, EventArgs e)
        {
            // Only trigger if currently in idle state and not sleeping
            if (_currentState == AnimationState.Idle && !_isSleeping)
            {
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.AutoTriggered);
            }
            else
            {
                // Restart timer with new random interval if not in idle
                _autoTriggerTimer.Interval = TimeSpan.FromSeconds(GetRandomIdleTime());
            }
        }

        private void HandleClick(Point clickPosition)
        {
            // Determine if click was on head or body
            // Assuming top 1/3 of window is head, rest is body
            double headThreshold = this.Height / 3.0;

            if (clickPosition.Y <= headThreshold)
            {
                // Head click
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.ClickTriggered, "clickTriggered/click_HEAD");
            }
            else
            {
                // Body click - randomly select from 3 body click animations
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.ClickTriggered, GetRandomBodyClickPath());
            }
        }

        
        private void ClickDragTimer_Tick(object? sender, EventArgs e)
        {
            _clickDragTimer.Stop();
            _isWaitingForDragOrClick = false;
            
            // If we reach here, it's a click (not a drag)
            HandleClick(_clickPosition);
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            
            if (point.Properties.IsRightButtonPressed)
            {
                // Show context menu
                ShowContextMenu();
                e.Handled = true;
                return;
            }
            
            if (point.Properties.IsLeftButtonPressed)
            {
                _startPosition = e.GetPosition(this);
                _clickPosition = _startPosition;
                _isWaitingForDragOrClick = true;
                
                // Start timer to detect if this becomes a drag or remains a click
                _clickDragTimer.Start();
                e.Handled = true;
            }
        }
        
        
        private void MainGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                // End drag sequence
                _isDragging = false;
                _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                StartAnimation(AnimationState.DragEnd);
            }
            else if (_isWaitingForDragOrClick)
            {
                // This is a click (pointer was pressed and released without significant movement)
                HandleClick(_clickPosition);
            }

            _isWaitingForDragOrClick = false;
            _clickDragTimer.Stop();
        }
        
        private void HandleDragStart()
        {
            _isDragging = true;
            _autoTriggerTimer.Stop();
            
            // Show initial drag frame, then start drag loop
            StartAnimation(AnimationState.DragTriggered);
            
            // Start drag loop after a brief delay
            var delayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                StartAnimation(AnimationState.DragLoop);
            };
            delayTimer.Start();
        }
        
        private void ShowContextMenu()
        {
            var contextMenu = new ContextMenu();
            
            if (!_isSleeping)
            {
                var sleepItem = new MenuItem { Header = "Sleep" };
                sleepItem.Click += (s, e) =>
                {
                    _animationTimer.Stop();
                    _autoTriggerTimer.Stop();
                    StartAnimation(AnimationState.Sleep);
                };
                contextMenu.Items.Add(sleepItem);
            }
            else
            {
                var awakeItem = new MenuItem { Header = "Awake" };
                awakeItem.Click += (s, e) =>
                {
                    _animationTimer.Stop();
                    StartAnimation(AnimationState.SleepEnd);
                };
                contextMenu.Items.Add(awakeItem);
            }
            
            // Always have exit option
            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) =>
            {
                _animationTimer.Stop();
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.Shutdown);
            };
            
            contextMenu.Items.Add(exitItem);
            contextMenu.Open(this);
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
            if (_isDragging)
            {
                // End drag sequence
                _isDragging = false;
                _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                StartAnimation(AnimationState.DragEnd);
            }
            else if (_isResizing)
            {
                _isResizing = false;
            }
            else if (_isWaitingForDragOrClick)
            {
                // This is a click (pointer was pressed and released without significant movement)
                HandleClick(_clickPosition);
            }

            _isWaitingForDragOrClick = false;
            _clickDragTimer.Stop();
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            var currentPosition = e.GetPosition(this);
            
            if (_isWaitingForDragOrClick)
            {
                // Check if mouse has moved enough to consider it a drag
                var deltaX = Math.Abs(currentPosition.X - _startPosition.X);
                var deltaY = Math.Abs(currentPosition.Y - _startPosition.Y);
                
                if (deltaX > 5 || deltaY > 5) // 5 pixel threshold for drag detection
                {
                    _clickDragTimer.Stop();
                    _isWaitingForDragOrClick = false;
                    HandleDragStart();
                }
            }
            
            if (_isDragging)
            {
                var delta = currentPosition - _startPosition;
                
                this.Position = new PixelPoint(
                    this.Position.X + (int)delta.X,
                    this.Position.Y + (int)delta.Y
                );
            }
            else if (_isResizing)
            {
                var currentScreenPosition = this.PointToScreen(currentPosition);
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
