using System;
using System.IO;
using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public static class PlayerSaveLoadManager
    {
        private static readonly string SaveFileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "Chunks");
        private static readonly string SaveFilePath = Path.Combine(SaveFileDirectory, "player.json");
        /// <summary>
        /// Saves the player's position and rotation to a JSON file.
        /// </summary>
        /// <param name="player">The Astronaut instance representing the player.</param>
        public static void SavePlayer(Astronaut player)
        {
            try
            {

                if(Directory.Exists(SaveFileDirectory)) {
                    Directory.CreateDirectory(SaveFileDirectory);
                }
                PlayerData data = new PlayerData
                {
                    PositionX = player.Position.X,
                    PositionY = player.Position.Y,
                    PositionZ = player.Position.Z,
                    RotationX = player.GetRotation().X,
                    RotationY = player.GetRotation().Y,
                    RotationZ = player.GetRotation().Z,
                    RotationW = player.GetRotation().W
                };

                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SaveFilePath, jsonString);
                Debug.Log($"Player data saved at {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.DebugError($"Error saving player data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the player's position and rotation from a JSON file and applies it to the Astronaut instance.
        /// </summary>
        /// <param name="player">The Astronaut instance representing the player.</param>
        public static void LoadPlayer(Astronaut player)
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    Debug.Log("Player save file does not exist.");
                    return;
                }

                string jsonString = File.ReadAllText(SaveFilePath);
                PlayerData data = JsonSerializer.Deserialize<PlayerData>(jsonString);

                if (data == null)
                {
                    Debug.DebugError("Failed to deserialize player data.");
                    return;
                }

                // Apply loaded position
                player.Position = new Vector3(data.PositionX, data.PositionY, data.PositionZ);

                // Reconstruct the rotation quaternion
                Quaternion loadedRotation = new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW);
                player.SetRotation(loadedRotation);

                Debug.Log("Player data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.DebugError($"Error loading player data: {ex.Message}");
            }
        }

        /// <summary>
        /// A helper class for serialization containing player's position and rotation as primitive types.
        /// </summary>
        private class PlayerData
        {
            // Position components
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }

            // Rotation components (Quaternion: X, Y, Z, W)
            public float RotationX { get; set; }
            public float RotationY { get; set; }
            public float RotationZ { get; set; }
            public float RotationW { get; set; }
        }
    }
}
