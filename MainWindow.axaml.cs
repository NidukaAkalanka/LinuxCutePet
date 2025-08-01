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
        private bool _isShiftPressed = false; // Track if Shift was pressed during right-click
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
        
        // Memory optimization: Bitmap caching system
        private Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();
        private HashSet<string> _criticalAnimations = new HashSet<string> { "startup", "idle", "shutdown" };
        private string? _currentAnimationPath = null;
        private DispatcherTimer _memoryCleanupTimer = null!;
        
        // Animation pre-caching system
        private bool _isPreCaching = false;
        private bool _preCachingComplete = false;
        private List<string> _allAnimationPaths = new List<string>();
        private int _currentPreCacheIndex = 0;

        // Parameterless constructor for XAML compatibility
        public MainWindow() : this(skipPreCaching: false)
        {
        }

        public MainWindow(bool skipPreCaching = false)
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
            
            // Load dance setting from config
            _isDanceEnabled = ConfigManager.GetDanceEnabled();
            
            // Initialize audio monitoring
            InitializeAudioMonitoring();

            // Get a reference to UI elements
            var mainGrid = this.FindControl<Grid>("MainGrid");
            var resizeGrip = this.FindControl<Rectangle>("ResizeGrip");
            _petImage = this.FindControl<Image>("PetImage");

            // Initialize default pet image
            if (_petImage != null)
            {
                ShowSingleFrame("000.png");
            }

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

            if (!skipPreCaching)
            {
                // Always run pre-caching on startup for optimal performance
                // Hide the main window initially for pre-caching
                this.Hide();
                
                // Start animation pre-caching
                StartAnimationPreCaching();
            }
            else
            {
                // Pre-caching already done by StartupManager
                _preCachingComplete = true;
                // Start with startup animation (proper sequence)
                StartPet();
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Clean up bitmap cache
            ClearBitmapCache();
            
            // Clean up audio monitoring
            _audioMonitorService?.StopMonitoring();
            
            // Stop all timers
            _animationTimer?.Stop();
            _autoTriggerTimer?.Stop();
            _clickDragTimer?.Stop();
            _rightClickDragTimer?.Stop();
            _musicDurationTimer?.Stop();
            _positionUpdateTimer?.Stop();
            _memoryCleanupTimer?.Stop();
            
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
            
            // Initialize memory cleanup timer - runs every 30 seconds to free unused bitmaps
            _memoryCleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _memoryCleanupTimer.Tick += MemoryCleanupTimer_Tick;
            _memoryCleanupTimer.Start();
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
            // Use the factory to create the appropriate audio monitor service for the platform
            _audioMonitorService = AudioMonitorFactory.CreateAudioMonitor();
            
            _audioMonitorService.AudioActivityChanged += OnAudioActivityChanged;
            _audioMonitorService.StartMonitoring();
        }
        
        private void OnAudioActivityChanged(object? sender, AudioActivityChangedEventArgs e)
        {
            if (e.IsActive)
            {
                // Only start tracking if dance is enabled and not already playing
                if (_isDanceEnabled && !_isMusicPlaying)
                {
                    // Audio activity detected - start tracking
                    _musicStartTime = e.Timestamp;
                    _isMusicPlaying = true;
                    _musicDurationTimer.Start(); // Start checking duration
                }
            }
            else if (!e.IsActive && _isMusicPlaying)
            {
                // Audio activity stopped - stop music tracking
                _isMusicPlaying = false;
                _musicDurationTimer.Stop(); // Stop duration checking
                
                // Stop dancing immediately when music ends (no delay needed)
                if (_currentState == AnimationState.MusicLoop || _currentState == AnimationState.MusicTriggered)
                {
                    StartAnimation(AnimationState.MusicEnd);
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
            return this.Position.Y <= _screenBounds.Y + 10; // Within 10 pixels of top edge
        }
        
        private bool IsAtLeftEdge()
        {
            return this.Position.X <= _screenBounds.X + 10; // Within 10 pixels of left edge
        }
        
        private bool IsAtRightEdge()
        {
            return this.Position.X >= _screenBounds.Right - this.Width - 10; // Within 10 pixels of right edge
        }
        
        private bool IsAtBottomEdge()
        {
            return this.Position.Y >= _screenBounds.Bottom - this.Height - 10; // Within 10 pixels of bottom edge
        }
        
        private bool IsWindowOutOfBounds()
        {
            // Check if the window is completely or mostly outside the screen bounds
            var windowLeft = this.Position.X;
            var windowRight = this.Position.X + this.Width;
            var windowTop = this.Position.Y;
            var windowBottom = this.Position.Y + this.Height;
            
            // Consider out of bounds if more than 80% of the window is outside screen
            var visibleArea = 0.0;
            
            // Calculate visible area
            var visibleLeft = Math.Max(windowLeft, _screenBounds.X);
            var visibleRight = Math.Min(windowRight, _screenBounds.Right);
            var visibleTop = Math.Max(windowTop, _screenBounds.Y);
            var visibleBottom = Math.Min(windowBottom, _screenBounds.Bottom);
            
            if (visibleLeft < visibleRight && visibleTop < visibleBottom)
            {
                visibleArea = (visibleRight - visibleLeft) * (visibleBottom - visibleTop);
            }
            
            var totalArea = this.Width * this.Height;
            var visiblePercentage = visibleArea / totalArea;
            
            return visiblePercentage < 0.2; // Less than 20% visible = out of bounds
        }
        
        private void RepositionWindowToBounds()
        {
            if (!IsWindowOutOfBounds()) return;
            
            var currentPos = this.Position;
            var newX = currentPos.X;
            var newY = currentPos.Y;
            
            // Determine which edge the window is closest to
            var distanceToLeft = Math.Abs(currentPos.X - _screenBounds.X);
            var distanceToRight = Math.Abs(currentPos.X - (_screenBounds.Right - (int)this.Width));
            var distanceToTop = Math.Abs(currentPos.Y - _screenBounds.Y);
            var distanceToBottom = Math.Abs(currentPos.Y - (_screenBounds.Bottom - (int)this.Height));
            
            // Find the minimum distance to determine closest edge
            var minDistance = Math.Min(Math.Min(distanceToLeft, distanceToRight), 
                                     Math.Min(distanceToTop, distanceToBottom));
            
            // Reposition to the closest edge with a small margin
            const int margin = 5;
            
            if (minDistance == distanceToLeft)
            {
                // Snap to left edge
                newX = _screenBounds.X + margin;
            }
            else if (minDistance == distanceToRight)
            {
                // Snap to right edge
                newX = _screenBounds.Right - (int)this.Width - margin;
            }
            else if (minDistance == distanceToTop)
            {
                // Snap to top edge
                newY = _screenBounds.Y + margin;
            }
            else if (minDistance == distanceToBottom)
            {
                // Snap to bottom edge
                newY = _screenBounds.Bottom - (int)this.Height - margin;
            }
            
            // Ensure the repositioned window is fully within bounds
            newX = Math.Max(_screenBounds.X, Math.Min(newX, _screenBounds.Right - (int)this.Width));
            newY = Math.Max(_screenBounds.Y, Math.Min(newY, _screenBounds.Bottom - (int)this.Height));
            
            this.Position = new PixelPoint(newX, newY);
        }
        
        private void EnsureProperPositionForClimbing(string animationPath)
        {
            var currentPos = this.Position;
            var newX = currentPos.X;
            var newY = currentPos.Y;
            bool needsRepositioning = false;
            
            if (animationPath.Contains("climb"))
            {
                // For climbing animations (bottom to top), ensure the pet is at the bottom edge
                // and positioned properly on the left or right side
                if (animationPath.Contains("climb.left"))
                {
                    // Position at bottom-left edge
                    newX = _screenBounds.X + 10; // Small margin from left edge
                    newY = _screenBounds.Bottom - (int)this.Height - 10; // Small margin from bottom
                    needsRepositioning = true;
                }
                else if (animationPath.Contains("climb.right"))
                {
                    // Position at bottom-right edge  
                    newX = _screenBounds.Right - (int)this.Width - 10; // Small margin from right edge
                    newY = _screenBounds.Bottom - (int)this.Height - 10; // Small margin from bottom
                    needsRepositioning = true;
                }
            }
            else if (animationPath.Contains("fall"))
            {
                // For falling animations (top to bottom), ensure the pet is at the top edge
                if (animationPath.Contains("fall.left"))
                {
                    // Position at top-left edge
                    newX = _screenBounds.X + 10;
                    newY = _screenBounds.Y + 10;
                    needsRepositioning = true;
                }
                else if (animationPath.Contains("fall.right"))
                {
                    // Position at top-right edge
                    newX = _screenBounds.Right - (int)this.Width - 10;
                    newY = _screenBounds.Y + 10;
                    needsRepositioning = true;
                }
            }
            
            if (needsRepositioning)
            {
                this.Position = new PixelPoint(newX, newY);
            }
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
        
        private bool IsOnAnyEdge()
        {
            // Check if the pet is actually at any edge of the screen
            return IsAtLeftEdge() || IsAtRightEdge() || IsAtTopEdge() || IsAtBottomEdge();
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
                // For climbing animations, only allow if pet is actually at an edge
                if (IsCloserToTop())
                {
                    // Move from top to bottom (falling) - allowed from any position
                    var sideSuffix = IsCloserToLeft() ? "left" : "right";
                    return $"autoTriggered/move/vertical/topToBottom/fall.{sideSuffix}";
                }
                else
                {
                    // Move from bottom to top (climbing) - only allow if at edge
                    if (!IsOnAnyEdge())
                    {
                        // Pet is not at an edge, can't climb on nothing
                        // Return null to fall back to idle and let random picker try again later
                        return null;
                    }
                    
                    // Pet is at an edge, determine which side to climb
                    var sideSuffix = IsCloserToLeft() ? "left" : "right";
                    return $"autoTriggered/move/vertical/bottomToTop/climb.{sideSuffix}";
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
                    
                    // Check if we've reached the top edge or if window is going out of bounds
                    if (IsAtTopEdge())
                    {
                        StopMovement();
                        return;
                    }
                    
                    // For climbing animations, ensure we don't go out of bounds
                    if (_currentMoveAnimationPath.Contains("climb"))
                    {
                        var nextY = currentPos.Y + deltaY;
                        if (nextY < _screenBounds.Y)
                        {
                            // Reposition to stay within bounds before continuing
                            this.Position = new PixelPoint(currentPos.X, _screenBounds.Y);
                            StopMovement();
                            return;
                        }
                    }
                }
                else if (_currentMoveAnimationPath.Contains("topToBottom"))
                {
                    deltaY = _moveSpeed;
                    
                    // Check if we've reached the bottom edge or if window is going out of bounds
                    if (IsAtBottomEdge())
                    {
                        StopMovement();
                        return;
                    }
                    
                    // For falling animations, ensure we don't go out of bounds
                    if (_currentMoveAnimationPath.Contains("fall"))
                    {
                        var nextY = currentPos.Y + deltaY;
                        if (nextY > _screenBounds.Bottom - this.Height)
                        {
                            // Reposition to stay within bounds before continuing
                            this.Position = new PixelPoint(currentPos.X, _screenBounds.Bottom - (int)this.Height);
                            StopMovement();
                            return;
                        }
                    }
                }
            }
            
            // Apply movement with bounds checking for vertical climbing/falling
            var newX = currentPos.X + deltaX;
            var newY = currentPos.Y + deltaY;
            
            // Additional bounds check for climbing animations to prevent going out of screen
            if (_currentMoveAnimationPath != null && (_currentMoveAnimationPath.Contains("climb") || _currentMoveAnimationPath.Contains("fall")))
            {
                // Ensure the window stays within screen bounds during vertical movement
                newX = Math.Max(_screenBounds.X, Math.Min(newX, _screenBounds.Right - (int)this.Width));
                newY = Math.Max(_screenBounds.Y, Math.Min(newY, _screenBounds.Bottom - (int)this.Height));
            }
            
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
        
        private void MemoryCleanupTimer_Tick(object? sender, EventArgs e)
        {
            // Periodic cleanup of unused bitmaps (keep current animation and critical ones)
            if (_currentState == AnimationState.Idle) // Only cleanup during idle to avoid interruption
            {
                ClearNonCriticalBitmaps(_currentAnimationPath);
                
                // Force garbage collection if cache is getting large
                if (_bitmapCache.Count > 50)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
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
                string filePath = System.IO.Path.Combine(GetAssetsDirectory(), animationPath, frameName);
                
                // Check if the file exists
                if (File.Exists(filePath))
                {
                    frames.Add(filePath);
                }
                else
                {
                    // If we can't load this frame, we've reached the end
                    break;
                }
            }
            
            return frames;
        }
        
        private string GetAssetsDirectory()
        {
            // Get the directory where the executable is located
            string executablePath = AppContext.BaseDirectory;
            return System.IO.Path.Combine(executablePath, "Assets");
        }
        
        // Memory optimization methods
        private void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap?.Dispose();
            }
            _bitmapCache.Clear();
        }
        
        private void ClearNonCriticalBitmaps(string? keepAnimationPath = null)
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _bitmapCache)
            {
                var framePath = kvp.Key;
                bool isCritical = _criticalAnimations.Any(critical => framePath.Contains(critical));
                bool isCurrentAnimation = keepAnimationPath != null && framePath.Contains(keepAnimationPath);
                
                if (!isCritical && !isCurrentAnimation)
                {
                    kvp.Value?.Dispose();
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _bitmapCache.Remove(key);
            }
        }
        
        private Bitmap? LoadBitmapFromPath(string filePath)
        {
            // Check cache first
            if (_bitmapCache.TryGetValue(filePath, out var cachedBitmap))
            {
                return cachedBitmap;
            }
            
            // Load from file system
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }
                
                var bitmap = new Bitmap(filePath);
                
                // Cache the bitmap
                _bitmapCache[filePath] = bitmap;
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        
        private void PreloadCriticalAnimations()
        {
            // Preload only the essential animations to minimize startup time
            foreach (var criticalAnimation in _criticalAnimations)
            {
                var frames = LoadAnimationFrames(criticalAnimation);
                foreach (var frame in frames.Take(3)) // Only load first 3 frames of each critical animation
                {
                    LoadBitmapFromPath(frame);
                }
            }
        }
        
        private List<string> GetAllAnimationPaths()
        {
            var allPaths = new List<string>();
            
            // Basic animations
            allPaths.AddRange(new[]
            {
                "startup", "idle", "shutdown"
            });
            
            // Auto-triggered animations
            allPaths.AddRange(new[]
            {
                "autoTriggered/aside", "autoTriggered/boring", "autoTriggered/down",
                "autoTriggered/meow", "autoTriggered/squat", "autoTriggered/tennis",
                "autoTriggered/think", "autoTriggered/yawning"
            });
            
            // Movement animations
            allPaths.AddRange(new[]
            {
                "autoTriggered/move/horizontal/leftToRight/walk",
                "autoTriggered/move/horizontal/leftToRight/crawl",
                "autoTriggered/move/horizontal/rightToLeft/walk", 
                "autoTriggered/move/horizontal/rightToLeft/crawl",
                "autoTriggered/move/horizontal/top/leftToRight/swing",
                "autoTriggered/move/horizontal/top/rightToLeft/swing",
                "autoTriggered/move/vertical/bottomToTop/climb.left",
                "autoTriggered/move/vertical/bottomToTop/climb.right",
                "autoTriggered/move/vertical/topToBottom/fall.left",
                "autoTriggered/move/vertical/topToBottom/fall.right"
            });
            
            // Click triggered animations
            allPaths.AddRange(new[]
            {
                "clickTriggered/click_HEAD",
                "clickTriggered/click_BODY/0",
                "clickTriggered/click_BODY/1", 
                "clickTriggered/click_BODY/2"
            });
            
            // Drag animations
            allPaths.AddRange(new[]
            {
                "dragTriggered", "dragTriggered/loop", "dragTriggered/loopOut",
                "rightDragTriggered", "rightDragTriggered/loop", "rightDragTriggered/loopOut"
            });
            
            // Music animations
            allPaths.AddRange(new[]
            {
                "musicTriggered", "musicTriggered/loop", "musicTriggered/loopOut"
            });
            
            // Activity animations
            allPaths.AddRange(new[]
            {
                "menuTriggered/calligraphy", "menuTriggered/calligraphy/loop", "menuTriggered/calligraphy/loopOut",
                "menuTriggered/clean", "menuTriggered/clean/loop", "menuTriggered/clean/loopOut",
                "menuTriggered/game", "menuTriggered/game/loop", "menuTriggered/game/loopOut",
                "menuTriggered/paperwork", "menuTriggered/paperwork/loop", "menuTriggered/paperwork/loopOut",
                "menuTriggered/rope", "menuTriggered/rope/loop", "menuTriggered/rope/loopOut",
                "menuTriggered/sleep", "menuTriggered/sleep/loop", "menuTriggered/sleep/loopOut",
                "menuTriggered/stream", "menuTriggered/stream/loop", "menuTriggered/stream/loopOut",
                "menuTriggered/study", "menuTriggered/study/loop", "menuTriggered/study/loopOut"
            });
            
            return allPaths;
        }
        
        private void StartAnimationPreCaching()
        {
            _isPreCaching = true;
            _allAnimationPaths = GetAllAnimationPaths();
            _currentPreCacheIndex = 0;
            
            var preCacheWindow = new PreCacheWindow();
            preCacheWindow.Show();
            
            // Use a timer to process animations one by one to avoid blocking the UI
            var preCacheTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            
            preCacheTimer.Tick += (s, e) =>
            {
                if (_currentPreCacheIndex >= _allAnimationPaths.Count)
                {
                    // Pre-caching complete
                    preCacheTimer.Stop();
                    _isPreCaching = false;
                    _preCachingComplete = true;
                    
                    preCacheWindow.SetCompleted();
                    
                    // Close pre-cache window after a short delay and start the pet
                    var closeTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1000)
                    };
                    closeTimer.Tick += (cs, ce) =>
                    {
                        closeTimer.Stop();
                        preCacheWindow.Close();
                        StartPet();
                    };
                    closeTimer.Start();
                    return;
                }
                
                // Process current animation
                var animPath = _allAnimationPaths[_currentPreCacheIndex];
                var frames = LoadAnimationFrames(animPath);
                
                // Load all frames to ensure OS caches them
                foreach (var frame in frames)
                {
                    LoadBitmapFromPath(frame);
                }
                
                // Update progress
                preCacheWindow.UpdateProgress(_currentPreCacheIndex + 1, _allAnimationPaths.Count, animPath);
                _currentPreCacheIndex++;
            };
            
            preCacheTimer.Start();
        }
        
        private void StartPet()
        {
            // Show the main window and start normal pet operations
            this.Show();
            StartAnimation(AnimationState.Startup);
        }
        
        public void ReCacheAnimations()
        {
            // Clear current cache
            ClearBitmapCache();
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            // Restart pre-caching (forced re-cache - doesn't modify config)
            this.Hide();
            StartForcedAnimationPreCaching();
        }
        
        private void StartForcedAnimationPreCaching()
        {
            _isPreCaching = true;
            _allAnimationPaths = GetAllAnimationPaths();
            _currentPreCacheIndex = 0;
            
            var preCacheWindow = new PreCacheWindow();
            preCacheWindow.Show();
            
            // Use a timer to process animations one by one to avoid blocking the UI
            var preCacheTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            
            preCacheTimer.Tick += (s, e) =>
            {
                if (_currentPreCacheIndex >= _allAnimationPaths.Count)
                {
                    // Pre-caching complete (forced re-cache - don't update config)
                    preCacheTimer.Stop();
                    _isPreCaching = false;
                    _preCachingComplete = true;
                    
                    preCacheWindow.SetCompleted();
                    
                    // Close pre-cache window after a short delay and start the pet
                    var closeTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1000)
                    };
                    closeTimer.Tick += (cs, ce) =>
                    {
                        closeTimer.Stop();
                        preCacheWindow.Close();
                        StartPet();
                    };
                    closeTimer.Start();
                    return;
                }
                
                // Process current animation
                var animPath = _allAnimationPaths[_currentPreCacheIndex];
                var frames = LoadAnimationFrames(animPath);
                
                // Load all frames to ensure OS caches them
                foreach (var frame in frames)
                {
                    LoadBitmapFromPath(frame);
                }
                
                // Update progress
                preCacheWindow.UpdateProgress(_currentPreCacheIndex + 1, _allAnimationPaths.Count, animPath);
                _currentPreCacheIndex++;
            };
            
            preCacheTimer.Start();
        }
        
        private void StartAnimation(AnimationState state, string? specificPath = null)
        {
            // Stop any current animation to properly interrupt looping animations
            _animationTimer.Stop();
            
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
                        // Check if window is out of bounds before starting movement
                        // This prevents climbing out of view when at screen edges
                        if (_currentMoveAnimationPath.Contains("climb") || _currentMoveAnimationPath.Contains("fall"))
                        {
                            // More aggressive repositioning for climbing animations
                            EnsureProperPositionForClimbing(_currentMoveAnimationPath);
                        }
                        
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
            
            // Memory optimization: Clean up previous animation bitmaps (except critical ones)
            ClearNonCriticalBitmaps(animationPath);
            _currentAnimationPath = animationPath;
            
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
                string filePath = System.IO.Path.Combine(GetAssetsDirectory(), framePath);
                var bitmap = LoadBitmapFromPath(filePath);
                
                if (bitmap != null)
                {
                    _petImage.Source = bitmap;
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
                var resourcePath = _currentAnimationFrames[_currentFrameIndex];
                var bitmap = LoadBitmapFromPath(resourcePath);
                
                if (bitmap != null)
                {
                    _petImage.Source = bitmap;
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
            // Stop any ongoing movement when user clicks
            if (_isMoving)
            {
                StopMovement();
            }
            
            // Reset music state on user interaction to prevent false dancing
            if (_currentState == AnimationState.MusicLoop || _currentState == AnimationState.MusicTriggered)
            {
                _isMusicPlaying = false;
                _musicDurationTimer.Stop();
            }
            
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
            
            // If we reach here, it's a right click (not a drag)
            if (_isShiftPressed)
            {
                // Show developer context menu with autoTriggered actions
                ShowDeveloperContextMenu();
            }
            else
            {
                // Show normal context menu
                ShowContextMenu();
            }
        }

        private void MainGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            
            if (point.Properties.IsRightButtonPressed)
            {
                // Stop any ongoing movement when user right-clicks (before we know if it's click or drag)
                if (_isMoving)
                {
                    StopMovement();
                }
                
                _rightClickPosition = e.GetPosition(this);
                _isWaitingForRightDragOrClick = true;
                _isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                
                // Start timer to detect if this becomes a drag or remains a click
                _rightClickDragTimer.Start();
                e.Handled = true;
                return;
            }
            
            if (point.Properties.IsLeftButtonPressed)
            {
                // Stop any ongoing movement when user left-clicks (before we know if it's click or drag)
                if (_isMoving)
                {
                    StopMovement();
                }
                
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
                
                // Check if window is out of bounds and reposition if needed
                RepositionWindowToBounds();
                
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
                
                // Check if window is out of bounds and reposition if needed
                RepositionWindowToBounds();
                
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
                // This is a right click - show appropriate context menu based on Shift key
                if (_isShiftPressed)
                {
                    ShowDeveloperContextMenu();
                }
                else
                {
                    ShowContextMenu();
                }
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
            // Stop any ongoing movement when user right-clicks
            if (_isMoving)
            {
                StopMovement();
            }
            
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
                    
                    // If dancing is disabled and currently dancing, stop the music animation
                    if (!_isDanceEnabled && (_currentState == AnimationState.MusicLoop || _currentState == AnimationState.MusicTriggered))
                    {
                        // Stop music tracking and force end the dance
                        _isMusicPlaying = false;
                        _musicDurationTimer.Stop();
                        StartAnimation(AnimationState.MusicEnd);
                    }
                    
                    // Save the dance setting to config for persistence
                    ConfigManager.UpdateDanceEnabled(_isDanceEnabled);
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
        
        private void ShowDeveloperContextMenu()
        {
            // Stop any ongoing movement when user shift+right-clicks
            if (_isMoving)
            {
                StopMovement();
            }
            
            var contextMenu = new ContextMenu();
            
            // Add header to indicate this is a developer menu
            var headerItem = new MenuItem { Header = "🔧 Developer Actions", IsEnabled = false };
            contextMenu.Items.Add(headerItem);
            contextMenu.Items.Add(new Separator());
            
            // Basic AutoTriggered animations
            var basicAnimationsItem = new MenuItem { Header = "Basic Animations" };
            
            var basicAnimations = new[]
            {
                ("Aside", "autoTriggered/aside"),
                ("Boring", "autoTriggered/boring"),
                ("Down", "autoTriggered/down"),
                ("Meow", "autoTriggered/meow"),
                ("Squat", "autoTriggered/squat"),
                ("Tennis", "autoTriggered/tennis"),
                ("Think", "autoTriggered/think"),
                ("Yawning", "autoTriggered/yawning")
            };
            
            foreach (var (name, path) in basicAnimations)
            {
                var item = new MenuItem { Header = name };
                var animationPath = path; // Capture for closure
                item.Click += (s, e) => TriggerAutoTriggeredAnimation(animationPath);
                basicAnimationsItem.Items.Add(item);
            }
            
            contextMenu.Items.Add(basicAnimationsItem);
            
            // Movement animations
            var movementItem = new MenuItem { Header = "Movement Animations" };
            
            // Horizontal movements
            var horizontalItem = new MenuItem { Header = "Horizontal" };
            
            var horizontalMovements = new[]
            {
                ("Walk Left to Right", "autoTriggered/move/horizontal/leftToRight/walk"),
                ("Crawl Left to Right", "autoTriggered/move/horizontal/leftToRight/crawl"),
                ("Walk Right to Left", "autoTriggered/move/horizontal/rightToLeft/walk"),
                ("Crawl Right to Left", "autoTriggered/move/horizontal/rightToLeft/crawl"),
                ("Swing Left to Right (Top)", "autoTriggered/move/horizontal/top/leftToRight/swing"),
                ("Swing Right to Left (Top)", "autoTriggered/move/horizontal/top/rightToLeft/swing")
            };
            
            foreach (var (name, path) in horizontalMovements)
            {
                var item = new MenuItem { Header = name };
                var animationPath = path; // Capture for closure
                item.Click += (s, e) => TriggerAutoTriggeredAnimation(animationPath);
                horizontalItem.Items.Add(item);
            }
            
            movementItem.Items.Add(horizontalItem);
            
            // Vertical movements
            var verticalItem = new MenuItem { Header = "Vertical" };
            
            var verticalMovements = new[]
            {
                ("Climb Left (Bottom to Top)", "autoTriggered/move/vertical/bottomToTop/climb.left"),
                ("Climb Right (Bottom to Top)", "autoTriggered/move/vertical/bottomToTop/climb.right"),
                ("Fall Left (Top to Bottom)", "autoTriggered/move/vertical/topToBottom/fall.left"),
                ("Fall Right (Top to Bottom)", "autoTriggered/move/vertical/topToBottom/fall.right")
            };
            
            foreach (var (name, path) in verticalMovements)
            {
                var item = new MenuItem { Header = name };
                var animationPath = path; // Capture for closure
                item.Click += (s, e) => TriggerAutoTriggeredAnimation(animationPath);
                verticalItem.Items.Add(item);
            }
            
            movementItem.Items.Add(verticalItem);
            contextMenu.Items.Add(movementItem);
            
            // Music animations
            var musicItem = new MenuItem { Header = "Music Animations" };
            var musicTriggeredItem = new MenuItem { Header = "Start Music Dance" };
            musicTriggeredItem.Click += (s, e) => TriggerMusicAnimation();
            musicItem.Items.Add(musicTriggeredItem);
            
            if (_currentState == AnimationState.MusicLoop)
            {
                var stopMusicItem = new MenuItem { Header = "Stop Music Dance" };
                stopMusicItem.Click += (s, e) => TriggerStopMusicAnimation();
                musicItem.Items.Add(stopMusicItem);
            }
            
            contextMenu.Items.Add(musicItem);
            
            contextMenu.Items.Add(new Separator());
            
            // Add re-cache animations option
            var reCacheItem = new MenuItem { Header = "🔄 Re-cache Animations" };
            reCacheItem.Click += (s, e) => ReCacheAnimations();
            contextMenu.Items.Add(reCacheItem);
            
            contextMenu.Items.Add(new Separator());
            
            // Regular context menu option
            var normalMenuItem = new MenuItem { Header = "Show Normal Menu" };
            normalMenuItem.Click += (s, e) => ShowContextMenu();
            contextMenu.Items.Add(normalMenuItem);
            
            contextMenu.Open(this);
        }
        
        private void TriggerAutoTriggeredAnimation(string animationPath)
        {
            // Stop current activity/animation
            _autoTriggerTimer.Stop();
            _animationTimer.Stop();
            
            // Stop any current activity
            if (_currentActivity != null)
            {
                _currentActivity = null;
            }
            
            // Stop movement if active
            if (_isMoving)
            {
                StopMovement();
            }
            
            // Trigger the specific animation
            StartAnimation(AnimationState.AutoTriggered, animationPath);
        }
        
        private void TriggerMusicAnimation()
        {
            // Stop current activity/animation
            _autoTriggerTimer.Stop();
            _animationTimer.Stop();
            
            // Stop any current activity
            if (_currentActivity != null)
            {
                _currentActivity = null;
            }
            
            // Stop movement if active
            if (_isMoving)
            {
                StopMovement();
            }
            
            // Start music dance animation
            StartAnimation(AnimationState.MusicTriggered);
        }
        
        private void TriggerStopMusicAnimation()
        {
            // Only stop if currently in music loop
            if (_currentState == AnimationState.MusicLoop)
            {
                StartAnimation(AnimationState.MusicEnd);
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
            if (_isDragging)
            {
                // End drag sequence
                _isDragging = false;
                
                // Check if window is out of bounds and reposition if needed
                RepositionWindowToBounds();
                
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
                
                // Check if window is out of bounds and reposition if needed
                RepositionWindowToBounds();
                
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
                // This is a right click - show appropriate context menu based on Shift key
                if (_isShiftPressed)
                {
                    ShowDeveloperContextMenu();
                }
                else
                {
                    ShowContextMenu();
                }
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
