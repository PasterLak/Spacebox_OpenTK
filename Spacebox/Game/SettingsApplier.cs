using Engine;
using Engine.Audio;
using Spacebox.Game.Events;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Rendering;

namespace Spacebox.Game;

public static class SettingsApplier
{
    public static void ApplyAll()
    {
        ApplyAudio(Settings.Audio);
        ApplyGraphics(Settings.Graphics);
        ApplyGameplay(Settings.Gameplay);
    }

    public static void ApplyAudio(AudioSettings audio)
    {
        AudioMixer.Master = audio.Master / 100f;
        AudioMixer.SetVolume(AudioChannel.Music, audio.Music / 100f);
        AudioMixer.SetVolume(AudioChannel.Ambient, audio.Ambient / 100f);
        AudioMixer.SetVolume(AudioChannel.Effects, audio.Effects / 100f);
        AudioMixer.SetVolume(AudioChannel.UI, audio.UI / 100f);
    }

    public static void UpdateFocusMute(bool isFocused)
    {
        AudioMixer.Muted = Settings.Audio.MuteWhenUnfocused && !isFocused;
    }

    public static void ApplyGraphics(GraphicsSettings graphics)
    {
        var window = SpaceboxWindow.Instance;
        if (window != null)
        {
            window.SetWindowMode(graphics.WindowMode);
            window.SetVSync(graphics.VSync);
            window.RenderScale = graphics.ResolutionScalePercent / 100f;
            window.PostProcessingEnabled = graphics.PostProcessing;
            (window as SpaceboxWindow)?.Ssao?.Configure(SsaoSettingsMapper.From(graphics));
        }

        ApplyFrameLimit(graphics);
        ParticleSystem.GloballyEnabled = graphics.EffectsEnabled;
        ApplyChunkVisuals(graphics);

        if (Camera.Main is LocalAstronaut player && player.IsAlive && !Input.IsAction("zoom"))
            player.FOV = graphics.Fov;

        var changed = new GraphicsSettingsChangedEvent { NewSettings = graphics };
        EventBus.Publish(ref changed);
    }

    public static void ApplyGameplay(GameplaySettings gameplay)
    {
        Camera360Base.MouseSensitivity = gameplay.MouseSensitivity / 100f;
        Camera360Base.InvertMouseY = gameplay.InvertMouseY;
    }

    private static void ApplyFrameLimit(GraphicsSettings graphics)
    {
        if (graphics.MaxFps <= 0) FrameLimiter.SetUnlimited();
        else FrameLimiter.TargetFPS = graphics.MaxFps;
    }

    private static void ApplyChunkVisuals(GraphicsSettings graphics)
    {
        bool aoChanged = MeshGenerator.EnableAO != graphics.AO;
        bool lightingChanged = LightManager.EnableLighting != graphics.VoxelLighting;
        if (!aoChanged && !lightingChanged) return;

        MeshGenerator.EnableAO = graphics.AO;
        LightManager.EnableLighting = graphics.VoxelLighting;
        World.Instance?.RefreshChunkVisuals(lightingChanged);
    }
}
