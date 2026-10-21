namespace Spacebox.Game.Mobs;

public static class MobDeathParticles
{
    public const int MaxCount = 200;

    public static MobDeathParticlesJSON Sanitize(MobDeathParticlesJSON settings)
    {
        settings ??= new MobDeathParticlesJSON();
        return new MobDeathParticlesJSON
        {
            Color = settings.Color,
            Count = Math.Clamp(settings.Count, 0, MaxCount),
            Speed = Math.Clamp(settings.Speed, 0f, 20f),
            Size = Math.Clamp(settings.Size, 0.01f, 2f),
            Life = Math.Clamp(settings.Life, 0.1f, 5f),
            Radius = Math.Clamp(settings.Radius, 0f, 4f)
        };
    }
}
