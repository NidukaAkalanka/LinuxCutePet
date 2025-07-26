using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PetViewerLinux
{
    public partial class DanceSettingsWindow : Window
    {
        private CheckBox? _danceCheckBox;
        private Button? _okButton;
        private Button? _cancelButton;
        
        public bool IsDanceEnabled { get; private set; }
        public bool DialogResult { get; private set; } = false;

        public DanceSettingsWindow()
        {
            InitializeComponent();
            
            _danceCheckBox = this.FindControl<CheckBox>("DanceCheckBox");
            _okButton = this.FindControl<Button>("OkButton");
            _cancelButton = this.FindControl<Button>("CancelButton");
            
            if (_okButton != null)
            {
                _okButton.Click += OnOkButtonClick;
            }
            
            if (_cancelButton != null)
            {
                _cancelButton.Click += OnCancelButtonClick;
            }
        }

        public DanceSettingsWindow(bool currentDanceEnabled) : this()
        {
            if (_danceCheckBox != null)
            {
                _danceCheckBox.IsChecked = currentDanceEnabled;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnOkButtonClick(object? sender, RoutedEventArgs e)
        {
            if (_danceCheckBox != null)
            {
                IsDanceEnabled = _danceCheckBox.IsChecked ?? false;
                DialogResult = true;
            }
            Close();
        }

        private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
