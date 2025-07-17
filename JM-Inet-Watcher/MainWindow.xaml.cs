using System;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms; // For NotifyIcon
using Microsoft.Win32; // For registry

namespace JM_Inet_Watcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private const string ServiceName = "INetClockTool";
        private NotifyIcon _notifyIcon;
        private bool _isExit;
        private const string StartupKey = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "JM-Inet-Watcher";

        public MainWindow()
        {
            InitializeComponent();
            SetupNotifyIcon();
            SetStartup();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            CheckAndUpdateServiceStatus();
            Loaded += (s, e) => MinimizeToTray();
        }

        private void SetupNotifyIcon()
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons8-clock-80.ico");
            System.Drawing.Icon trayIcon = null;
            if (System.IO.File.Exists(iconPath))
            {
                trayIcon = new System.Drawing.Icon(iconPath);
            }
            _notifyIcon = new NotifyIcon
            {
                Icon = trayIcon ?? System.Drawing.SystemIcons.Information,
                Visible = true,
                Text = "INetClockTool Watcher"
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => 
            {
                Dispatcher.BeginInvoke(new Action(() => RestoreFromTray()));
            });
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            _notifyIcon.ContextMenuStrip = menu;
        }

        private void MinimizeToTray()
        {
            Hide();
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            Activate();
            Focus();
        }

        private void ExitApp()
        {
            _isExit = true;
            if (_notifyIcon != null)
                _notifyIcon.Dispose();
            Close();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
                MinimizeToTray();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
            else
            {
                if (_notifyIcon != null)
                    _notifyIcon.Dispose();
            }
            base.OnClosing(e);
        }

        private void SetStartup()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            using (var key = Registry.CurrentUser.OpenSubKey(StartupKey, true) ?? Registry.CurrentUser.CreateSubKey(StartupKey))
            {
                if (key != null)
                    key.SetValue(AppName, '"' + exePath + '"');
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckAndUpdateServiceStatus();
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var pageLoadAnimation = (Storyboard)FindResource("PageLoadAnimation");
            pageLoadAnimation.Begin(MainGrid);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }

        private void ServiceStatusLabel_Loaded(object sender, RoutedEventArgs e)
        {
            var statusAnimation = (Storyboard)FindResource("StatusChangeAnimation");
            statusAnimation.Begin(ServiceStatusLabel);
        }

        private void UpdateStatusLabelWithAnimation(string statusText)
        {
            ServiceStatusLabel.Opacity = 0;
            ServiceStatusLabel.Text = statusText;
            
            // Update status indicator color and start pulse animation
            UpdateStatusIndicator(statusText);
            
            var statusAnimation = (Storyboard)FindResource("StatusChangeAnimation");
            statusAnimation.Begin(ServiceStatusLabel);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            // Use Dispatcher to ensure UI thread execution
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Force show and restore
                    if (!IsVisible || WindowState == WindowState.Minimized)
                    {
                        Show();
                        WindowState = WindowState.Normal;
                        ShowInTaskbar = true;
                    }
                    
                    // Bring to front with multiple methods
                    Activate();
                    Focus();
                    
                    // Force to top temporarily
                    bool wasTopmost = Topmost;
                    Topmost = true;
                    Topmost = wasTopmost;
                    
                    // Additional focus methods
                    BringIntoView();
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    System.Diagnostics.Debug.WriteLine($"NotifyIcon restore error: {ex.Message}");
                }
            }));
        }

        private void UpdateStatusIndicator(string statusText)
        {
            var statusIndicator = FindName("StatusIndicator") as Border;
            var statusDot = FindName("StatusDot") as Ellipse;
            var pulseAnimation = (Storyboard)FindResource("PulseAnimation");
            
            if (statusIndicator != null && statusDot != null)
            {
                pulseAnimation.Stop(statusIndicator);
                
                if (statusText.Contains("Running"))
                {
                    var successBrush = (SolidColorBrush)FindResource("SuccessBrush");
                    statusIndicator.Background = successBrush;
                    statusDot.Fill = successBrush;
                    ServiceStatusLabel.Style = (Style)FindResource("StatusRunningStyle");
                    pulseAnimation.Begin(statusIndicator, true);
                }
                else if (statusText.Contains("Stopped"))
                {
                    var errorBrush = (SolidColorBrush)FindResource("ErrorBrush");
                    statusIndicator.Background = errorBrush;
                    statusDot.Fill = errorBrush;
                    ServiceStatusLabel.Style = (Style)FindResource("StatusStoppedStyle");
                }
                else
                {
                    var warningBrush = (SolidColorBrush)FindResource("WarningBrush");
                    statusIndicator.Background = warningBrush;
                    statusDot.Fill = warningBrush;
                    ServiceStatusLabel.Style = (Style)FindResource("StatusUnknownStyle");
                }
            }
        }

        private void CheckAndUpdateServiceStatus()
        {
            string statusText = "Unknown";
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    statusText = sc.Status.ToString();
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        statusText = sc.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                statusText = $"Error: {ex.Message}";
            }
            UpdateStatusLabelWithAnimation(statusText);
            LastCheckedLabel.Text = $"Last Checked: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            StartServiceManually();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CheckAndUpdateServiceStatus();
        }

        private void StartServiceManually()
        {
            string statusText = "Unknown";
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                    statusText = sc.Status.ToString();
                }
            }
            catch (Exception ex)
            {
                statusText = $"Error: {ex.Message}";
            }
            UpdateStatusLabelWithAnimation(statusText);
            LastCheckedLabel.Text = $"Last Checked: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
    }
}