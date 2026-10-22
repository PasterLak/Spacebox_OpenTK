using Spacebox.AssetAudit;

namespace Spacebox.Tests.Assets;

public sealed class AssetAuditTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_audit_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private void Write(string relativePath, string content = "x")
    {
        string path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private AssetAuditResult Audit()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Spacebox"));
        return new AssetAuditor(_root).Analyze();
    }

    private static bool IsUnused(AssetAuditResult result, string relativePath)
    {
        return result.Unused.Any(file => file.RelativePath == relativePath);
    }

    [Fact]
    public void BlockTexture_IsUsedOnlyWhenJsonReferencesIt()
    {
        Write("Spacebox/Mods/GameSets/Default/blocks.json", "[{ \"Textures\": { \"Active\": { \"All\": \"stoneWall\" } } }]");
        Write("Spacebox/Mods/GameSets/Default/Blocks/Walls/stoneWall.png");
        Write("Spacebox/Mods/GameSets/Default/Blocks/oldWall.png");

        var result = Audit();

        Assert.Contains(result.Used, file => file.RelativePath == "Mods/GameSets/Default/Blocks/Walls/stoneWall.png");
        Assert.True(IsUnused(result, "Mods/GameSets/Default/Blocks/oldWall.png"));
    }

    [Fact]
    public void Emission_FollowsItsTextureName()
    {
        Write("Spacebox/Mods/GameSets/Default/items.json", "[{ \"Sprite\": \"lamp\" }]");
        Write("Spacebox/Mods/GameSets/Default/Emissions/lamp.png");
        Write("Spacebox/Mods/GameSets/Default/Emissions/oldLamp.png");

        var result = Audit();

        Assert.False(IsUnused(result, "Mods/GameSets/Default/Emissions/lamp.png"));
        Assert.True(IsUnused(result, "Mods/GameSets/Default/Emissions/oldLamp.png"));
    }

    [Fact]
    public void SoundUsedByShortNameInCode_IsKept()
    {
        Write("Spacebox/Game/Sounds.cs", "class Sounds { void Play() { Resources.Load<AudioClip>(\"ricochet\"); } }");
        Write("Spacebox/Mods/GameSets/Default/Sounds/Weapons/ricochet.ogg");
        Write("Spacebox/Mods/GameSets/Default/Sounds/Weapons/oldShot.ogg");

        var result = Audit();

        Assert.False(IsUnused(result, "Mods/GameSets/Default/Sounds/Weapons/ricochet.ogg"));
        Assert.True(IsUnused(result, "Mods/GameSets/Default/Sounds/Weapons/oldShot.ogg"));
    }

    [Fact]
    public void ReferencesInCommentedCode_DoNotCount()
    {
        Write("Spacebox/Game/Old.cs", "class Old {\n// var a = \"Resources/Models/skybox.obj\";\n/* \"Resources/Models/test.obj\" */\n}");
        Write("Spacebox/Resources/Models/skybox.obj");
        Write("Spacebox/Resources/Models/test.obj");

        var result = Audit();

        Assert.True(IsUnused(result, "Resources/Models/skybox.obj"));
        Assert.True(IsUnused(result, "Resources/Models/test.obj"));
    }

    [Fact]
    public void IgnoredFolders_AreUnusedEvenWhenTheNameIsReferenced()
    {
        Write("Spacebox/Mods/GameSets/Default/blocks.json", "[{ \"Textures\": { \"Active\": { \"All\": \"wall\" } } }]");
        Write("Spacebox/Mods/GameSets/Default/Blocks/_Ignored/wall.png");

        var result = Audit();

        Assert.True(IsUnused(result, "Mods/GameSets/Default/Blocks/_Ignored/wall.png"));
    }

    [Fact]
    public void Shaders_FollowTheIncludeChain()
    {
        Write("Spacebox/Game/Mat.cs", "class Mat { object s = Load(\"Resources/Shaders/block\"); }");
        Write("Spacebox/Resources/Shaders/block.glsl", "--Frag\n#include \"includes/lighting.fs\"\n");
        Write("Spacebox/Resources/Shaders/includes/lighting.fs", "#include \"fog.glsl\"\n");
        Write("Spacebox/Resources/Shaders/includes/fog.glsl", "void f() {}");
        Write("Spacebox/Resources/Shaders/includes/core.fs", "void g() {}");
        Write("Spacebox/Resources/Shaders/outline.glsl", "--Frag\n#include \"includes/core.fs\"\n");

        var result = Audit();

        Assert.False(IsUnused(result, "Resources/Shaders/block.glsl"));
        Assert.False(IsUnused(result, "Resources/Shaders/includes/lighting.fs"));
        Assert.False(IsUnused(result, "Resources/Shaders/includes/fog.glsl"));
        Assert.True(IsUnused(result, "Resources/Shaders/includes/core.fs"));
        Assert.True(IsUnused(result, "Resources/Shaders/outline.glsl"));
    }

    [Fact]
    public void InterpolatedPath_KeepsAllFilesWithItsPrefix()
    {
        Write("Spacebox/Game/Skin.cs", "class Skin { string P(string c) => $\"Resources/Textures/Skins/Astronaut_{c}.jpg\"; }");
        Write("Spacebox/Resources/Textures/Skins/Astronaut_Red.jpg");
        Write("Spacebox/Resources/Textures/Skins/Old_Blue.jpg");

        var result = Audit();

        Assert.False(IsUnused(result, "Resources/Textures/Skins/Astronaut_Red.jpg"));
        Assert.True(IsUnused(result, "Resources/Textures/Skins/Old_Blue.jpg"));
    }

    [Fact]
    public void GameSetRelativeJsonPath_IsUsed()
    {
        Write("Spacebox/Mods/GameSets/Default/items.json", "[{ \"ArmorModel\": \"Models/Armor/armor_arms.obj\" }]");
        Write("Spacebox/Mods/GameSets/Default/Models/Armor/armor_arms.obj");
        Write("Spacebox/Mods/GameSets/Default/Models/Armor/armor_old.obj");

        var result = Audit();

        Assert.False(IsUnused(result, "Mods/GameSets/Default/Models/Armor/armor_arms.obj"));
        Assert.True(IsUnused(result, "Mods/GameSets/Default/Models/Armor/armor_old.obj"));
    }

    [Fact]
    public void KeepListAndProtectedFiles_AreNeverUnused()
    {
        Write("Spacebox/Build/asset-keep.txt", "# comment\nResources/Models/Keep/**\n");
        Write("Spacebox/Resources/Models/Keep/statue.obj");
        Write("Spacebox/Resources/Textures/UI/slot.png");
        Write("Spacebox/Resources/Audio/Mood/effect1.ogg");
        Write("Spacebox/Mods/GameSets/Default/Generation/data.json", "{}");
        Write("Spacebox/Mods/GameSets/Default/icon.png");

        var result = Audit();

        Assert.Empty(result.Unused);
    }

    [Fact]
    public void MissingCodePath_IsReported()
    {
        Write("Spacebox/Game/Missing.cs", "class Missing { string P = \"Resources/Textures/gone.png\"; }");

        var result = Audit();

        Assert.Contains("Resources/Textures/gone.png", result.MissingReferences);
    }

    [Fact]
    public void RealRepository_KeepsCriticalAssets()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Spacebox", "Mods", "GameSets"))) dir = dir.Parent;
        Assert.NotNull(dir);

        var result = new AssetAuditor(dir!.FullName).Analyze();
        var unused = result.Unused.Select(file => file.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Resources/Shaders/block.glsl", unused);
        Assert.DoesNotContain("Resources/Shaders/includes/lighting.fs", unused);
        Assert.DoesNotContain("Resources/Shaders/includes/gbuffer.glsl", unused);
        Assert.DoesNotContain("Resources/Shaders/PostProcessing/ssaoComposite.glsl", unused);
        Assert.DoesNotContain("Resources/Audio/error.ogg", unused);
        Assert.DoesNotContain("Resources/Audio/Music/menu.ogg", unused);
        Assert.DoesNotContain("Resources/Models/Player/Astronaut_Body_Fly.obj", unused);
        Assert.DoesNotContain("Resources/Textures/Blocks/LOD.png", unused);
        Assert.DoesNotContain("Mods/GameSets/Default/Sounds/Weapons/ar.ogg", unused);
        Assert.DoesNotContain("Mods/GameSets/Default/Sounds/Weapons/ricochet.ogg", unused);
    }
}
