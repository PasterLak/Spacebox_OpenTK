using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Spacebox.Scenes;
using static Spacebox.Game.GameSetLoader;

namespace Spacebox.Game.GUI
{
    public class GameMenu
    {
        public static bool IsVisible = false;
        private enum MenuState
        {
            Main,
            WorldSelect,
            NewWorld,
            Options
        }

        private MenuState currentState = MenuState.Main;

        private List<WorldInfo> worlds = new List<WorldInfo>();
        private List<ModConfig> gameSets = new List<ModConfig>();

        private string newWorldName = "New World";
        private string newWorldAuthor = "";
        private string newWorldSeed = "";
        private int selectedGameSetIndex = 0;

        private WorldInfo selectedWorld = null;

        private Vector2 oldItemSpacing;
        private Vector2 oldWindowPadding;
        private AudioSource click1;

     
        public GameMenu()
        {
            LoadWorlds();
            LoadGameSets();

            click1 = new AudioSource(SoundManager.GetClip("UI/click1"));
        }

        public void Render()
        {
            if (!IsVisible) return;

            oldItemSpacing = ImGui.GetStyle().ItemSpacing;
            oldWindowPadding = ImGui.GetStyle().WindowPadding;

            ImGui.GetStyle().ItemSpacing = Vector2.Zero;
            ImGui.GetStyle().WindowPadding = Vector2.Zero;

         
         

            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1f, 0.75f, 0f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1f, 0.8f, 0f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(1f, 0.72f, 0f, 1.0f));

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));

            switch (currentState)
            {
                case MenuState.Main:
                    RenderMainMenu();
                    break;
                case MenuState.WorldSelect:
                    RenderWorldSelectMenu();
                    break;
                case MenuState.NewWorld:
                    RenderNewWorldMenu();
                    break;
                case MenuState.Options:
                    RenderOptionsMenu();
                    break;
            }

            ImGui.PopStyleColor(6);
            ImGui.GetStyle().ItemSpacing = oldItemSpacing;
            ImGui.GetStyle().WindowPadding = oldWindowPadding;
        }

        public static void DrawElementColors(Vector2 windowPos,Vector2 windowSize, float displayY, float offsetMultiplayer = 0.004f)
        {
            float offsetValue = displayY * offsetMultiplayer;
            Vector2 offset = new Vector2(offsetValue, offsetValue);

            uint backgroundColor = ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, 1f));
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint borderColor2 = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(windowPos, windowPos + windowSize
                 , borderColor2);
            drawList.AddRectFilled(windowPos, windowPos + windowSize - offset, borderColor);

            drawList.AddRectFilled(windowPos + offset, windowPos + windowSize - offset,
                backgroundColor);
        }

        private void RenderMainMenu()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = windowSize.X * 0.15f;
            float windowHeight = windowSize.Y * 0.3f;
            var windowPos = CenterNextWindow2(windowWidth, windowHeight);

            ImGui.Begin("Main Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar );

            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            float spacing = windowHeight * 0.03f;

            DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);

            float totalButtonsHeight = buttonHeight * 3 + spacing * 2;
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 4));

            CenterButtonWithBackground("Play", buttonWidth, buttonHeight, () => currentState = MenuState.WorldSelect);
            ImGui.Dummy(new Vector2(0, spacing * 2));
            CenterButtonWithBackground("Options", buttonWidth, buttonHeight, () => currentState = MenuState.Options);
            ImGui.Dummy(new Vector2(0, spacing));
            ImGui.Dummy(new Vector2(0, (windowHeight - totalButtonsHeight) / 2));
            CenterButtonWithBackground("Exit", buttonWidth, buttonHeight , () => Window.Instance.Quit());
            ImGui.PopStyleColor(6);
            ImGui.End();
        }

        private string GetModNameById(string id)
        {
            foreach(var m in gameSets)
            {
                if (m.ModId == id) return m.ModName;
            }

            return "WRONG MOD ID";
        }

        private void RenderWorldSelectMenu()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            var windowPos = CenterNextWindow2(windowWidth, windowHeight);
            
            ImGui.Begin("Select World", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            float horizontalMargin = windowWidth * 0.06f; // Отступ от левого и правого краев (6% ширины окна)
            float verticalSpacing = windowHeight * 0.03f; // Вертикальный отступ между элементами

            float listHeight = windowHeight * 0.4f;
            float infoHeight = windowHeight * 0.25f;
            float buttonAreaHeight = windowHeight - listHeight - infoHeight - verticalSpacing * 4;

            DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);

            float contentWidth = windowWidth - horizontalMargin * 2;
            ImGui.Dummy(new Vector2(0, verticalSpacing));
         
            ImGui.SetCursorPosX(horizontalMargin);
            ImGui.BeginChild("WorldList", new Vector2(contentWidth, listHeight));

            
            for (int i = 0; i < worlds.Count; i++)
            {
                var world = worlds[i];
                bool isSelected = (selectedWorld == world);
              

                if (ImGui.Selectable($" {world.Name} ", isSelected))
                {
                    click1.Play();
                    selectedWorld = world;
                }

            }
          
            ImGui.EndChild();

           
            ImGui.Dummy(new Vector2(0, verticalSpacing));

           
            ImGui.SetCursorPosX(horizontalMargin);
            ImGui.BeginChild("WorldInfo", new Vector2(contentWidth, infoHeight ));
           
            DrawElementColors(ImGui.GetCursorPos(), new Vector2(contentWidth, infoHeight), windowSize.Y, 0.004f);
            if (selectedWorld != null)
            {
                ImGui.Text($" Name: {selectedWorld.Name}");
                ImGui.Text($" Author: {selectedWorld.Author}");
               
                ImGui.Text($" Mod: {GetModNameById(selectedWorld.ModId)}");

                bool isOldVersion = IsVersionOlder(selectedWorld.GameVersion, Application.Version);

                if (isOldVersion)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $" Version: {selectedWorld.GameVersion}");
                }
                else
                {
                    ImGui.Text($" Version: {selectedWorld.GameVersion}");
                }

                //ImGui.Text($"Last Edited: {selectedWorld.LastEditDate}");
            }

            ImGui.EndChild();

            // Отступ между информацией о мире и кнопками "Play"/"Delete"
            ImGui.Dummy(new Vector2(0, verticalSpacing));

            // Кнопки "Play" и "Delete World"
            if (selectedWorld != null)
            {
                float buttonSpacing = windowWidth * 0.02f; // Отступ между кнопками (6% ширины окна)
                float buttonWidth = (contentWidth - buttonSpacing) / 2;
                float buttonHeight = windowHeight * 0.08f;

                float buttonY = listHeight + infoHeight + verticalSpacing * 3.5f;
                float buttonStartX = horizontalMargin + (contentWidth - (buttonWidth * 2 + buttonSpacing)) / 2;

  
                ButtonWithBackground("Play", new Vector2(buttonWidth, buttonHeight),
               new Vector2(buttonStartX, buttonY + windowHeight * 0.02f), () =>
               {
                   click1.Play();
                   LoadWorld(selectedWorld);
               });


                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.25f, 0.25f, 1.0f));
                // Кнопка "Delete World"
        
                ButtonWithBackground("Delete World", new Vector2(buttonWidth, buttonHeight),
              new Vector2(buttonStartX + buttonWidth + buttonSpacing, buttonY + windowHeight * 0.02f), () =>
              {
                  click1.Play();
                  DeleteWorld(selectedWorld);
                  selectedWorld = null;
                  if (worlds.Count > 0)
                  {
                      selectedWorld = worlds[0];
                  }
              });
                ImGui.PopStyleColor(6);
                //ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.54f, 0.54f, 0.54f, 1.0f));
            }

            // Отступ между кнопками "Play"/"Delete" и кнопками "Create New World"/"Back"
            ImGui.Dummy(new Vector2(0, verticalSpacing));

            // Кнопки внизу окна
            float bottomButtonSpacing = windowWidth * 0.02f; // Отступ между кнопками (6% ширины окна)
            float bottomButtonWidth = (contentWidth - bottomButtonSpacing) / 2;
            float bottomButtonHeight = windowHeight * 0.08f;
            float bottomMargin = windowHeight * 0.05f; // Отступ от нижнего края окна

            float bottomButtonY = windowHeight - bottomButtonHeight - bottomMargin;
            float bottomButtonStartX = horizontalMargin + (contentWidth - (bottomButtonWidth * 2 + bottomButtonSpacing)) / 2;



            ButtonWithBackground("Create New World", new Vector2(bottomButtonWidth, bottomButtonHeight),
              new Vector2(bottomButtonStartX, bottomButtonY), () =>
              {
                  click1.Play();
                  Random r = new Random();
                  newWorldSeed = r.Next(int.MinValue, int.MaxValue).ToString();
                  newWorldName = GetNewWorldUniqueName();
                  currentState = MenuState.NewWorld;
              });

            ButtonWithBackground("Back", new Vector2(bottomButtonWidth, bottomButtonHeight),
             new Vector2(bottomButtonStartX + bottomButtonWidth + bottomButtonSpacing, bottomButtonY), () =>
              {
                  click1.Play();
                  currentState = MenuState.Main;
              });
           
            ImGui.End();
        }



        private void RenderNewWorldMenu()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            var windowPos = CenterNextWindow2(windowWidth, windowHeight);

            ImGui.Begin("Create New World", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            float inputWidth = windowWidth * 0.8f;
            float inputHeight = windowHeight * 0.06f;
            float buttonWidth = windowWidth * 0.4f;
            float buttonHeight = inputHeight * 1.5f;
            float spacing = windowHeight * 0.02f;
            float labelHeight = ImGui.CalcTextSize("A").Y;

            DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            // Корректировка общей высоты ввода для учета меток и полей ввода
            float totalInputHeight = (labelHeight + inputHeight + spacing) * 4;
            float topPadding = (windowHeight - totalInputHeight - buttonHeight - ImGui.GetStyle().WindowPadding.Y * 2) / 2;
            ImGui.Dummy(new Vector2(0, topPadding));

            CenterInputText("World Name", ref newWorldName, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            CenterInputText("Author", ref newWorldAuthor, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            CenterInputText("Seed", ref newWorldSeed, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));

            // Центрированная метка для GameSet
            string comboLabel = "GameSet";
            Vector2 labelSize = ImGui.CalcTextSize(comboLabel);
            ImGui.SetCursorPosX((windowWidth - labelSize.X) * 0.5f);
            ImGui.Text(comboLabel);

            // Поле выбора GameSet
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (inputHeight - labelHeight) / 2));
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.SetCursorPosX((windowWidth - inputWidth) / 2);
            if (ImGui.BeginCombo("##GameSet", gameSets[selectedGameSetIndex].ModName))
            {
                for (int i = 0; i < gameSets.Count; i++)
                {
                    bool isSelected = (i == selectedGameSetIndex);
                    if (ImGui.Selectable(gameSets[i].ModName, isSelected))
                    {
                        click1.Play();
                        selectedGameSetIndex = i;
                    }
                    if (isSelected)
                    {
                       
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopStyleVar();

            float totalButtonWidth = buttonWidth * 2 + spacing; // Используем spacing вместо ItemSpacing.X
            float bottomMargin = windowHeight * 0.05f; // Отступ от нижнего края окна (5% от высоты окна)

            // Позиционирование кнопок с учетом отступа от низа и между ними
            float buttonY = windowHeight - buttonHeight - bottomMargin;
            float buttonStartX = (windowWidth - totalButtonWidth) / 2;

            ButtonWithBackground("Create", new Vector2(buttonWidth, buttonHeight), 
                new Vector2(buttonStartX, buttonY), () =>
            {
                click1.Play();
                if (IsNameUnique(newWorldName))
                {
                    CreateNewWorld();
                    currentState = MenuState.WorldSelect;
                }
            });


            ButtonWithBackground("Cancel", new Vector2(buttonWidth, buttonHeight),
               new Vector2(buttonStartX + buttonWidth + spacing, buttonY), () =>
                {
                    click1.Play();
                    currentState = MenuState.WorldSelect;
                });

            ImGui.PopStyleColor(6);
            ImGui.End();
        }

        private bool IsNameUnique(string name)
        {
            foreach (var w in worlds)
            {
                if (w.Name == name)
                {
                    return false;
                }
            }

            return true;
        }
        private string GetNewWorldUniqueName()
        {
            if (worlds.Count == 0) return "New World";

            int x = 2;
            string name = "New World";
            string nameRes = "New World";
            bool searching = true;

            while(searching)
            {
                bool found = false;
          
                if(!IsNameUnique(nameRes))
                {
                    nameRes = name + x;
                    x++;
                    found = true;
                }
                else
                {
                    return nameRes;
                }

                if(!found)
                {
                    searching = false;
                }
            }

            return nameRes;
            
        }
        // Updated CenterInputText function
        private void CenterInputText(string label, ref string input, uint maxLength, float width, float height)
        {
            float windowWidth = ImGui.GetWindowWidth();

            // Draw the label centered
            Vector2 labelSize = ImGui.CalcTextSize(label);
            ImGui.SetCursorPosX((windowWidth - labelSize.X) * 0.5f);
            ImGui.Text(label);

            // Set the position for the input field
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (height - labelSize.Y) / 2));
            ImGui.SetNextItemWidth(width);
            ImGui.SetCursorPosX((windowWidth - width) * 0.5f);
            ImGui.InputText($"##{label}", ref input, maxLength);
            ImGui.PopStyleVar();
        }


        private void RenderOptionsMenu()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;

            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            var windowPos = CenterNextWindow2(windowWidth, windowHeight);

            ImGui.Begin("Options", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            float buttonWidth = windowWidth * 0.5f;
            float buttonHeight = windowHeight * 0.1f;

            DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);

            ImGui.SetCursorPosY(windowHeight - buttonHeight - ImGui.GetStyle().WindowPadding.Y * 2);
            ImGui.SetCursorPosX((windowWidth - buttonWidth) / 2);

            if (ImGui.Button("Back", new Vector2(buttonWidth, buttonHeight)))
            {
                click1.Play();
                currentState = MenuState.Main;
            }

            ImGui.End();
        }

        private void LoadWorlds()
        {
            worlds.Clear();

            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");

            if (!Directory.Exists(worldsDirectory))
            {
                Directory.CreateDirectory(worldsDirectory);
            }

            string[] worldFolders = Directory.GetDirectories(worldsDirectory);

            foreach (string worldFolder in worldFolders)
            {
                string worldJsonPath = Path.Combine(worldFolder, "world.json");
                if (File.Exists(worldJsonPath))
                {
                    string jsonContent = File.ReadAllText(worldJsonPath);
                    WorldInfo worldInfo = JsonSerializer.Deserialize<WorldInfo>(jsonContent);
                    if (worldInfo != null)
                    {
                        worlds.Add(worldInfo);
                    }
                }
            }
        }

        private void LoadGameSets()
        {
            gameSets.Clear();

            string gameSetsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameSets");

            if (!Directory.Exists(gameSetsDirectory))
            {
                Directory.CreateDirectory(gameSetsDirectory);
            }

            string[] modFolders = Directory.GetDirectories(gameSetsDirectory);

            foreach (string modFolder in modFolders)
            {
                string configJsonPath = Path.Combine(modFolder, "config.json");
                if (File.Exists(configJsonPath))
                {
                    string jsonContent = File.ReadAllText(configJsonPath);
                    ModConfig gameSetInfo = JsonSerializer.Deserialize<ModConfig>(jsonContent);
                    gameSetInfo.FolderName = Path.GetFileName(modFolder);

                    if (gameSetInfo != null)
                    {
                        gameSets.Add(gameSetInfo);
                    }
                }
            }
        }

        private void CreateNewWorld()
        {
            WorldInfo newWorld = new WorldInfo
            {
                Name = newWorldName,
                Author = newWorldAuthor,
                Seed = newWorldSeed,
                ModId = gameSets[selectedGameSetIndex].ModId,
                GameVersion = Application.Version,
                LastEditDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string newWorldDirectory = Path.Combine(worldsDirectory, newWorldName);

            if (!Directory.Exists(newWorldDirectory))
            {
                Directory.CreateDirectory(newWorldDirectory);
            }

            string worldJsonPath = Path.Combine(newWorldDirectory, "world.json");
            string jsonContent = JsonSerializer.Serialize(newWorld, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(worldJsonPath, jsonContent);

            string chunksDirectory = Path.Combine(newWorldDirectory, "Chunks");
            if (!Directory.Exists(chunksDirectory))
            {
                Directory.CreateDirectory(chunksDirectory);
            }

            /*string playerJsonPath = Path.Combine(newWorldDirectory, "player.json");
            PlayerInfo playerInfo = new PlayerInfo
            {
            };
            string playerJsonContent = JsonSerializer.Serialize(playerInfo, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(playerJsonPath, playerJsonContent);
            */
            worlds.Add(newWorld);
            selectedWorld = newWorld;
        }

        private void LoadWorld(WorldInfo world)
        {
            List<string> args = new List<string>();

            ModConfig modInfo = null;

            foreach(var mod in gameSets)
            {
                if(mod.ModId == world.ModId)
                {
                    modInfo = mod;
                    break;
                }
            }


            args.Add(world.Name);
            args.Add( world.ModId);
            args.Add( world.Seed);
            args.Add(modInfo.FolderName);

            SceneManager.LoadScene(typeof(SpaceScene), args.ToArray());
        }

        private void DeleteWorld(WorldInfo world)
        {
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string worldDirectory = Path.Combine(worldsDirectory, world.Name);

            if (Directory.Exists(worldDirectory))
            {
                Directory.Delete(worldDirectory, true);
            }

            worlds.Remove(world);
            selectedWorld = null;

           
        }

        public static Vector2 CenterNextWindow(float width, float height)
        {
            var pos = new Vector2(
                (ImGui.GetIO().DisplaySize.X - width) * 0.5f,
                (ImGui.GetIO().DisplaySize.Y - height) * 0.5f);
            ImGui.SetNextWindowPos(pos,
                ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height));

            return pos;
        }

        public static Vector2 CenterNextWindow2(float width, float height)
        {
            var pos = new Vector2(
                (ImGui.GetIO().DisplaySize.X - width) * 0.5f,
                (ImGui.GetIO().DisplaySize.Y - height) * 0.7f);
            ImGui.SetNextWindowPos(pos,
                ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height));

            return pos;
        }

        private void CenterButton(string label, float width, float height, Action onClick)
        {
            float windowWidth = ImGui.GetWindowWidth();
            ImGui.SetCursorPosX((windowWidth - width) * 0.5f);
            if (ImGui.Button(label, new Vector2(width, height)))
            {
                click1.Play();
                onClick();
            }
        }

        public void ButtonWithBackground(string label, Vector2 size, Vector2 cursorPos,Action onClick)
        {
            float windowWidth = ImGui.GetWindowWidth();
        
            ImGui.SetCursorPos(cursorPos);

            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = size.Y * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);

       
            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            var drawList = ImGui.GetWindowDrawList();



            drawList.AddRectFilled(buttonPos - offset, buttonPos + size + offset, borderColor);
            drawList.AddRectFilled(buttonPos, buttonPos + size + offset, lightColor);

            if (ImGui.Button(label, size))
            {
                click1.Play();
                onClick?.Invoke();
            }
        }
        /*
        uint backgroundColor = ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, 1f));
        uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
        uint borderColor2 = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));*/
        public static void CenterButtonWithBackground(string label, float width, float height, Action onClick)
        {
            float windowWidth = ImGui.GetWindowWidth();
            float cursorX = (windowWidth - width) * 0.5f;
            ImGui.SetCursorPosX(cursorX);

            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);

            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            var drawList = ImGui.GetWindowDrawList();

         
            
            drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
            drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height) + offset, lightColor);

            if (ImGui.Button(label, new Vector2(width, height)))
            {
                //click1.Play(); 
                onClick?.Invoke();
            }
        }







        private bool IsVersionOlder(string worldVersion, string currentVersion)
        {
            Version worldVer, currentVer;
            if (Version.TryParse(worldVersion, out worldVer) && Version.TryParse(currentVersion, out currentVer))
            {
                return worldVer < currentVer;
            }
            return false;
        }
    }

    public class WorldInfo
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Seed { get; set; }
        public string ModId { get; set; }
        public string GameVersion { get; set; }
        public string LastEditDate { get; set; }
        public string FolderName { get; set; } = "";
        /*
        public static bool operator == (WorldInfo left, WorldInfo right)
        {
            return left.Name == right.Name;
        }

        public static bool operator !=(WorldInfo left, WorldInfo right)
        {
            return !(left == right);
        }*/
    }

    public class PlayerInfo
    {
    }

    public static class Application
    {
        public const string Version = "0.0.8";
        public const string Author = "PasterLak";
        public const string EngineVersion = "1.3";
    }

    public static class ImGuiExtensions
    {
        public static void SetNextItemHeight(this ImGuiIOPtr io, float height)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (height - ImGui.CalcTextSize("A").Y) / 2));
        }
    }
}
