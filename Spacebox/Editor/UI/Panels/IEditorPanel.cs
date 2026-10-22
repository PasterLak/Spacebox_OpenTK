using Engine.Graphics.Atlas;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;

namespace Spacebox.Editor.UI.Panels;

public interface IEditorPanel
{
    string Title { get; }
    void Draw(EditorContext context);
}

public interface IEntryPreview
{
    bool CanPreview(EditorContext context, EditorEntry entry);
    void DrawControls(EditorContext context, EditorEntry entry);
    void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport);
}

public interface IEntryCustomPreview : IEntryPreview
{
    void Draw(EditorContext context, EditorEntry entry);
}

public interface IEntrySummary
{
    void Draw(EditorContext context, EditorEntry entry);
}

public interface IEntryIconProvider
{
    AtlasSprite GetIcon(EditorContext context, EditorEntry entry);
}
