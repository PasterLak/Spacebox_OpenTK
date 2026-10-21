using System.Text;

namespace Spacebox.Game.Resource;

public class PictureFrameBlockData : BlockData, IBlockTooltip
{
    public const int MaxSize = 4;

    private readonly HashSet<string> _pictureIds;

    public int Width { get; }
    public int Height { get; }
    public float ViewDistance { get; }
    public IReadOnlyCollection<string> PictureIds => _pictureIds;

    public PictureFrameBlockData(BlockData baseBlock, PictureFrameJSON settings, string modId) : base(baseBlock)
    {
        settings ??= new PictureFrameJSON();

        Width = ClampSize(settings.Width);
        Height = ClampSize(settings.Height);
        ViewDistance = Math.Clamp(settings.ViewDistance, 4f, 256f);
        _pictureIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (settings.Pictures == null) return;
        foreach (var picture in settings.Pictures)
        {
            if (string.IsNullOrWhiteSpace(picture)) continue;
            string id = picture.Trim();
            _pictureIds.Add(id.Contains(':') ? id : GameSetLoader.CombineId(modId, id));
        }
    }

    public static int ClampSize(int size) => Math.Clamp(size, 1, MaxSize);

    public bool Accepts(Item item)
    {
        return item is PictureItem && item.Id_string != null && _pictureIds.Contains(item.Id_string);
    }

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nPicture size: {Width}x{Height}");
        text.Append($"\nFits {_pictureIds.Count} pictures");
    }
}
