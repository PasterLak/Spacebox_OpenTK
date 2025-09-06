using System;
using System.Numerics;

namespace Engine.Commands
{
    public class ColorCommand : ICommand
    {
        public string Name => "color";
        public string Description => "Changes the color of the console text. Usage: color [color_name] [message]";

        public void Execute(string[] args)
        {
            if (args.Length < 2)
            {
                Debug.Log("Usage: color [color_name] [message]", new Vector4(1f, 0f, 0f, 1f));
                return;
            }

            string colorName = args[0].ToLower();
            string message = args[1];
            Vector4 color = GetColorByName(colorName);

            if (color == Vector4.Zero)
            {
                Debug.Error($"Unknown color: {colorName}");
                return;
            }

            Debug.Log(message, color);
        }

        private Vector4 GetColorByName(string colorName)
        {
            return colorName switch
            {
                "red" => new Vector4(1f, 0f, 0f, 1f),
                "green" => new Vector4(0f, 1f, 0f, 1f),
                "blue" => new Vector4(0f, 0f, 1f, 1f),
                "yellow" => new Vector4(1f, 1f, 0f, 1f),
                "cyan" => new Vector4(0f, 1f, 1f, 1f),
                "magenta" => new Vector4(1f, 0f, 1f, 1f),
                "white" => new Vector4(1f, 1f, 1f, 1f),
                _ => Vector4.Zero,
            };
        }
    }
}
