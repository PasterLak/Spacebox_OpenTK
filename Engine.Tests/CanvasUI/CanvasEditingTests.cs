using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Tests.CanvasUI;

public class CanvasEditingTests
{
    private static CanvasRoot CreateRoot()
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new MonospaceFont(10f, 6f, 10f), DefaultFontSize = 10f } };
        root.Resize(new Vector2(800f, 600f));
        return root;
    }

    private static void Frame(CanvasRoot root, Vector2 pointer, bool down = false, KeyPress[] keys = null, string text = "")
    {
        root.Update(0.016f, new PointerState { Position = pointer, LeftDown = down }, new KeyboardFrame(keys ?? Array.Empty<KeyPress>(), text));
    }

    private static void Click(CanvasRoot root, Vector2 point)
    {
        Frame(root, point);
        Frame(root, point, true);
        Frame(root, point);
    }

    private static void Key(CanvasRoot root, Keys key, bool control = false, bool shift = false)
    {
        Frame(root, root.PointerPosition, false, new[] { new KeyPress(key, shift, control, false, false) });
    }

    private static void Type(CanvasRoot root, string text) => Frame(root, root.PointerPosition, false, null, text);

    [Fact]
    public void TextEditState_EditsWithSelectionWordsAndUndo()
    {
        var state = new TextEditState { Text = "hello world" };
        state.SetCaret(11, false);
        Assert.True(state.Backspace(true));
        Assert.Equal("hello ", state.Text);

        state.MoveLeft(true, true);
        Assert.Equal("hello", state.SelectedText.Trim());
        state.Insert("bye");
        Assert.Equal("bye", state.Text);

        Assert.True(state.Undo());
        Assert.Equal("hello ", state.Text);
        Assert.True(state.Redo());
        Assert.Equal("bye", state.Text);

        state.MaxLength = 4;
        state.SetCaret(3, false);
        state.Insert("abc");
        Assert.Equal("byea", state.Text);
    }

    [Fact]
    public void TextEditState_MovesBetweenLines()
    {
        var state = new TextEditState(true) { Text = "abc\nde\nfghij" };
        state.SetCaret(2, false);
        state.MoveLines(1, false);
        Assert.Equal(6, state.Caret);
        state.MoveLines(1, false);
        Assert.Equal(9, state.Caret);
        state.MoveHome(false);
        Assert.Equal(7, state.Caret);
        Assert.Equal((2, 0), state.LineAndColumn(state.Caret));

        var single = new TextEditState { Text = "a\nb" };
        Assert.Equal("a b", single.Text);
    }

    [Fact]
    public void TextEditKeys_UseTheClipboard()
    {
        var clipboard = new MemoryClipboard();
        var state = new TextEditState { Text = "copy me" };
        state.SelectAll();

        Assert.Equal(TextEditResult.Handled, TextEditKeys.Handle(state, new KeyPress(Keys.C, false, true, false, false), clipboard, true));
        Assert.Equal("copy me", clipboard.GetText());
        Assert.Equal(TextEditResult.Changed, TextEditKeys.Handle(state, new KeyPress(Keys.X, false, true, false, false), clipboard, true));
        Assert.Equal("", state.Text);
        Assert.Equal(TextEditResult.Changed, TextEditKeys.Handle(state, new KeyPress(Keys.V, false, true, false, false), clipboard, true));
        Assert.Equal("copy me", state.Text);
        Assert.Equal(TextEditResult.Submitted, TextEditKeys.Handle(state, new KeyPress(Keys.Enter, false, false, false, false), clipboard, true));
        Assert.Equal(TextEditResult.Handled, TextEditKeys.Handle(state, new KeyPress(Keys.Backspace, false, false, false, false), clipboard, false));
        Assert.Equal("copy me", state.Text);
    }

    [Fact]
    public void LineEdit_TakesFocusTypesAndReportsChanges()
    {
        var root = CreateRoot();
        var edit = root.Add(new LineEdit());
        edit.SetOffsets(10, 10, 210, 34);
        root.Layout();
        string changed = null, finished = null, submitted = null;
        edit.TextChanged += t => changed = t;
        edit.EditingFinished += t => finished = t;
        edit.TextSubmitted += t => submitted = t;

        Click(root, new Vector2(20, 20));
        Assert.True(edit.HasFocus);
        Type(root, "abc");
        Assert.Equal("abc", edit.Text);
        Assert.Equal("abc", changed);
        Key(root, Keys.Backspace);
        Assert.Equal("ab", edit.Text);
        Key(root, Keys.Enter);
        Assert.Equal("ab", submitted);
        Assert.Equal("ab", finished);

        Click(root, new Vector2(500, 500));
        Assert.False(edit.HasFocus);
        Assert.False(root.WantsKeyboard);
    }

    [Fact]
    public void Tab_MovesFocusAndUnhandledKeysAreReported()
    {
        var root = CreateRoot();
        var box = root.Add(new VBoxContainer());
        box.SetOffsets(10, 10, 300, 200);
        var first = box.AddChild(new LineEdit());
        var second = box.AddChild(new LineEdit());
        root.Layout();
        var unhandled = new List<Keys>();
        root.UnhandledKeyPress += press => unhandled.Add(press.Key);

        first.GrabFocus();
        Key(root, Keys.Tab);
        Assert.True(second.HasFocus);
        Key(root, Keys.Tab, shift: true);
        Assert.True(first.HasFocus);

        root.ReleaseFocus();
        Key(root, Keys.S, control: true);
        Assert.Contains(Keys.S, unhandled);
    }

    [Fact]
    public void NumberEdit_ParsesClampsAndSteps()
    {
        var root = CreateRoot();
        var number = root.Add(new NumberEdit { MinValue = 0, MaxValue = 10, Step = 0.5 });
        number.SetOffsets(10, 10, 110, 34);
        root.Layout();
        double reported = -1;
        number.ValueChanged += v => reported = v;

        number.GrabFocus();
        number.SelectAll();
        Type(root, "42");
        Key(root, Keys.Enter);
        Assert.Equal(10, number.Value);
        Assert.Equal(10, reported);
        Key(root, Keys.Down);
        Assert.Equal(9.5, number.Value);
        Assert.True(NumberEdit.TryParse("1,5", out var comma) && comma == 1.5);
        Assert.False(NumberEdit.TryParse("abc", out _));

        number.Integer = true;
        number.Value = 3.7;
        Assert.Equal("4", number.Text);
    }

    [Fact]
    public void ItemList_SelectsWithMouseAndKeysSkippingHeaders()
    {
        var root = CreateRoot();
        var list = root.Add(new ItemList());
        list.SetOffsets(0, 0, 200, 300);
        list.AddHeader("Group");
        list.AddItem("one");
        list.AddItem("two");
        root.Layout();
        var selected = new List<int>();
        list.ItemSelected += selected.Add;

        Click(root, new Vector2(20, list.RowHeight * 0.5f));
        Assert.Equal(-1, list.SelectedIndex);
        Click(root, new Vector2(20, list.RowHeight * 1.5f));
        Assert.Equal(1, list.SelectedIndex);
        Key(root, Keys.Down);
        Assert.Equal(2, list.SelectedIndex);
        Key(root, Keys.Up);
        Key(root, Keys.Up);
        Assert.Equal(1, list.SelectedIndex);
        Assert.Equal(new[] { 1, 2, 1 }, selected);
    }

    [Fact]
    public void ComboBox_FiltersAndPicksAValue()
    {
        var root = CreateRoot();
        var combo = root.Add(new ComboBox());
        combo.SetOffsets(10, 10, 210, 34);
        combo.AddOption("iron_ore");
        combo.AddOption("iron_ingot");
        combo.AddOption("glass");
        root.Layout();
        string value = null;
        combo.ValueChanged += v => value = v;

        combo.Edit.GrabFocus();
        Type(root, "ingot");
        Assert.True(combo.IsOpen);
        Key(root, Keys.Down);
        Key(root, Keys.Enter);
        Assert.Equal("iron_ingot", value);
        Assert.Equal("iron_ingot", combo.Value);
        Assert.False(combo.IsOpen);
    }

    [Fact]
    public void Containers_SwitchTabsSplitAndFold()
    {
        var root = CreateRoot();
        var tabs = root.Add(new TabContainer());
        tabs.SetOffsets(0, 0, 400, 300);
        var a = tabs.AddTab(new Control(), "First");
        var b = tabs.AddTab(new Control(), "Second");
        root.Layout();
        Assert.True(a.Visible);
        Assert.False(b.Visible);
        Click(root, new Vector2(70, tabs.TabHeight * 0.5f));
        root.Layout();
        Assert.Equal(1, tabs.CurrentTab);
        Assert.True(b.Visible);

        var split = root.Add(new SplitContainer { MinFirst = 10, MinSecond = 10 });
        split.SetOffsets(0, 300, 400, 400);
        var left = split.AddChild(new Control());
        split.AddChild(new Control());
        split.Ratio = 0.5f;
        root.Layout();
        Assert.InRange(left.Rect.Width, 190f, 200f);

        var fold = root.Add(new FoldableContainer("Section"));
        fold.SetOffsets(500, 0, 700, 200);
        var content = fold.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 50f) });
        root.Layout();
        float expanded = fold.GetCombinedMinimumSize().Y;
        fold.Expanded = false;
        root.Layout();
        Assert.True(fold.GetCombinedMinimumSize().Y < expanded);
        Assert.False(content.Visible);
    }

    [Fact]
    public void ColorPicker_ParsesHexAndDialogReportsTheButton()
    {
        Assert.True(ColorPickerButton.TryParseHex("#FF8000", out var color));
        Assert.Equal("#FF8000", ColorPickerButton.ToHex(color));
        Assert.False(ColorPickerButton.TryParseHex("zz", out _));

        var root = CreateRoot();
        int result = -1;
        var dialog = new ConfirmDialog();
        dialog.Show(root, "Delete", "Really?", new[] { "Delete", "Cancel" }, r => result = r);
        root.Layout();
        Assert.True(dialog.HasFocus);
        Key(root, Keys.Escape);
        Assert.Equal(1, result);
        Assert.False(dialog.IsOpen);
    }

    [Fact]
    public void AutoHidePopup_ClosesOnOutsideClick()
    {
        var root = CreateRoot();
        var popup = root.Add(new PopupPanel { AutoHide = true }, UiLayer.Popup);
        popup.AddChild(new Control { CustomMinimumSize = new Vector2(100f, 100f) });
        popup.Popup(new Rect2(10, 10, 10, 10), PopupSide.Below);
        root.Layout();
        bool hidden = false;
        popup.Hidden += _ => hidden = true;

        Click(root, new Vector2(30, 40));
        Assert.True(popup.Visible);
        Click(root, new Vector2(700, 500));
        Assert.True(hidden);
    }
}
