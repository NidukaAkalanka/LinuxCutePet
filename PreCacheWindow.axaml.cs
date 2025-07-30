using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace PetViewerLinux
{
    public class PreCacheWindow : Window
    {
        private TextBlock _statusText;
        private ProgressBar _progressBar;

        public PreCacheWindow()
        {
            Title = "LinuxCutePet - Initializing";
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CanResize = false;
            ShowInTaskbar = true;
            Topmost = true;

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            var titleText = new TextBlock
            {
                Text = "🐱 LinuxCutePet",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            _statusText = new TextBlock
            {
                Text = "Caching animations for smooth playback...",
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            _progressBar = new ProgressBar
            {
                Height = 20,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var infoText = new TextBlock
            {
                Text = "This might take some time. Please wait...",
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            panel.Children.Add(titleText);
            panel.Children.Add(_statusText);
            panel.Children.Add(_progressBar);
            panel.Children.Add(infoText);

            Content = panel;
        }

        public void UpdateProgress(int current, int total, string currentAnimation)
        {
            var percentage = (double)current / total * 100;
            _progressBar.Value = percentage;
            _statusText.Text = $"Caching: {currentAnimation} ({current}/{total})";
        }

        public void SetCompleted()
        {
            _statusText.Text = "✅ Caching complete! Starting pet...";
            _progressBar.Value = 100;
        }
    }
}
