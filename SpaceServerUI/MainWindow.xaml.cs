using CommonLibrary;
using SpaceNetwork;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpaceServerUI
{
    public partial class MainWindow : Window
    {
        private ServerNetwork _server;
        private CommandProcessor _commandProcessor;
        private bool _serverStarted;

        public MainWindow()
        {
            InitializeComponent();
            SetLocalIp();
            LoadConfig();
        }

        private void SetLocalIp()
        {
            string localIP = "Not found";
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in networkInterfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        var ipProps = ni.GetIPProperties();
                        var ipv4 = ipProps.UnicastAddresses
                            .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
                            .Select(a => a.Address)
                            .FirstOrDefault();
                        if (ipv4 != null)
                        {
                            localIP = ipv4.ToString();
                            break;
                        }
                    }
                }
                LocalIpTextBlock.Text = $"Local IP: {localIP}";
            }
            catch (System.Exception ex)
            {
                LocalIpTextBlock.Text = "Local IP: Not found";
                LogMessage($"[Server]: Error getting local IP: {ex.Message}");
            }
        }

        private void LoadConfig()
        {
            ConfigManager.LoadConfig();
            Title = "Server: " + Settings.Name;
            PortTextBlock.Text = $"Port: {Settings.Port}";
            KeyTextBlock.Text = $"Key: {Settings.Key}";
            NameTextBlock.Text =  Settings.Name;
            PortInputTextBox.Text = Settings.Port.ToString();
            KeyInputTextBox.Text = Settings.Key;
            NameInputTextBox.Text = Settings.Name;
        }

        private void StartServer()
        {
            LoadConfig();
            _server = new ServerNetwork(Settings.Key, Settings.Port, Settings.MaxPlayers, LogMessage);
            _commandProcessor = new CommandProcessor(_server, LogMessage);
            Task.Run(() => _server.RunMainLoop());
            PlayersHeaderTextBlock.Text = $"Players online: 0/{Settings.MaxPlayers}";
            Title = "Server: " + Settings.Name;
            _serverStarted = true;
            StartRestartButton.Content = "Restart Server";
            StartRestartButton.Background = Brushes.Red;
            StopButton.Visibility = Visibility.Visible;
            _commandProcessor.OnClear += () => { InputTextBox.Clear(); LogListBox.Items.Clear(); };
        }

        private void StopServer()
        {
            PlayersHeaderTextBlock.Text = $"Players online: 0/0";
            _server?.Stop();
            _serverStarted = false;
            StopButton.Visibility = Visibility.Collapsed;
            StartRestartButton.Content = "Start Server";
            StartRestartButton.Background = Brushes.Green;
            LogMessage("[Server]: Server stopped.");
        }

        private void RestartServer()
        {
            StopServer();
            StartServer();
            LogMessage("[Server]: Server restarted.");
        }

        private void ProcessInput()
        {
            var input = InputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                _commandProcessor.ProcessCommand(input);
                InputTextBox.Clear();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessInput();
                e.Handled = true;
            }
        }

        private void SavePortButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PortInputTextBox.Text.Trim(), out int newPort))
            {
                Settings.Port = newPort;
                ConfigManager.SaveConfig();
                PortTextBlock.Text = $"Port: {Settings.Port}";
                LogMessage($"[Server]: Port changed to {Settings.Port} and saved to config.");
            }
            else
            {
                LogMessage("[Server]: Invalid port value.");
            }
        }

        private void SaveKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var newKey = KeyInputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newKey))
            {
                Settings.Key = newKey;
                ConfigManager.SaveConfig();
                KeyTextBlock.Text = $"Game Key: {Settings.Key}";
                LogMessage($"[Server]: Game key changed to {Settings.Key} and saved to config.");
            }
            else
            {
                LogMessage("[Server]: Invalid key value.");
            }
        }

        private void SaveNameButton_Click(object sender, RoutedEventArgs e)
        {
            var newName = NameInputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                Settings.Name = newName;
                ConfigManager.SaveConfig();
                NameTextBlock.Text = Settings.Name;
                Title = "Server: " + Settings.Name;
                LogMessage($"[Server]: Server name changed to {Settings.Name} and saved to config.");
            }
            else
            {
                LogMessage("[Server]: Invalid name value.");
            }
        }

        private void StartRestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_serverStarted)
            {
                StartServer();
                LogMessage("[Server]: Server started.");
            }
            else
            {
                RestartServer();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        public void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogListBox.Items.Add($"{System.DateTime.Now:HH:mm:ss} - {message}");
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                if (message.Contains("connected") || message.Contains("disconnected") || message.Contains("kicked"))
                {
                    RefreshPlayers();
                }
            });
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
        }


        private void RefreshPlayers()
        {
            PlayersPanel.Children.Clear();
            var players = _server.GetAllPlayers();
            int count = 0;
            foreach (var p in players)
            {
                count++;
                var outerGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var colorIndicator = new System.Windows.Shapes.Rectangle
                {
                    Width = 15,
                    Height = 15,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(p.Color.X * 255), (byte)(p.Color.Y * 255), (byte)(p.Color.Z * 255))),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 0)
                };
                var infoText = new TextBlock { Text = $"[{p.ID}] {p.Name}", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                infoPanel.Children.Add(colorIndicator);
                infoPanel.Children.Add(infoText);
                Grid.SetColumn(infoPanel, 0);
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5, 0, 5, 0) };
                var kickButton = new Button { Content = "Kick", Tag = p.ID, Margin = new Thickness(2, 0, 2, 0), Width = 40, Height = 25 };
                kickButton.Click += (s, e) =>
                {
                    int playerId = (int)((Button)s).Tag;
                    bool kicked = _server.KickPlayer(playerId);
                    if (kicked)
                        LogMessage($"[Server]: Player with ID {playerId} kicked.");
                };
                var banButton = new Button { Content = "Ban", Tag = p.ID, Margin = new Thickness(2, 0, 2, 0), Width = 40, Height = 25 };
                banButton.Click += (s, e) =>
                {
                    int playerId = (int)((Button)s).Tag;
                    LogMessage($"[Server]: Player with ID {playerId} banned.");
                };
                buttonPanel.Children.Add(kickButton);
                buttonPanel.Children.Add(banButton);
                Grid.SetColumn(buttonPanel, 1);
                outerGrid.Children.Add(infoPanel);
                outerGrid.Children.Add(buttonPanel);
                var border = new Border { BorderThickness = new Thickness(1, 0, 0, 1), BorderBrush = SystemColors.ControlDarkBrush, Child = outerGrid };
                PlayersPanel.Children.Add(border);
            }
            PlayersHeaderTextBlock.Text = $"Players online: {count}/{Settings.MaxPlayers}";
        }
    }
}
