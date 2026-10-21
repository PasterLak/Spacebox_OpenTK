using System.Collections.Concurrent;
using System.Numerics;
using ImGuiNET;

namespace Engine.Automation;

public static unsafe class ImGuiTextCapture
{
    private static readonly ConcurrentQueue<TaskCompletionSource<UiTextSnapshot>> _requests = new ConcurrentQueue<TaskCompletionSource<UiTextSnapshot>>();
    private static readonly ImGuiGlyphTable _glyphs = new ImGuiGlyphTable();
    private static readonly GlyphTextAssembler _assembler = new GlyphTextAssembler();

    public static Task<UiTextSnapshot> CaptureNextFrameAsync()
    {
        var request = new TaskCompletionSource<UiTextSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        _requests.Enqueue(request);
        return request.Task;
    }

    public static void Publish(ImDrawDataPtr drawData)
    {
        if (_requests.IsEmpty) return;

        UiTextSnapshot snapshot;
        try
        {
            snapshot = Read(drawData);
        }
        catch (Exception ex)
        {
            while (_requests.TryDequeue(out var failed)) failed.TrySetException(ex);
            return;
        }

        while (_requests.TryDequeue(out var request)) request.TrySetResult(snapshot);
    }

    private static UiTextSnapshot Read(ImDrawDataPtr drawData)
    {
        _glyphs.Refresh();
        var windows = new List<UiWindowText>();

        for (int i = drawData.CmdListsCount - 1; i >= 0; i--)
        {
            var list = drawData.CmdLists[i];
            var window = ReadList(list, drawData.CmdListsCount - 1 - i);
            if (window != null) windows.Add(window);
        }

        return new UiTextSnapshot(drawData.DisplaySize.X, drawData.DisplaySize.Y, windows);
    }

    private static UiWindowText ReadList(ImDrawListPtr list, int layer)
    {
        var native = list.NativePtr;
        var vertices = (ImDrawVert*)native->VtxBuffer.Data;
        var indices = (ushort*)native->IdxBuffer.Data;
        var commands = (ImDrawCmd*)native->CmdBuffer.Data;
        if (native->VtxBuffer.Size == 0) return null;

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        for (int v = 0; v < native->VtxBuffer.Size; v++)
        {
            min = Vector2.Min(min, vertices[v].pos);
            max = Vector2.Max(max, vertices[v].pos);
        }

        for (int c = 0; c < native->CmdBuffer.Size; c++)
        {
            var command = commands[c];
            if (command.UserCallback != IntPtr.Zero || command.TextureId != _glyphs.TextureId) continue;
            ReadGlyphs(vertices + command.VtxOffset, indices + command.IdxOffset, (int)command.ElemCount);
        }

        var texts = _assembler.Finish();
        string id = list._OwnerName.ToString() ?? "";
        int hidden = id.IndexOf("##", StringComparison.Ordinal);
        string name = hidden >= 0 ? id.Substring(0, hidden) : id;
        return new UiWindowText(name, id, layer, min.X, min.Y, max.X - min.X, max.Y - min.Y, texts);
    }

    private static void ReadGlyphs(ImDrawVert* vertices, ushort* indices, int count)
    {
        int i = 0;
        while (i + 5 < count)
        {
            if (!IsQuad(indices + i) || !TryReadGlyph(vertices, indices[i], out var placement))
            {
                i += 3;
                continue;
            }

            _assembler.Add(placement);
            i += 6;
        }
    }

    private static bool IsQuad(ushort* idx)
    {
        int a = idx[0];
        return idx[1] == a + 1 && idx[2] == a + 2 && idx[3] == a && idx[4] == a + 2 && idx[5] == a + 3;
    }

    private static bool TryReadGlyph(ImDrawVert* vertices, int first, out GlyphPlacement placement)
    {
        placement = default;
        var topLeft = vertices[first];
        var bottomRight = vertices[first + 2];
        if (!_glyphs.TryGet(topLeft.uv, out var glyph)) return false;
        if (MathF.Abs(bottomRight.uv.X - glyph.U1) > 1e-5f || MathF.Abs(bottomRight.uv.Y - glyph.V1) > 1e-5f) return false;

        float width = glyph.X1 - glyph.X0;
        float scale = width > 0f ? (bottomRight.pos.X - topLeft.pos.X) / width : 1f;
        placement = new GlyphPlacement(
            glyph.Char,
            topLeft.pos.X - glyph.X0 * scale,
            topLeft.pos.Y - glyph.Y0 * scale,
            glyph.AdvanceX * scale,
            glyph.FontSize * scale,
            glyph.SpaceAdvance * scale);
        return true;
    }
}
