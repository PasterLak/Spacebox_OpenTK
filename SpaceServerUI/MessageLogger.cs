using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServerCommon;

namespace SpaceServerUI
{
    public static class MessageLogger
    {
        public static ListBoxItem CreateLogItem(string message, LogType type)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss} - {message}";
            TextBlock tb = new TextBlock
            {
                Text = formattedMessage,
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetBrushForLogType(type),
                
            };
            return new ListBoxItem { Content = tb };
        }

        private static Brush GetBrushForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                case LogType.Success:
                    return Brushes.Green;
                case LogType.Warning:
                    return new SolidColorBrush(Color.FromRgb(200, 100, 0));
                case LogType.Info:
                    return new SolidColorBrush(Color.FromRgb(255, 170, 0));
                default:
                    return Brushes.Black;
            }
        }
    }
}
