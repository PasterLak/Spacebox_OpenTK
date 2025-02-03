using CommonLibrary;
using SpaceNetwork;
using System.IO;

using System.Net;
using System.Net.NetworkInformation;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpaceServerUI
{
    public partial class MainWindow : Window
    {
        private ServerNetwork _server;
        private CommandProcessor _commandProcessor;

        public MainWindow()
        {
            InitializeComponent();
            SetLocalIp();
            LoadConfig();
            StartServer();

            _commandProcessor.OnClear += () => { InputTextBox.Clear(); LogListBox.Items.Clear(); };

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
            catch (Exception ex)
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
            KeyTextBlock.Text = $"Game Key: {Settings.Key}";
            PortInputTextBox.Text = Settings.Port.ToString();
            KeyInputTextBox.Text = Settings.Key;
        }

        private void StartServer()
        {
            _server = new ServerNetwork(Settings.Key, Settings.Port, Settings.MaxPlayers, LogMessage);
            _commandProcessor = new CommandProcessor(_server, LogMessage);
            Task.Run(() => _server.RunMainLoop());

            PlayersHeaderTextBlock.Text = $"Players online: {0}/{Settings.MaxPlayers}";
            Title = "Server: " + Settings.Name;
        }

        private void StopServer()
        {
            _server?.Stop();
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

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartServer();
        }

        public void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogListBox.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                if (message.Contains("connected") || message.Contains("disconnected") || message.Contains("kicked"))
                {
                    RefreshPlayers();
                }
            });
        }

        private void RefreshPlayers()
        {
            PlayersPanel.Children.Clear();
            var players = _server.GetAllPlayers();

            // Обновляем заголовок с информацией об игроках
           int count = 0;

            foreach (var p in players)
            {
                count++;
                // Внешняя сетка с двумя колонками:
                // - Первая (0) занимает оставшееся пространство для цвета и текста.
                // - Вторая (1) для кнопок, авто по ширине.
                var outerGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Левая панель для информации (цветовой индикатор и ник)
                var infoPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var colorIndicator = new System.Windows.Shapes.Rectangle
                {
                    Width = 15,
                    Height = 15,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(
                            (byte)(p.Color.X * 255),
                            (byte)(p.Color.Y * 255),
                            (byte)(p.Color.Z * 255)
                        )
                    ),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 0)
                };

                var infoText = new TextBlock
                {
                    Text = $"[{p.ID}] {p.Name}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                infoPanel.Children.Add(colorIndicator);
                infoPanel.Children.Add(infoText);
                Grid.SetColumn(infoPanel, 0);

                // Правая панель для кнопок Kick и Ban
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5, 0, 5, 0)
                };

                var kickButton = new Button
                {
                    Content = "Kick",
                    Tag = p.ID,
                    Margin = new Thickness(2, 0, 2, 0),
                    Width = 40,
                    Height = 25
                };
                kickButton.Click += (s, e) =>
                {
                    int playerId = (int)((Button)s).Tag;
                    bool kicked = _server.KickPlayer(playerId);
                    if (kicked)
                        LogMessage($"[Server]: Player with ID {playerId} kicked.");
                };

                var banButton = new Button
                {
                    Content = "Ban",
                    Tag = p.ID,
                    Margin = new Thickness(2, 0, 2, 0),
                    Width = 40,
                    Height = 25
                };
                banButton.Click += (s, e) =>
                {
                    int playerId = (int)((Button)s).Tag;
                    LogMessage($"[Server]: Player with ID {playerId} banned.");
                };

                buttonPanel.Children.Add(kickButton);
                buttonPanel.Children.Add(banButton);
                Grid.SetColumn(buttonPanel, 1);

                // Собираем внешний Grid
                outerGrid.Children.Add(infoPanel);
                outerGrid.Children.Add(buttonPanel);

                // Оборачиваем строку в рамку для разделения записей
                var border = new Border
                {
                    BorderThickness = new Thickness(1, 0, 0, 1),
                    BorderBrush = System.Windows.SystemColors.ControlDarkBrush,
                    Child = outerGrid
                };

                PlayersPanel.Children.Add(border);
            }

            PlayersHeaderTextBlock.Text = $"Players online: {count}/{Settings.MaxPlayers}";
        }



    }
}
