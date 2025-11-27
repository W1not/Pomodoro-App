using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;




namespace Pomodoro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TimeSpan _defaultWorkDuration = TimeSpan.FromMinutes(25);
        private readonly TimeSpan _defaultBreakDuration = TimeSpan.FromMinutes(5);

        private TimeSpan _remainingTime;
        private TimeSpan _currentPhaseTotal;
        private bool _isWorkPhase = true;
        private bool _isRunning = false;
        private int _pomodorosCompletados = 0;

        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            SetWorkPhase();
            SetIdleColor();
            UpdateUI();

        }

        string iconPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "icon_multi.ico"
);

        private void ShowToast(string title, string message)
        {
            new ToastContentBuilder()
                .AddAppLogoOverride(new Uri(iconPath))
                .AddText(title)
                .AddText(message)
                .Show();
        }

        private TimeSpan GetWorkDuration()
        {
            if (int.TryParse(WorkMinutesInput.Text, out int minutes) && minutes > 0)
                return TimeSpan.FromMinutes(minutes);

            return TimeSpan.FromMinutes(25);
        }

        private TimeSpan GetBreakDuration()
        {
            if (int.TryParse(BreakMinutesInput.Text, out int minutes) && minutes > 0)
                return TimeSpan.FromMinutes(minutes);

            return TimeSpan.FromMinutes(5);
        }

        private TimeSpan GetLongBreakDuration()
        {
            if (int.TryParse(LongBreakMinutesInput.Text, out int minutes) && minutes > 0)
                return TimeSpan.FromMinutes(minutes);

            return TimeSpan.FromMinutes(15);
        }

        private int GetLongBreakInterval()
        {
            if (int.TryParse(LongBreakIntervalInput.Text, out int interval) && interval > 0)
                return interval;

            return 4;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingTime > TimeSpan.Zero)
            {
                _remainingTime -= TimeSpan.FromSeconds(1);
                UpdateUI();
            }
            else
            {
                _timer.Stop();
                _isRunning = false;

                if (_isWorkPhase)
                {
                    _pomodorosCompletados++;
                    PomodoroCountText.Text = $"Pomodoros completados: {_pomodorosCompletados}";

                    ShowToast("Pomodoro completado", $"Has completado {_pomodorosCompletados} pomodoros");

                    // IR A DESCANSO
                    SetBreakPhase();
                }
                else
                {

                    ShowToast("Iniciando fase de trabajo", "Enfoquémonos en esta sesión. ¡Tú puedes!");

                    // IR A TRABAJO
                    SetWorkPhase();
                }

            }
        }


        private void SetWorkPhase()
        {
            
            _isWorkPhase = true;
            _currentPhaseTotal = GetWorkDuration();
            _remainingTime = _currentPhaseTotal;
            PhaseText.Text = "Fase: Trabajo";
            UpdateUI();
        }


        private void SetBreakPhase()
        {
            _isWorkPhase = false;

            int interval = GetLongBreakInterval();
            bool longBreak = (_pomodorosCompletados % interval == 0);

            if (longBreak)
            {
                _currentPhaseTotal = GetLongBreakDuration();
                PhaseText.Text = "Fase: Descanso largo";
                SetLongBreakColor();
            }
            else
            {
                _currentPhaseTotal = GetBreakDuration();
                PhaseText.Text = "Fase: Descanso";
                SetBreakRunningColor();
            }

            _remainingTime = _currentPhaseTotal;
            UpdateUI();
        }



        private void UpdateUI()
        {
            TimeText.Text = $"{_remainingTime.Minutes:D2}:{_remainingTime.Seconds:D2}";

            double progress = 1.0 - (_remainingTime.TotalSeconds / _currentPhaseTotal.TotalSeconds);
            if (double.IsNaN(progress) || double.IsInfinity(progress))
                progress = 0;

            Progress.Value = progress;
        }
        private void AnimateBackgroundTo(string hexColor, double durationSeconds = 0.6)
        {
            var newColor = (Color)ColorConverter.ConvertFromString(hexColor);

            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            StateBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        private void SetIdleColor()
        {
            AnimateBackgroundTo("#011627");
        }

        private void SetWorkRunningColor()
        {
            AnimateBackgroundTo("#17615A");
        }

        private void SetPauseColor()
        {
            AnimateBackgroundTo("#A05E00");
        }

        private void SetBreakRunningColor()
        {
            AnimateBackgroundTo("#971020");
        }

        private void SetLongBreakColor()
        {
            AnimateBackgroundTo("#A05E00"); 
        }


        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                _timer.Start();
                _isRunning = true;
                if (_isWorkPhase)
                    SetWorkRunningColor();
                else
                    SetBreakRunningColor();
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
                SetPauseColor();
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;

            SetIdleColor();
            SetWorkPhase();
            UpdateUI();
        }

        private bool _isConfigOpen = false;

        private void ToggleConfigPanel()
        {
            if (!_isConfigOpen)
            {
                // Mostrar overlay
                Overlay.Visibility = Visibility.Visible;
                Storyboard overlayIn = (Storyboard)FindResource("ShowOverlay");
                overlayIn.Begin();

                // Mostrar panel
                ConfigPanel.Visibility = Visibility.Visible;
                Storyboard panelIn = (Storyboard)FindResource("ShowConfigPanelStoryboard");
                panelIn.Begin(ConfigPanel);

                _isConfigOpen = true;
            }
            else
            {
                // Ocultar overlay
                Storyboard overlayOut = (Storyboard)FindResource("HideOverlay");
                overlayOut.Completed += (s, e) =>
                {
                    Overlay.Visibility = Visibility.Collapsed;
                };
                overlayOut.Begin();

                // Ocultar panel
                Storyboard panelOut = (Storyboard)FindResource("HideConfigPanelStoryboard");
                panelOut.Completed += (s, e) =>
                {
                    ConfigPanel.Visibility = Visibility.Collapsed;
                };
                panelOut.Begin(ConfigPanel);

                _isConfigOpen = false;
            }
        }



        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleConfigPanel();
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleConfigPanel();
        }



    }
}