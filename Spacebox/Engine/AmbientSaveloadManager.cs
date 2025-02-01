using System.Text.Json;
using OpenTK.Mathematics;


namespace Spacebox.Engine
{
    public static class AmbientSaveLoadManager
    {
        private static readonly string SaveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ambient.json");


        public static void SaveAmbient()
        {
            try
            {
                AmbientData data = new AmbientData
                {
                    AmbientColorX = Lighting.AmbientColor.X,
                    AmbientColorY = Lighting.AmbientColor.Y,
                    AmbientColorZ = Lighting.AmbientColor.Z,
                    BackgroundColorR = Lighting.BackgroundColor.R,
                    BackgroundColorG = Lighting.BackgroundColor.G,
                    BackgroundColorB = Lighting.BackgroundColor.B,
                    BackgroundColorA = Lighting.BackgroundColor.A
                };

                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SaveFilePath, jsonString);
                //Debug.WriteLine($"Ambient data saved at {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error saving ambient data: {ex.Message}");
            }
        }

    
        public static void LoadAmbient()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    //Console.WriteLine("Ambient save file does not exist.");
                    return;
                }

                string jsonString = File.ReadAllText(SaveFilePath);
                AmbientData data = JsonSerializer.Deserialize<AmbientData>(jsonString);

                if (data == null)
                {
                    Debug.Error("Failed to deserialize ambient data.");
                    return;
                }

               
                Lighting.AmbientColor = new Vector3(data.AmbientColorX, data.AmbientColorY, data.AmbientColorZ);
                //Lighting.BackgroundColor = Color.FromArgb(data.BackgroundColorA, data.BackgroundColorR, data.BackgroundColorG, data.BackgroundColorB);

                //Console.WriteLine("Ambient data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.Error($"Error loading ambient data: {ex.Message}");
            }
        }

        private class AmbientData
        {
          
            public float AmbientColorX { get; set; }
            public float AmbientColorY { get; set; }
            public float AmbientColorZ { get; set; }

           
            public byte BackgroundColorA { get; set; }
            public byte BackgroundColorR { get; set; }
            public byte BackgroundColorG { get; set; }
            public byte BackgroundColorB { get; set; }
        }
    }
}
