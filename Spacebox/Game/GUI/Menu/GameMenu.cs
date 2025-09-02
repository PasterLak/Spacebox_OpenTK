using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Engine;
using Engine.Audio;
using Engine.SceneManagement;
using Spacebox.Scenes;
using static Spacebox.Game.Resource.GameSetLoader;
using Spacebox.Game.Resource;
using Spacebox.GUI;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI.Menu
{

    public class GameMenu : IDisposable
    {
        public static bool IsVisible = false;

        private readonly Dictionary<System.Type, MenuWindow> windows = new();
        private System.Type currentWindowType;

        private readonly List<WorldInfo> worlds = new();
        private readonly List<ModConfig> gameSets = new();
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
        public AudioSource Click1;
        public bool showDeleteWindow = false;
        public bool showVersionConvertWindow = false;


        private DeleteWindow deleteWindow;
        private UpdateVersionWindow updateVersionWindow;

        public GameMenu()
        {
            Click1 = new AudioSource(Resources.Get<AudioClip>("Resources/Audio/UI/click1.ogg"));
            WorldInfoSaver.LoadWorlds(worlds);
            GameSetsUnpacker.UnpackMods(true);
            LoadGameSets();

            trashIcon = Resources.Load<Texture2D>("Resources/Textures/UI/trash.png");
            trashIcon.FlipY();

            gamemodes = Enum.GetNames<GameMode>();

            Reg(new MainMenuWindow(this));
            Reg(new WorldSelectWindow(this));
            Reg(new NewWorldWindow(this));
            Reg(new OptionsWindow(this));
            Reg(new MultiplayerWindow(this));
            Reg(new SettingsControlsWindow(this));
            Reg(new ControlsWindow(this));
            Reg(new GraphicsWindow(this));
            Reg(new AudioWindow(this));
            Reg(new GameWindow(this));

            deleteWindow = new DeleteWindow(this);
            updateVersionWindow = new UpdateVersionWindow(this);

            currentWindowType = typeof(MainMenuWindow);
        }

        private T Reg<T>(T window) where T : MenuWindow
        {
            windows[typeof(T)] = window;
            return window;
        }

        public void Open<T>() where T : MenuWindow
        {
            var t = typeof(T);
            if (windows.ContainsKey(t)) currentWindowType = t;
        }

        public T Get<T>() where T : MenuWindow
        {
            if (windows.TryGetValue(typeof(T), out var w)) return (T)w;
            return null;
        }

        public void Render()
        {
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt))
            {
                foreach (var w in worlds)
                {
                    if (w.Name == "Developer")
                    {
                        BlackScreenOverlay.IsEnabled = true;
                        selectedWorld = w;
                        Click1.Play();
                        if (VersionConverter.IsVersionOld(selectedWorld.GameVersion, Application.Version))
                            showVersionConvertWindow = true;
                        else
                            LoadWorld(selectedWorld);
                    }
                }
            }

            if (!IsVisible) return;

            oldItemSpacing = ImGui.GetStyle().ItemSpacing;
            oldWindowPadding = ImGui.GetStyle().WindowPadding;
            ImGui.GetStyle().ItemSpacing = Vector2.Zero;
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1f, 0.75f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1f, 0.8f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(1f, 0.72f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Theme.Colors.Deep);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));

            if (currentWindowType != null && windows.TryGetValue(currentWindowType, out var wnd))
                wnd.Render();

            if (showDeleteWindow) deleteWindow.Render();
            if (showVersionConvertWindow) updateVersionWindow.Render();

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

        public ModConfig? GetModById(string id)
        {
            foreach (var m in gameSets)
            {
                if (m.ModId == id) return m;
            }
            return null;
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
                    if (gameSetInfo != null)
                    {
                        gameSetInfo.FolderName = Path.GetFileName(modFolder);
                        gameSets.Add(gameSetInfo);
                    }
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
                Click1.Play();
                onClick?.Invoke();
            }
        }

        public static void ButtonWithBackground2(string label, Vector2 size, Vector2 cursorPos, Action onClick)
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
                Click1.Play();
                onClick?.Invoke();
            }
            Vector2 imagePosMin = buttonPos + offset;
            Vector2 imagePosMax = buttonPos + size - offset;
            drawList.AddImage(trashIcon.Handle, imagePosMin, imagePosMax);
        }

        public void LoadWorld(WorldInfo world, bool multiplayer = false)
        {
            ModConfig modInfo = null;
            foreach (var mod in gameSets)
            {
                if (mod.ModId == world.ModId) { modInfo = mod; break; }
            }

            var arg = new SpaceSceneArgs()
            {
                worldName = world.Name,
                modId = world.ModId,
                seed = world.Seed,
                modfolderName = modInfo?.FolderName ?? ""
            };

            if (multiplayer)
                SceneManager.Load<MultiplayerScene, SpaceSceneArgs>(arg);
            else
                SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(arg);
        }

        public void DeleteWorld(WorldInfo world)
        {
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string worldDirectory = Path.Combine(worldsDirectory, world.Name);
            if (Directory.Exists(worldDirectory)) Directory.Delete(worldDirectory, true);
            worlds.Remove(world);
            selectedWorld = null;
        }

        public void Dispose()
        {
            Click1.Stop();
            Click1.Clip.AudioSource = null;
            Click1.Dispose();
        }

        public List<WorldInfo> GetWorlds() => worlds;
        public List<ModConfig> GetGameSets() => gameSets;

        public void SetStateToControls() => Open<ControlsWindow>();
        public void SetStateToMain() => Open<MainMenuWindow>();
        public void SetStateToWorldSelect() => Open<WorldSelectWindow>();
        public void SetStateToNewWorld() => Open<NewWorldWindow>();
        public void SetStateToOptions() => Open<OptionsWindow>();
        public void SetStateToMultiplayer() => Open<MultiplayerWindow>();
        public void SetStateToSettingsControls() => Open<SettingsControlsWindow>();
        public void SetStateToGraphics() => Open<GraphicsWindow>();
    }
}
