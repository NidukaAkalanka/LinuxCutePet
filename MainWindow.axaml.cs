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
        MusicTriggeredStart,
        MusicTriggeredLoop,
        MusicTriggeredEnd,
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
        
        // Music trigger system
        private IAudioMonitor? _audioMonitor;
        private DateTime _lastMusicTriggerStart = DateTime.MinValue;
        private bool _isMusicTriggered = false;
        private DispatcherTimer _musicTriggerDelayTimer = null!;

        public MainWindow()
        {
            InitializeComponent();

            // Set window to stay on top
            this.Topmost = true;

            // Initialize animation system
            InitializeAnimationSystem();
            
            // Initialize pet activities
            InitializePetActivities();
            
            // Initialize music trigger system
            InitializeMusicTriggerSystem();

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
        
        private void InitializeMusicTriggerSystem()
        {
            // Initialize the delay timer for 3-second minimum trigger duration
            _musicTriggerDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // 3 second minimum duration
            };
            _musicTriggerDelayTimer.Tick += MusicTriggerDelayTimer_Tick;
            
            // Initialize audio monitor
            _audioMonitor = new AudioMonitor();
            _audioMonitor.VolumeChanged += OnVolumeChanged;
            _audioMonitor.StartMonitoring();
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
                    
                case AnimationState.MusicTriggeredStart:
                    animationPath = "musicTriggered";
                    _nextState = AnimationState.MusicTriggeredLoop;
                    break;
                    
                case AnimationState.MusicTriggeredLoop:
                    animationPath = "musicTriggered/loop";
                    _isLooping = true;
                    break;
                    
                case AnimationState.MusicTriggeredEnd:
                    animationPath = "musicTriggered/loopOut";
                    _nextState = AnimationState.Idle;
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
            // Only trigger if currently in idle state and no activity is active
            if (_currentState == AnimationState.Idle && _currentActivity == null)
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
            // Stop any ongoing music trigger
            _isMusicTriggered = false;
            _musicTriggerDelayTimer.Stop();
            
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

        private void MusicTriggerDelayTimer_Tick(object? sender, EventArgs e)
        {
            _musicTriggerDelayTimer.Stop();
            
            // After 3 seconds of continuous audio, start music animation if not already active
            if (_isMusicTriggered && _currentState == AnimationState.Idle && _currentActivity == null)
            {
                StartAnimation(AnimationState.MusicTriggeredStart);
            }
        }

        private void OnVolumeChanged(float volume)
        {
            float threshold = SettingsManager.TriggerVolumeThreshold;
            
            if (volume >= threshold)
            {
                // Audio activity detected
                if (!_isMusicTriggered)
                {
                    _isMusicTriggered = true;
                    _lastMusicTriggerStart = DateTime.Now;
                    
                    // Start the 3-second delay timer
                    _musicTriggerDelayTimer.Start();
                }
            }
            else
            {
                // No significant audio activity
                if (_isMusicTriggered)
                {
                    _isMusicTriggered = false;
                    _musicTriggerDelayTimer.Stop();
                    
                    // If we're currently in music-triggered animation, end it
                    if (_currentState == AnimationState.MusicTriggeredLoop)
                    {
                        StartAnimation(AnimationState.MusicTriggeredEnd);
                    }
                }
            }
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
            // Stop any ongoing music trigger
            _isMusicTriggered = false;
            _musicTriggerDelayTimer.Stop();
            
            _isDragging = true;
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
            // Stop any ongoing music trigger
            _isMusicTriggered = false;
            _musicTriggerDelayTimer.Stop();
            
            _isRightDragging = true;
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
                
                // Stop music trigger when starting manual activity
                _isMusicTriggered = false;
                _musicTriggerDelayTimer.Stop();
                
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
                
                // Trigger Volume submenu
                var triggerVolumeItem = new MenuItem { Header = "Trigger Volume" };
                var currentThreshold = SettingsManager.TriggerVolumeThreshold;
                
                // Add volume level options
                var volumeLevels = new[] { 
                    (0.1f, "10%"), 
                    (0.2f, "20%"), 
                    (0.3f, "30%"), 
                    (0.4f, "40%"), 
                    (0.5f, "50%") 
                };
                
                foreach (var (threshold, label) in volumeLevels)
                {
                    var volumeItem = new MenuItem { 
                        Header = label + (Math.Abs(currentThreshold - threshold) < 0.01f ? " ✓" : "")
                    };
                    var capturedThreshold = threshold; // Capture for closure
                    volumeItem.Click += (s, e) => {
                        SettingsManager.TriggerVolumeThreshold = capturedThreshold;
                    };
                    triggerVolumeItem.Items.Add(volumeItem);
                }
                
                settingsItem.Items.Add(triggerVolumeItem);
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
        
        protected override void OnClosed(EventArgs e)
        {
            // Cleanup resources
            _animationTimer?.Stop();
            _autoTriggerTimer?.Stop();
            _clickDragTimer?.Stop();
            _rightClickDragTimer?.Stop();
            _musicTriggerDelayTimer?.Stop();
            
            _audioMonitor?.Dispose();
            
            base.OnClosed(e);
        }
    }
}
