
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Engine;
using Engine.Audio;
using Engine.SceneManagment;
using Spacebox.Game.Player;
using Spacebox.Scenes;
using static Spacebox.Game.Resources.GameSetLoader;

namespace Spacebox.Game.GUI.Menu
{
    public class GameMenu
    {
        public static bool IsVisible = false;
        public enum MenuState { Main, WorldSelect, NewWorld, Options, Multiplayer }
        private MenuState currentState = MenuState.Main;
        private List<WorldInfo> worlds = new List<WorldInfo>();
        private List<ModConfig> gameSets = new List<ModConfig>();
        private readonly string[] gamemodes;
        private int selectedGameModeIndex = 1;
        public string newWorldName = "New World";
        public string newWorldAuthor = "";
        public string newWorldSeed = "";
        private int selectedGameSetIndex = 0;
        public WorldInfo selectedWorld = null;
        public Texture2D trashIcon;
        private Vector2 oldItemSpacing;
        private Vector2 oldWindowPadding;
        public AudioSource click1;
        public bool showDeleteWindow = false;
        public bool showVersionConvertWindow = false;
        public MainMenuWindow mainMenuWindow;
        public WorldSelectWindow worldSelectWindow;
        public NewWorldWindow newWorldWindow;
        public OptionsWindow optionsWindow;
        public MultiplayerWindow multiplayerWindow;
        public DeleteWindow deleteWindow;
        public UpdateVersionWindow updateVersionWindow;

        public GameMenu()
        {
            WorldInfoSaver.LoadWorlds(worlds);
            LoadGameSets();
            click1 = new AudioSource(SoundManager.GetClip("UI/click1"));
            trashIcon = TextureManager.GetTexture("Resources/Textures/UI/trash.png", true);
            trashIcon.FlipY();
            gamemodes = Enum.GetNames<GameMode>();
            mainMenuWindow = new MainMenuWindow(this);
            worldSelectWindow = new WorldSelectWindow(this);
            newWorldWindow = new NewWorldWindow(this);
            optionsWindow = new OptionsWindow(this);
            multiplayerWindow = new MultiplayerWindow(this);
            deleteWindow = new DeleteWindow(this);
            updateVersionWindow = new UpdateVersionWindow(this);
        }

        public void Render()
        {
            if (!IsVisible) return;
            oldItemSpacing = ImGui.GetStyle().ItemSpacing;
            oldWindowPadding = ImGui.GetStyle().WindowPadding;
            ImGui.GetStyle().ItemSpacing = Vector2.Zero;
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1f, 0.75f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1f, 0.8f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(1f, 0.72f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Theme.Colors.Deep);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            switch (currentState)
            {
                case MenuState.Main:
                    mainMenuWindow.Render();
                    break;
                case MenuState.WorldSelect:
                    worldSelectWindow.Render();
                    break;
                case MenuState.NewWorld:
                    newWorldWindow.Render();
                    break;
                case MenuState.Options:
                    optionsWindow.Render();
                    break;
                case MenuState.Multiplayer:
                    multiplayerWindow.Render();
                    break;
            }
            if (showDeleteWindow) deleteWindow.Render();
            if (showVersionConvertWindow) updateVersionWindow.Render();
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt))
            {
                foreach (var w in worlds)
                {
                    if (w.Name == "Developer")
                    {
                        selectedWorld = w;
                        click1.Play();
                        if (VersionConverter.IsVersionOld(selectedWorld.GameVersion, Application.Version))
                        {
                            showVersionConvertWindow = true;
                        }
                        else
                        {
                            LoadWorld(selectedWorld);
                        }
                    }
                }
            }
            ImGui.PopStyleColor(5);
            ImGui.GetStyle().ItemSpacing = oldItemSpacing;
            ImGui.GetStyle().WindowPadding = oldWindowPadding;
        }

        public static void DrawElementColors(Vector2 windowPos, Vector2 windowSize, float displayY, float offsetMultiplayer = 0.004f)
        {
            float offsetValue = displayY * offsetMultiplayer;
            Vector2 offset = new Vector2(offsetValue, offsetValue);
            uint backgroundColor = Theme.Colors.BackgroundUint;
            uint borderColor = Theme.Colors.BorderLightUint;
            uint borderColor2 = Theme.Colors.BorderDarkUint;
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(windowPos, windowPos + windowSize, borderColor2);
            drawList.AddRectFilled(windowPos, windowPos + windowSize - offset, borderColor);
            drawList.AddRectFilled(windowPos + offset, windowPos + windowSize - offset, backgroundColor);
        }

        public static Vector2 CenterNextWindow(float width, float height)
        {
            var pos = new Vector2((ImGui.GetIO().DisplaySize.X - width) * 0.5f, (ImGui.GetIO().DisplaySize.Y - height) * 0.5f);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height));
            return pos;
        }

        public static Vector2 CenterNextWindow2(float width, float height)
        {
            var pos = new Vector2((ImGui.GetIO().DisplaySize.X - width) * 0.5f, (ImGui.GetIO().DisplaySize.Y - height) * 0.7f);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height));
            return pos;
        }

        public static Vector2 CenterNextWindow3(float width, float height)
        {
            var pos = new Vector2((ImGui.GetIO().DisplaySize.X - width) * 0.5f, (ImGui.GetIO().DisplaySize.Y - height) * 0.22f);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height));
            return pos;
        }

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
            if (ImGui.Button(label, new Vector2(width, height))) onClick?.Invoke();
        }

        public List<WorldInfo> Worlds => worlds;
        public List<ModConfig> GameSets => gameSets;
        public string[] Gamemodes => gamemodes;
        public string NewWorldName { get => newWorldName; set => newWorldName = value; }
        public string NewWorldAuthor { get => newWorldAuthor; set => newWorldAuthor = value; }
        public string NewWorldSeed { get => newWorldSeed; set => newWorldSeed = value; }
        public int SelectedGameModeIndex { get => selectedGameModeIndex; set => selectedGameModeIndex = value; }
        public int SelectedGameSetIndex { get => selectedGameSetIndex; set => selectedGameSetIndex = value; }

        public void GenerateRandomSeedAndName()
        {
            Random r = new Random();
            newWorldSeed = r.Next(int.MinValue, int.MaxValue).ToString();
            newWorldName = GetNewWorldUniqueName();
        }

        public string GetModNameById(string id)
        {
            foreach (var m in gameSets)
            {
                if (m.ModId == id) return m.ModName;
            }
            return "WRONG MOD ID";
        }

        public bool IsNameUnique(string name)
        {
            foreach (var w in worlds)
            {
                if (w.Name == name) return false;
            }
            return true;
        }

        public string GetNewWorldUniqueName()
        {
            if (worlds.Count == 0) return "New World";
            int x = 2;
            string name = "New World";
            string nameRes = "New World";
            bool searching = true;
            while (searching)
            {
                bool found = false;
                if (!IsNameUnique(nameRes))
                {
                    nameRes = name + x;
                    x++;
                    found = true;
                }
                else return nameRes;
                if (!found) searching = false;
            }
            return nameRes;
        }

        public void CenterInputText(string label, ref string input, uint maxLength, float width, float height)
        {
            float windowWidth = ImGui.GetWindowWidth();
            Vector2 labelSize = ImGui.CalcTextSize(label);
            ImGui.SetCursorPosX((windowWidth - labelSize.X) * 0.5f);
            ImGui.Text(label);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, (height - labelSize.Y) / 2));
            ImGui.SetNextItemWidth(width);
            ImGui.SetCursorPosX((windowWidth - width) * 0.5f);
            ImGui.InputText("##" + label, ref input, maxLength);
            ImGui.PopStyleVar();
        }

        private void LoadGameSets()
        {
            gameSets.Clear();
            string gameSetsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameSets");
            if (!Directory.Exists(gameSetsDirectory)) Directory.CreateDirectory(gameSetsDirectory);
            string[] modFolders = Directory.GetDirectories(gameSetsDirectory);
            foreach (string modFolder in modFolders)
            {
                string configJsonPath = Path.Combine(modFolder, "config.json");
                if (File.Exists(configJsonPath))
                {
                    string jsonContent = File.ReadAllText(configJsonPath);
                    ModConfig gameSetInfo = JsonSerializer.Deserialize<ModConfig>(jsonContent);
                    gameSetInfo.FolderName = Path.GetFileName(modFolder);
                    if (gameSetInfo != null) gameSets.Add(gameSetInfo);
                }
            }
        }

        public void CreateNewWorld()
        {
            WorldInfo newWorld = new WorldInfo
            {
                Name = newWorldName,
                Author = newWorldAuthor,
                GameMode = (GameMode)selectedGameModeIndex,
                Seed = newWorldSeed,
                ModId = gameSets[selectedGameSetIndex].ModId,
                GameVersion = Application.Version,
                LastEditDate = WorldInfo.GetCurrentDate(),
                FolderName = newWorldName,
                ShowWelcomeWindow = true
            };
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string newWorldDirectory = Path.Combine(worldsDirectory, newWorldName);
            if (!Directory.Exists(newWorldDirectory)) Directory.CreateDirectory(newWorldDirectory);
            string worldJsonPath = Path.Combine(newWorldDirectory, "world.json");
            string jsonContent = JsonSerializer.Serialize(newWorld, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(worldJsonPath, jsonContent);
            string chunksDirectory = Path.Combine(newWorldDirectory, "Chunks");
            if (!Directory.Exists(chunksDirectory)) Directory.CreateDirectory(chunksDirectory);
            worlds.Add(newWorld);
            selectedWorld = newWorld;
        }

        public void ButtonWithBackground(string label, Vector2 size, Vector2 cursorPos, Action onClick)
        {
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

        public void ButtonWithBackgroundAndIcon(string label, Vector2 size, Vector2 cursorPos, Action onClick)
        {
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
            Vector2 imagePosMin = buttonPos + offset;
            Vector2 imagePosMax = buttonPos + size - offset;
            drawList.AddImage(trashIcon.Handle, imagePosMin, imagePosMax);
        }

        public void LoadWorld(WorldInfo world, bool multiplayer = false)
        {
            List<string> args = new List<string>();
            ModConfig modInfo = null;
            foreach (var mod in gameSets)
            {
                if (mod.ModId == world.ModId)
                {
                    modInfo = mod;
                    break;
                }
            }
            args.Add(world.Name);
            args.Add(world.ModId);
            args.Add(world.Seed);
            args.Add(modInfo.FolderName);

            if(multiplayer)
            {
                SceneManager.LoadScene(typeof(MultiplayerSpaceScene), args.ToArray());
            }
            else
            {
                SceneManager.LoadScene(typeof(LocalSpaceScene), args.ToArray());
            }
            
        }

        public void DeleteWorld(WorldInfo world)
        {
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string worldDirectory = Path.Combine(worldsDirectory, world.Name);
            if (Directory.Exists(worldDirectory)) Directory.Delete(worldDirectory, true);
            worlds.Remove(world);
            selectedWorld = null;
        }

        public void SetStateToMain() { currentState = MenuState.Main; }
        public void SetStateToWorldSelect() { currentState = MenuState.WorldSelect; }
        public void SetStateToNewWorld() { currentState = MenuState.NewWorld; }
        public void SetStateToOptions() { currentState = MenuState.Options; }
        public void SetStateToMultiplayer() { currentState = MenuState.Multiplayer; }
    }
}
