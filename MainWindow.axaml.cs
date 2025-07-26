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
        RightDragTriggered,
        RightDragLoop,
        RightDragEnd,
        ActivityStart,
        ActivityLoop,
        ActivityEnd,
        MusicTriggered,
        MusicLoop,
        MusicEnd,
        MoveHorizontalLeftToRight,
        MoveHorizontalRightToLeft,
        MoveVerticalBottomToTop,
        MoveVerticalTopToBottom,
        MoveTopSwing,
        MoveLoop,
        MoveEnd,
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
        
        // PetActivity system
        private PetActivity? _currentActivity = null;
        private Dictionary<string, PetActivity> _availableActivities = null!;
        
        // Click detection
        private Point _clickPosition;
        private bool _isWaitingForDragOrClick = false;
        
        // Right-click drag detection
        private Point _rightClickPosition;
        private bool _isWaitingForRightDragOrClick = false;
        private bool _isRightDragging = false;
        private DispatcherTimer _rightClickDragTimer = null!;
        
        // Audio monitoring for music dancing
        private IAudioMonitorService _audioMonitorService = null!;
        private DateTime _musicStartTime;
        private bool _isMusicPlaying = false;
        private bool _isDanceEnabled = true; // Default: dancing enabled
        private DispatcherTimer _musicDurationTimer = null!; // Timer to check if music has played long enough
        private const int MUSIC_DETECTION_DELAY_MS = 3000; // 3 seconds
        private const int MUSIC_STOP_DELAY_MS = 1000; // 1 second delay before stopping dance;
        
        // Screen resolution and position tracking
        private PixelRect _screenBounds;
        private DispatcherTimer _positionUpdateTimer = null!;
        private string? _currentMoveAnimationPath = null;
        private int _moveSpeed = 3; // pixels per frame during movement
        private bool _isMoving = false;

        public MainWindow()
        {
            InitializeComponent();

            // Set window to stay on top
            this.Topmost = true;

            // Initialize screen bounds detection
            InitializeScreenBounds();

            // Initialize animation system
            InitializeAnimationSystem();
            
            // Initialize pet activities
            InitializePetActivities();
            
            // Initialize audio monitoring
            InitializeAudioMonitoring();

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
                // mainGrid.DoubleTapped += (s, e) => this.Close();
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

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Clean up audio monitoring
            _audioMonitorService?.StopMonitoring();
            
            // Stop all timers
            _animationTimer?.Stop();
            _autoTriggerTimer?.Stop();
            _clickDragTimer?.Stop();
            _rightClickDragTimer?.Stop();
            _musicDurationTimer?.Stop();
            _positionUpdateTimer?.Stop();
            
            base.OnClosing(e);
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
            
            // Initialize right-click drag detection timer
            _rightClickDragTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 500ms delay to detect drag vs click
            };
            _rightClickDragTimer.Tick += RightClickDragTimer_Tick;
            
            // Initialize music duration timer
            _musicDurationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Check every 100ms
            };
            _musicDurationTimer.Tick += MusicDurationTimer_Tick;
            
            // Initialize position update timer for movement animations
            _positionUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Update position every 50ms for smooth movement
            };
            _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;
        }
        
        private void InitializeScreenBounds()
        {
            // Get the primary screen bounds
            var screen = this.Screens.Primary;
            if (screen != null)
            {
                _screenBounds = screen.WorkingArea;
            }
            else
            {
                // Fallback to a default screen size if we can't detect
                _screenBounds = new PixelRect(0, 0, 1920, 1080);
            }
        }
        
        private void InitializeAudioMonitoring()
        {
            // Initialize audio monitoring service (Linux-specific for now)
            if (OperatingSystem.IsLinux())
            {
                _audioMonitorService = new LinuxAudioMonitorService();
            }
            else
            {
                // For Windows, we'd add a WindowsAudioMonitorService later
                // For now, create a dummy service that doesn't monitor
                _audioMonitorService = new DummyAudioMonitorService();
            }
            
            _audioMonitorService.AudioActivityChanged += OnAudioActivityChanged;
            _audioMonitorService.StartMonitoring();
        }
        
        private void OnAudioActivityChanged(object? sender, AudioActivityChangedEventArgs e)
        {
            // Only proceed if dance is enabled
            if (!_isDanceEnabled)
                return;
                
            if (e.IsActive)
            {
                if (!_isMusicPlaying)
                {
                    // Audio activity detected - start tracking
                    _musicStartTime = e.Timestamp;
                    _isMusicPlaying = true;
                    _musicDurationTimer.Start(); // Start checking duration
                }
            }
            else if (!e.IsActive && _isMusicPlaying)
            {
                // Audio activity stopped - stop music immediately with 1 second delay
                _isMusicPlaying = false;
                _musicDurationTimer.Stop(); // Stop duration checking
                if (_currentState == AnimationState.MusicLoop)
                {
                    // Wait 1 second before stopping to avoid rapid changes
                    DispatcherTimer.Run(() =>
                    {
                        if (!_isMusicPlaying && _currentState == AnimationState.MusicLoop)
                        {
                            StartAnimation(AnimationState.MusicEnd);
                        }
                        return false;
                    }, TimeSpan.FromMilliseconds(MUSIC_STOP_DELAY_MS));
                }
            }
        }
        
        private void MusicDurationTimer_Tick(object? sender, EventArgs e)
        {
            // Only proceed if music is playing and dance is enabled
            if (!_isMusicPlaying || !_isDanceEnabled)
            {
                _musicDurationTimer.Stop();
                return;
            }
            
            // Check if we've had audio activity long enough to start dancing
            var duration = DateTime.Now - _musicStartTime;
            if (duration.TotalMilliseconds >= MUSIC_DETECTION_DELAY_MS && 
                _currentState == AnimationState.Idle && 
                _currentActivity == null)
            {
                // Start dancing animation
                _autoTriggerTimer.Stop();
                _musicDurationTimer.Stop(); // Stop checking once we start dancing
                StartAnimation(AnimationState.MusicTriggered);
            }
        }
        
        private bool IsAtTopEdge()
        {
            return this.Position.Y <= _screenBounds.Y + 100; // Within 100 pixels of top edge
        }
        
        private bool IsAtLeftEdge()
        {
            return this.Position.X <= _screenBounds.X + 100; // Within 100 pixels of left edge
        }
        
        private bool IsAtRightEdge()
        {
            return this.Position.X >= _screenBounds.Right - this.Width - 100; // Within 100 pixels of right edge
        }
        
        private bool IsAtBottomEdge()
        {
            return this.Position.Y >= _screenBounds.Bottom - this.Height - 100; // Within 100 pixels of bottom edge
        }
        
        private bool IsCloserToLeft()
        {
            double centerX = this.Position.X + this.Width / 2;
            double screenCenterX = _screenBounds.X + _screenBounds.Width / 2;
            return centerX < screenCenterX;
        }
        
        private bool IsCloserToTop()
        {
            double centerY = this.Position.Y + this.Height / 2;
            double screenCenterY = _screenBounds.Y + _screenBounds.Height / 2;
            return centerY < screenCenterY;
        }
        
        private string? GetMoveAnimationBasedOnPosition()
        {
            // Special case: if at top edge, use swing animation
            if (IsAtTopEdge())
            {
                return IsCloserToLeft() ? "autoTriggered/move/horizontal/top/leftToRight/swing" 
                                       : "autoTriggered/move/horizontal/top/rightToLeft/swing";
            }
            
            // Randomly choose between horizontal and vertical movement (70% horizontal, 30% vertical)
            bool useHorizontalMovement = _random.NextDouble() < 0.7;
            
            if (useHorizontalMovement)
            {
                // Determine horizontal movement
                if (IsCloserToLeft())
                {
                    // Move from left to right
                    var activities = new[] { "walk", "crawl" };
                    var selectedActivity = activities[_random.Next(activities.Length)];
                    return $"autoTriggered/move/horizontal/leftToRight/{selectedActivity}";
                }
                else
                {
                    // Move from right to left
                    var activities = new[] { "walk", "crawl" };
                    var selectedActivity = activities[_random.Next(activities.Length)];
                    return $"autoTriggered/move/horizontal/rightToLeft/{selectedActivity}";
                }
            }
            else
            {
                // Determine vertical movement
                if (IsCloserToTop())
                {
                    // Move from top to bottom
                    var activities = new[] { "fall.left", "fall.right" };
                    var selectedActivity = activities[_random.Next(activities.Length)];
                    return $"autoTriggered/move/vertical/topToBottom/{selectedActivity}";
                }
                else
                {
                    // Move from bottom to top
                    var activities = new[] { "climb.left", "climb.right" };
                    var selectedActivity = activities[_random.Next(activities.Length)];
                    return $"autoTriggered/move/vertical/bottomToTop/{selectedActivity}";
                }
            }
        }
        
        private void PositionUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isMoving) return;
            
            var currentPos = this.Position;
            var deltaX = 0;
            var deltaY = 0;
            
            // Calculate movement based on animation type
            if (_currentMoveAnimationPath != null)
            {
                if (_currentMoveAnimationPath.Contains("leftToRight"))
                {
                    deltaX = _moveSpeed;
                    
                    // Check if we've reached the right edge
                    if (IsAtRightEdge())
                    {
                        StopMovement();
                        return;
                    }
                }
                else if (_currentMoveAnimationPath.Contains("rightToLeft"))
                {
                    deltaX = -_moveSpeed;
                    
                    // Check if we've reached the left edge
                    if (IsAtLeftEdge())
                    {
                        StopMovement();
                        return;
                    }
                }
                else if (_currentMoveAnimationPath.Contains("bottomToTop"))
                {
                    deltaY = -_moveSpeed;
                    
                    // Check if we've reached the top edge
                    if (IsAtTopEdge())
                    {
                        StopMovement();
                        return;
                    }
                }
                else if (_currentMoveAnimationPath.Contains("topToBottom"))
                {
                    deltaY = _moveSpeed;
                    
                    // Check if we've reached the bottom edge
                    if (IsAtBottomEdge())
                    {
                        StopMovement();
                        return;
                    }
                }
            }
            
            // Apply movement with bounds checking
            var newX = Math.Max(_screenBounds.X, Math.Min(currentPos.X + deltaX, _screenBounds.Right - (int)this.Width));
            var newY = Math.Max(_screenBounds.Y, Math.Min(currentPos.Y + deltaY, _screenBounds.Bottom - (int)this.Height));
            
            this.Position = new PixelPoint(newX, newY);
        }
        
        private void StartMovement(string animationPath)
        {
            _currentMoveAnimationPath = animationPath;
            _isMoving = true;
            _positionUpdateTimer.Start();
        }
        
        private void StopMovement()
        {
            _isMoving = false;
            _positionUpdateTimer.Stop();
            
            // Transition to move end animation
            if (_currentState == AnimationState.MoveLoop)
            {
                StartAnimation(AnimationState.MoveEnd);
            }
        }
        
        private void InitializePetActivities()
        {
            _availableActivities = new Dictionary<string, PetActivity>
            {
                {
                    "sleep",
                    new PetActivity(
                        "sleep",
                        "Sleep",
                        "Wake up",
                        "menuTriggered/sleep",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "study",
                    new PetActivity(
                        "study",
                        "Study",
                        "Stop Studying",
                        "menuTriggered/study",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "stream",
                    new PetActivity(
                        "stream",
                        "Stream",
                        "End Stream",
                        "menuTriggered/stream",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "clean",
                    new PetActivity(
                        "clean",
                        "Screen-cleaning",
                        "Stop Cleaning",
                        "menuTriggered/clean",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "calligraphy",
                    new PetActivity(
                        "calligraphy",
                        "Calligraphy",
                        "Stop Calligraphy",
                        "menuTriggered/calligraphy",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "game",
                    new PetActivity(
                        "game",
                        "Gaming",
                        "Stop Gaming",
                        "menuTriggered/game",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "rope",
                    new PetActivity(
                        "rope",
                        "Rope-Skipping",
                        "Stop Skipping",
                        "menuTriggered/rope",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
                {
                    "paperwork",
                    new PetActivity(
                        "paperwork",
                        "Do Taxes",
                        "Too much Tax",
                        "menuTriggered/paperwork",
                        AnimationState.ActivityStart,
                        AnimationState.ActivityLoop,
                        AnimationState.ActivityEnd
                    )
                },
            };
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
                    
                    // Check if this is a move animation
                    if (animationPath != null && animationPath.Contains("autoTriggered/move"))
                    {
                        // Determine which move state to use
                        if (animationPath.Contains("horizontal/leftToRight"))
                            _currentState = AnimationState.MoveHorizontalLeftToRight;
                        else if (animationPath.Contains("horizontal/rightToLeft"))
                            _currentState = AnimationState.MoveHorizontalRightToLeft;
                        else if (animationPath.Contains("horizontal/top"))
                            _currentState = AnimationState.MoveTopSwing;
                        else if (animationPath.Contains("vertical/bottomToTop"))
                            _currentState = AnimationState.MoveVerticalBottomToTop;
                        else if (animationPath.Contains("vertical/topToBottom"))
                            _currentState = AnimationState.MoveVerticalTopToBottom;
                        
                        _currentMoveAnimationPath = animationPath;
                        _nextState = AnimationState.MoveLoop;
                    }
                    else
                    {
                        _nextState = AnimationState.Idle;
                    }
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
                    
                case AnimationState.RightDragTriggered:
                    // Show the initial right drag frame
                    ShowSingleFrame("rightDragTriggered/000.png");
                    return;
                    
                case AnimationState.RightDragLoop:
                    animationPath = "rightDragTriggered/loop";
                    _isLooping = true;
                    break;
                    
                case AnimationState.RightDragEnd:
                    animationPath = "rightDragTriggered/loopOut";
                    _nextState = AnimationState.Idle;
                    break;
                    
                case AnimationState.ActivityStart:
                    if (_currentActivity != null)
                    {
                        animationPath = _currentActivity.AnimationPath;
                        _nextState = AnimationState.ActivityLoop;
                    }
                    break;
    
                case AnimationState.ActivityLoop:
                    if (_currentActivity != null)
                    {
                        animationPath = $"{_currentActivity.AnimationPath}/loop";
                        _isLooping = true;
                    }
                    break;
                    
                case AnimationState.ActivityEnd:
                    if (_currentActivity != null)
                    {
                        animationPath = $"{_currentActivity.AnimationPath}/loopOut";
                        _nextState = AnimationState.Idle;
                        _currentActivity = null;
                    }
                    break;
                    
                case AnimationState.MusicTriggered:
                    animationPath = "musicTriggered";
                    _nextState = AnimationState.MusicLoop;
                    break;
                    
                case AnimationState.MusicLoop:
                    animationPath = "musicTriggered/loop";
                    _isLooping = true;
                    break;
                    
                case AnimationState.MusicEnd:
                    animationPath = "musicTriggered/loopOut";
                    _nextState = AnimationState.Idle;
                    break;
                    
                case AnimationState.MoveHorizontalLeftToRight:
                case AnimationState.MoveHorizontalRightToLeft:
                case AnimationState.MoveVerticalBottomToTop:
                case AnimationState.MoveVerticalTopToBottom:
                case AnimationState.MoveTopSwing:
                    if (specificPath != null)
                    {
                        animationPath = specificPath;
                        _currentMoveAnimationPath = specificPath;
                        _nextState = AnimationState.MoveLoop;
                    }
                    break;
                    
                case AnimationState.MoveLoop:
                    if (_currentMoveAnimationPath != null)
                    {
                        animationPath = $"{_currentMoveAnimationPath}/loop";
                        _isLooping = true;
                        // Start movement during loop
                        StartMovement(_currentMoveAnimationPath);
                    }
                    break;
                    
                case AnimationState.MoveEnd:
                    if (_currentMoveAnimationPath != null)
                    {
                        animationPath = $"{_currentMoveAnimationPath}/loopOut";
                        _nextState = AnimationState.Idle;
                        _currentMoveAnimationPath = null;
                    }
                    break;
                    
                case AnimationState.Shutdown:
                    animationPath = "shutdown";
                    break;
            }
            
            _currentAnimationFrames = LoadAnimationFrames(animationPath ?? "");
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
            var allPaths = new List<string>();
            
            // Regular auto-triggered animations
            allPaths.AddRange(new[]
            {
                "autoTriggered/aside",
                "autoTriggered/boring", 
                "autoTriggered/meow",
                "autoTriggered/squat",
                "autoTriggered/tennis",
                "autoTriggered/think",
                "autoTriggered/yawning",
                "autoTriggered/down"
            });
            
            // Add movement animations based on position (30% chance to move, but not during activities)
            if (_random.NextDouble() < 0.3 && _currentActivity == null)
            {
                var moveAnimationPath = GetMoveAnimationBasedOnPosition();
                if (moveAnimationPath != null)
                {
                    return moveAnimationPath;
                }
            }
            
            return allPaths[_random.Next(allPaths.Count)];
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
            // Only trigger if currently in idle state, no activity is active, and not currently moving
            if (_currentState == AnimationState.Idle && _currentActivity == null && !_isMoving)
            {
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.AutoTriggered);
            }
            else
            {
                // Restart timer with new random interval if not in idle or if currently moving
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
            if (_currentActivity == null)
            {
                HandleClick(_clickPosition);
            }
        }
        
        private void RightClickDragTimer_Tick(object? sender, EventArgs e)
        {
            _rightClickDragTimer.Stop();
            _isWaitingForRightDragOrClick = false;
            
            // If we reach here, it's a right click (not a drag) - show context menu
            ShowContextMenu();
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            
            if (point.Properties.IsRightButtonPressed)
            {
                _rightClickPosition = e.GetPosition(this);
                _isWaitingForRightDragOrClick = true;
                
                // Start timer to detect if this becomes a drag or remains a click
                _rightClickDragTimer.Start();
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
                if (_currentActivity == null)
                {
                    _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                    StartAnimation(AnimationState.DragEnd);
                }
            }
            else if (_isRightDragging)
            {
                // End right drag sequence
                _isRightDragging = false;
                if (_currentActivity == null)
                {
                    _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                    StartAnimation(AnimationState.RightDragEnd);
                }
            }
            else if (_isWaitingForDragOrClick)
            {
                // This is a left click (pointer was pressed and released without significant movement)
                if (_currentActivity == null)
                {
                    HandleClick(_clickPosition);
                }
            }
            else if (_isWaitingForRightDragOrClick)
            {
                // This is a right click - show context menu
                ShowContextMenu();
            }

            _isWaitingForDragOrClick = false;
            _isWaitingForRightDragOrClick = false;
            _clickDragTimer.Stop();
            _rightClickDragTimer.Stop();
        }
        
        private void HandleDragStart()
        {
            _isDragging = true;
            
            // Stop any ongoing movement
            if (_isMoving)
            {
                StopMovement();
            }
            
            if (_currentActivity == null)
            {
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
        }
        
        private void HandleRightDragStart()
        {
            _isRightDragging = true;
            
            // Stop any ongoing movement
            if (_isMoving)
            {
                StopMovement();
            }
            
            if (_currentActivity == null)
            {
                _autoTriggerTimer.Stop();
                
                // Show initial right drag frame, then start right drag loop
                StartAnimation(AnimationState.RightDragTriggered);
                
                // Start right drag loop after a brief delay
                var delayTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    StartAnimation(AnimationState.RightDragLoop);
                };
                delayTimer.Start();
            }
        }
        
        private void StartActivity(string activityName)
        {
            if (_availableActivities.TryGetValue(activityName, out var activity))
            {
                _currentActivity = activity;
                _animationTimer.Stop();
                _autoTriggerTimer.Stop();
                StartAnimation(AnimationState.ActivityStart);
            }
        }
        
        private void StopActivity()
        {
            if (_currentActivity != null)
            {
                _animationTimer.Stop();
                StartAnimation(AnimationState.ActivityEnd);
            }
        }
        
        private void ShowContextMenu()
        {
            var contextMenu = new ContextMenu();
            
            if (_currentActivity == null)
            {
                // Create Activities submenu
                var activitiesItem = new MenuItem { Header = "Activities" };
                foreach (var activity in _availableActivities.Values)
                {
                    var activityItem = new MenuItem { Header = activity.StartMenuText };
                    var activityName = activity.Name; // Capture for closure
                    activityItem.Click += (s, e) => StartActivity(activityName);
                    activitiesItem.Items.Add(activityItem);
                }
                contextMenu.Items.Add(activitiesItem);
                
                // Create Settings submenu
                var settingsItem = new MenuItem { Header = "Settings" };
                
                // Add toggle for dance enable/disable
                var danceToggleItem = new MenuItem 
                { 
                    Header = _isDanceEnabled ? "✓ Dancing Enabled" : "Dancing Disabled"
                };
                danceToggleItem.Click += (s, e) => 
                {
                    _isDanceEnabled = !_isDanceEnabled;
                    // TODO: Save settings to file for persistence
                    // SaveSettings();
                };
                settingsItem.Items.Add(danceToggleItem);
                
                contextMenu.Items.Add(settingsItem);
            }
            else
            {
                // Show option to stop current activity
                var stopItem = new MenuItem { Header = _currentActivity.StopMenuText };
                stopItem.Click += (s, e) => StopActivity();
                contextMenu.Items.Add(stopItem);
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
                if (_currentActivity == null)
                {
                    _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                    StartAnimation(AnimationState.DragEnd);
                }
            }
            else if (_isRightDragging)
            {
                // End right drag sequence
                _isRightDragging = false;
                if (_currentActivity == null)
                {
                    _autoTriggerTimer.Stop(); // Stop auto-trigger during drag end animation
                    StartAnimation(AnimationState.RightDragEnd);
                }
            }
            else if (_isResizing)
            {
                _isResizing = false;
            }
            else if (_isWaitingForDragOrClick)
            {
                // This is a left click (pointer was pressed and released without significant movement)
                if (_currentActivity == null)
                {
                    HandleClick(_clickPosition);
                }
            }
            else if (_isWaitingForRightDragOrClick)
            {
                // This is a right click - show context menu
                ShowContextMenu();
            }

            _isWaitingForDragOrClick = false;
            _isWaitingForRightDragOrClick = false;
            _clickDragTimer.Stop();
            _rightClickDragTimer.Stop();
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            var currentPosition = e.GetPosition(this);
            
            if (_isWaitingForDragOrClick)
            {
                // Check if mouse has moved enough to consider it a left drag
                var deltaX = Math.Abs(currentPosition.X - _startPosition.X);
                var deltaY = Math.Abs(currentPosition.Y - _startPosition.Y);
                
                if (deltaX > 5 || deltaY > 5) // 5 pixel threshold for drag detection
                {
                    _clickDragTimer.Stop();
                    _isWaitingForDragOrClick = false;
                    HandleDragStart();
                }
            }
            
            if (_isWaitingForRightDragOrClick)
            {
                // Check if mouse has moved enough to consider it a right drag
                var deltaX = Math.Abs(currentPosition.X - _rightClickPosition.X);
                var deltaY = Math.Abs(currentPosition.Y - _rightClickPosition.Y);
                
                if (deltaX > 5 || deltaY > 5) // 5 pixel threshold for drag detection
                {
                    _rightClickDragTimer.Stop();
                    _isWaitingForRightDragOrClick = false;
                    HandleRightDragStart();
                }
            }
            
            if (_isDragging || _isRightDragging)
            {
                var delta = _isDragging ? 
                    currentPosition - _startPosition : 
                    currentPosition - _rightClickPosition;
                
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
