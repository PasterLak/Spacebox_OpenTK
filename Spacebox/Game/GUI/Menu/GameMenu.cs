using Engine.Graphics.Atlas;
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Engine;
using Engine.Audio;
using Engine.SceneManagement;
using Spacebox.Scenes;
using static Spacebox.Game.Resource.GameSetLoader;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;
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
        private readonly List<ModConfig> allGameSets = new();
        private readonly List<InstalledGameSet> installedGameSets = new();
        public readonly List<string> NewWorldExtensions = new();
        private readonly string[] gamemodes;
        private int selectedGameModeIndex = 1;
        public string newWorldName = "New World";
        public string newWorldAuthor = "";
        public string newWorldSeed = "";
        private int selectedGameSetIndex = 0;
        public WorldInfo selectedWorld = null;
        public AtlasSprite TrashIcon => UIAtlas.Get("trash");
        public AudioSource Click1;
        public bool showDeleteWindow = false;
        public bool showVersionConvertWindow = false;
        public UpdateVersionWindow VersionWindow => updateVersionWindow;
        public bool IsEditMode = false;


        private DeleteWindow deleteWindow;
        private UpdateVersionWindow updateVersionWindow;
        private readonly MenuMessageWindow messageWindow = new MenuMessageWindow();

        public GameMenu()
        {
            Click1 = new AudioSource(Resources.Load<AudioClip>("Resources/Audio/UI/click1.ogg", true), AudioChannel.UI);
            WorldInfoSaver.LoadWorlds(worlds);
            GameSetsUnpacker.UnpackMods(true);
            LoadGameSets();


            gamemodes = Enum.GetNames<GameMode>();

            Reg(new MainMenuWindow(this));
            Reg(new WorldSelectWindow(this));
            Reg(new NewWorldWindow(this));
            Reg(new MultiplayerWindow(this));
            Reg(new SettingsControlsWindow(this));
            Reg(new OptionsHostWindow(this));
            Reg(new ModsWindow(this));
            Reg(new ShipViewerWindow(this));
            Reg(new TexturePacksWindow(this));

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
            if (windows.ContainsKey(t))
            {
                if (currentWindowType != null && currentWindowType != t && windows.TryGetValue(currentWindowType, out var previous))
                    previous.OnWindowClosed();
                currentWindowType = t;
                windows[t].OnWindowChanged();

            }

        }

        public T Get<T>() where T : MenuWindow
        {
            if (windows.TryGetValue(typeof(T), out var w)) return (T)w;
            return null;
        }


        bool hide = false;

        public bool IsHidden => hide;

        public Spacebox.Game.GUI.MenuUi.MenuPage PageOf(System.Type windowType)
        {
            return windowType != null && windows.TryGetValue(windowType, out var window) ? window.Page : null;
        }

        public void Render()
        {
            if (hide) return;

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt))
            {
                foreach (var w in worlds)
                {
                    if (w.Name == "Developer")
                    {
                        BlackScreenOverlay.IsEnabled = true;
                        selectedWorld = w;
                        Click1.Play();
                        OpenWorld(selectedWorld);
                    }
                }
            }

            if (!IsVisible) return;

            using (MenuStyle.Begin())
            {
                if (currentWindowType != null && windows.TryGetValue(currentWindowType, out var wnd) && wnd.Page == null)
                    wnd.Render();

                bool canvasDialogs = Spacebox.Game.GUI.MenuUi.MenuCanvas.Current != null;
                if (showDeleteWindow && !canvasDialogs) deleteWindow.Render();
                if (showVersionConvertWindow && !canvasDialogs) updateVersionWindow.Render();
                messageWindow.Render(this);
            }
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
        public List<ModConfig> AllGameSets => allGameSets;
        public IReadOnlyList<InstalledGameSet> InstalledGameSets => installedGameSets;

        public void ShowMessage(string title, string text)
        {
            if (Spacebox.Game.GUI.MenuUi.MenuCanvas.Current != null) Spacebox.Game.GUI.MenuUi.MenuCanvas.Current.ShowMessage(title, text);
            else messageWindow.Show(title, text);
        }

        public InstalledGameSet FindInstalled(ModConfig config)
        {
            foreach (var installed in installedGameSets)
            {
                if (string.Equals(installed.FolderName, config?.FolderName, StringComparison.OrdinalIgnoreCase)) return installed;
            }
            return null;
        }

        public List<InstalledGameSet> ExtensionsFor(ModConfig baseConfig)
        {
            return Spacebox.Game.Resource.Content.InstalledGameSets.ExtensionsFor(installedGameSets, FindInstalled(baseConfig));
        }

        public string ExtensionProblem(InstalledGameSet extension, ModConfig baseConfig)
        {
            return Spacebox.Game.Resource.Content.ExtensionCompatibility.Problem(extension, FindInstalled(baseConfig), installedGameSets);
        }

        public ExtensionResolution ResolveNewWorldExtensions()
        {
            var baseSet = gameSets.Count > 0 ? FindInstalled(gameSets[Math.Clamp(selectedGameSetIndex, 0, gameSets.Count - 1)]) : null;
            return ExtensionResolver.Resolve(baseSet, installedGameSets, NewWorldExtensions);
        }

        public string NewWorldName { get => newWorldName; set => newWorldName = value; }
        public string NewWorldAuthor { get => newWorldAuthor; set => newWorldAuthor = value; }
        public string NewWorldSeed { get => newWorldSeed; set => newWorldSeed = value; }
        public int SelectedGameModeIndex { get => selectedGameModeIndex; set => selectedGameModeIndex = value; }
        public int SelectedGameSetIndex { get => selectedGameSetIndex; set => selectedGameSetIndex = value; }

        public void GenerateRandomSeedAndName()
        {
            NewWorldExtensions.Clear();
            Random r = new Random();
            newWorldSeed = r.Next(int.MinValue, int.MaxValue).ToString();
            newWorldName = GetNewWorldUniqueName();
        }

        public ModConfig? GetModById(string id)
        {
            foreach (var m in allGameSets)
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
                if (IsEditMode && selectedWorld != null && w == selectedWorld && w.Name == name)
                    return true;

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

        public void CenterInputText2(string label, ref string input, uint maxLength, float totalWidth, float height, nint iconTexture)
        {
            float windowWidth = ImGui.GetWindowWidth();
            Vector2 labelSize = ImGui.CalcTextSize(label);

            ImGui.SetCursorPosX((windowWidth - labelSize.X) * 0.5f);
            ImGui.Text(label);

            float spacing = 5.0f;
            float inputWidth = totalWidth - height - spacing;

            float textHeight = ImGui.GetFontSize();
            float paddingY = (height - textHeight) * 0.5f;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, paddingY));

            ImGui.SetCursorPosX((windowWidth - totalWidth) * 0.5f);

            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##" + label, ref input, maxLength);

            ImGui.SameLine(0, spacing);

            if (ImGui.ImageButton("##rndBtn_" + label, iconTexture, new Vector2(textHeight, textHeight)))
            {
                input = new Random().Next().ToString();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Randomize");
                ImGui.EndTooltip();
            }

            ImGui.PopStyleVar();
        }



        public int GameSetsVersion { get; private set; }

        private void LoadGameSets()
        {
            GameSetsVersion++;
            gameSets.Clear();
            allGameSets.Clear();
            installedGameSets.Clear();
            var modFolders = Spacebox.Game.Resource.Content.ModFolders.Local;
            foreach (var folder in modFolders.ContentFolders) Directory.CreateDirectory(folder);

            foreach (var installed in Spacebox.Game.Resource.Content.InstalledGameSets.Scan(modFolders))
            {
                var gameSetInfo = installed.Config;
                gameSetInfo.Icon = LoadIcon(installed.Folder);
                gameSetInfo.ModPath = installed.Folder;

                installedGameSets.Add(installed);
                allGameSets.Add(gameSetInfo);
                if (!installed.IsExtension) gameSets.Add(gameSetInfo);
            }
        }

        private static Texture2D? LoadIcon(string modPath)
        {


            var iconPath = Path.Combine(modPath, "icon.png");
            var icon = MenuIcons.Load(iconPath);
            if (icon == null) Debug.Warning("No icon.png for GameSet found! Path: " + iconPath);

            return icon;
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
                Extensions = new List<string>(Spacebox.Game.Resource.Content.WorldExtensionList.Normalize(NewWorldExtensions)),
                GameVersion = Application.Version,
                LastEditDate = WorldInfo.GetCurrentDate(),
                FolderName = newWorldName,
                ShowWelcomeWindow = true,
                GeneratorVersion = Spacebox.Game.Generation.GeneratorVersions.Current
            };

            SaveNewWorld(newWorld);


            worlds.Add(newWorld);
            selectedWorld = newWorld;
        }

        private static void SaveNewWorld(WorldInfo info)
        {
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string newWorldDirectory = Path.Combine(worldsDirectory, info.FolderName);
            if (!Directory.Exists(newWorldDirectory)) Directory.CreateDirectory(newWorldDirectory);
            WorldInfoFiles.Write(newWorldDirectory, info);
        }

        public bool SubmitWorldForm(bool extensionErrors)
        {
            if (IsEditMode)
            {
                string finalName = newWorldName;
                if (selectedWorld != null && finalName != selectedWorld.Name)
                {
                    int counter = 1;
                    string baseName = finalName;
                    while (!IsNameUnique(finalName))
                    {
                        finalName = $"{baseName} {counter}";
                        counter++;
                    }
                }
                newWorldName = finalName;
                SetStateToWorldSelect();
                SaveEditedWorld();
                return true;
            }

            if (extensionErrors)
            {
                ShowMessage("Extensions", "Fix the extension errors before creating the world");
                return false;
            }

            if (!IsNameUnique(NewWorldName))
            {
                ShowMessage("World name", "A world with this name already exists");
                return false;
            }

            CreateNewWorld();
            SetStateToWorldSelect();
            return true;
        }

        public void SaveEditedWorld()
        {
            if (selectedWorld == null) return;

            string oldFolderName = selectedWorld.FolderName;
            if (string.IsNullOrEmpty(oldFolderName)) oldFolderName = selectedWorld.Name;

            string newName = newWorldName;
            string worldsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Worlds");
            string oldPath = Path.Combine(worldsDirectory, oldFolderName);
            string newPath = Path.Combine(worldsDirectory, newName);

            if (oldFolderName != newName)
            {
                if (Directory.Exists(oldPath))
                {
                    if (!Directory.Exists(newPath))
                    {
                        try
                        {
                            Directory.Move(oldPath, newPath);
                            selectedWorld.FolderName = newName;
                        }
                        catch (Exception ex)
                        {
                            Debug.Error($"Failed to rename folder: {ex.Message}");
                            return;
                        }
                    }
                    else
                    {
                        Debug.Error($"Folder {newName} already exists!");
                        return;
                    }
                }
            }

            selectedWorld.Name = newName;
            selectedWorld.GameMode = (GameMode)selectedGameModeIndex;
            selectedWorld.LastEditDate = WorldInfo.GetCurrentDate();

            SaveNewWorld(selectedWorld);
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
            TrashIcon.Draw(drawList, imagePosMin, imagePosMax);
        }

        public void OpenWorld(WorldInfo world)
        {
            if (world == null) return;

            selectedWorld = world;
            if (VersionConverter.GetSupport(world, out _) == Versioning.WorldUpgradeStatus.UpToDate)
            {
                LoadWorld(world);
                return;
            }

            updateVersionWindow.Reset();
            showVersionConvertWindow = true;
        }

        public void LoadWorld(WorldInfo world, bool multiplayer = false)
        {
            if (!WorldGameSetCheck.TryResolve(world, out var gameSetError))
            {
                Debug.Error($"[Menu] {gameSetError}");
                ShowMessage("Missing GameSet content", gameSetError);
                return;
            }

            ModConfig modInfo = null;
            foreach (var mod in gameSets)
            {
                if (mod.ModId == world.ModId) { modInfo = mod; break; }
            }

            var clientConfig = Get<MultiplayerWindow>().LoadConfig();
            var arg = new SpaceSceneArgs()
            {
                worldName = world.Name,
                modId = world.ModId,
                extensions = WorldGameSetCheck.ExtensionsOf(world),
                seed = world.Seed,
                modfolderName = modInfo?.FolderName ?? "",
                skinColor = clientConfig.SkinColor
            };

            Spacebox.Network.GameSession.SetPlayer(clientConfig.PlayerNickname, clientConfig.SkinColor);
            if (multiplayer)
                Spacebox.Network.GameSession.BeginHost(Engine.Networking.NetworkManager.DefaultPort);
            else
                Spacebox.Network.GameSession.BeginSingleplayer();

            Input.HideCursor();
            hide = true;
            IsVisible = false;
            CenteredImageMenu.Hide();

            VerticalLinks.IsVisible = false;
            DevLogWindow.Instance.IsVisible = false;

            ColorOverlay.StartFade(FadeMode.FadeIn, new Vector3(0, 0, 0), 1f, 0, 1, 0, () =>
            {

                SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(arg);
            }, (t) =>
            {

                if (t < 0.3f) CenteredText.SetText("Loading");
                else if (t < 0.5f) CenteredText.SetText("Loading.");
                else if (t < 0.7f) CenteredText.SetText("Loading..");
                else CenteredText.SetText("Loading...");

                CenteredText.Show();
            });


        }

        public bool CanDeleteGameSet(ModConfig mod) => Spacebox.Game.Resource.Content.GameSetDependents.CanDelete(FindInstalled(mod));

        public Spacebox.Game.Resource.Content.GameSetDependents DependentsOf(ModConfig mod)
        {
            var uses = new List<Spacebox.Game.Resource.Content.WorldGameSetUse>(worlds.Count);
            foreach (var world in worlds)
                uses.Add(new Spacebox.Game.Resource.Content.WorldGameSetUse(world.Name, world.ModId, WorldGameSetCheck.ExtensionsOf(world)));
            return Spacebox.Game.Resource.Content.GameSetDependents.Find(FindInstalled(mod), installedGameSets, uses);
        }

        public string DeleteGameSet(ModConfig mod)
        {
            var installed = FindInstalled(mod);
            if (!Spacebox.Game.Resource.Content.GameSetDependents.CanDelete(installed)) return "This GameSet cannot be deleted";

            try
            {
                Directory.Delete(installed.Folder, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return "Could not delete the folder: " + ex.Message;
            }

            Debug.Log($"[Mods] Deleted '{installed.Id}' ({installed.Folder})");
            NewWorldExtensions.RemoveAll(id => installed.Matches(id));
            LoadGameSets();
            selectedGameSetIndex = Math.Clamp(selectedGameSetIndex, 0, Math.Max(0, gameSets.Count - 1));
            return null;
        }

        public string CreateGameSet(Spacebox.Game.Resource.Content.NewGameSetRequest request)
        {
            var result = Spacebox.Game.Resource.Content.GameSetCreator.Create(Spacebox.Game.Resource.Content.ModFolders.Local, request, installedGameSets);
            if (!result.Success) return result.Error;

            Debug.Success($"[Mods] Created {request.Kind} '{result.ModId}' in {result.Folder}");
            LoadGameSets();
            OpenGameSetEditor(GetModById(result.ModId), "Overview");
            return null;
        }

        public void OpenGameSetEditor(ModConfig mod, string startPanel)
        {
            if (mod == null) return;

            OpenEditor(new Spacebox.Editor.GameSetEditorArgs
            {
                ModId = mod.ModId,
                ModFolderName = mod.FolderName,
                StartPanel = startPanel
            });
        }

        public void OpenTexturePackEditor(Spacebox.Game.Resource.TexturePacks.TexturePack pack)
        {
            if (pack == null) return;
            OpenEditor(Spacebox.Editor.GameSetEditorLauncher.TexturePackArgs(pack.Folder));
        }

        private void OpenEditor(Spacebox.Editor.GameSetEditorArgs args)
        {
            Click1.Play();
            hide = true;
            IsVisible = false;
            CenteredImageMenu.Hide();
            VerticalLinks.IsVisible = false;
            if (DevLogWindow.Instance != null) DevLogWindow.Instance.IsVisible = false;

            ColorOverlay.StartFade(FadeMode.FadeIn, new Vector3(0, 0, 0), 0.5f, 0, 1, 0, () =>
            {
                SceneManager.Load<Spacebox.Editor.GameSetEditorScene, Spacebox.Editor.GameSetEditorArgs>(args);
            }, null);
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
                   
            Get<MultiplayerWindow>()?.Dispose();
            Get<ShipViewerWindow>()?.Dispose();
            Click1.Dispose();
        }

        public void RenderOffscreen()
        {
            if (currentWindowType != null && windows.TryGetValue(currentWindowType, out var window))
                window.OnSceneRender();
        }

        public List<WorldInfo> GetWorlds() => worlds;
        public List<ModConfig> GetGameSets() => gameSets;

        public Type CurrentWindowType => currentWindowType;

        public void SetStateToControls() => OpenOptions<ControlsWindow>();
        public void SetStateToMain() => Open<MainMenuWindow>();
        public void SetStateToWorldSelect() => Open<WorldSelectWindow>();
        public void SetStateToNewWorld() => Open<NewWorldWindow>();
        public void SetStateToOptions() => OpenOptions<OptionsWindow>();
        public void SetStateToMultiplayer() => Open<MultiplayerWindow>();
        public void SetStateToSettingsControls() => Open<SettingsControlsWindow>();
        public void SetStateToGraphics() => OpenOptions<GraphicsWindow>();

        private void OpenOptions<T>() where T : MenuWindow
        {
            Open<OptionsHostWindow>();
            Get<OptionsHostWindow>().Options.Show<T>();
        }
        public void SetStateToMods() => Open<ModsWindow>();
    }
}
