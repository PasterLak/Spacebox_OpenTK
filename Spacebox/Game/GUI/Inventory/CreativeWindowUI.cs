using Engine;
using Engine.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spacebox.Game.GUI;

public class CreativeWindowCategory
{
    public Type Name;
    public List<Item> Items = new List<Item>();
    public Storage Storage;

    public int ItemsCount { get; private set; }

    public CreativeWindowCategory(Type name)
    {
        Name = name;

        if (name != null)
            Items = GetItems(name);

        if (Items.Count > 0)
        {
            Storage = GameAssets.CreateCreativeStorage(5, Items);
        }

        ItemsCount = Items.Count;
    }

    private static List<Item> GetItems(Type categoryType)
    {
        return GameAssets.Items.Values
            .Where(i =>
                (categoryType == typeof(CreativeToolItem) && i is CreativeToolItem) ||
                (categoryType != typeof(CreativeToolItem) && i.GetType() == categoryType))
            .Where(x => x.Id > 1)
            .OrderBy(i => i.Id)
            .ToList();
    }
}

public sealed class CreativeWindowUI : IGameUiController
{
    public const int Columns = 5;

    public bool Enabled => player != null && player.GameModeRules.CreativeInventory;

    private LocalAstronaut player;
    private Storage storageAll;
    private CreativeWindowCategory[] categories;
    private AudioSource scrollAudio;
    private Storage selectedStorage;
    private int hovered = -1;

    private const uint SearchMaxLength = 64;
    private string _search = "";
    private string _appliedSearch = "";
    private Storage _searchResults;
    private readonly List<(Item Item, int Score)> _matches = new List<(Item, int)>();

    public bool IsSearching => _appliedSearch.Length > 0;
    public bool IsVisible => Enabled && UIManager.IsOpen("creative") && storageAll != null;
    public CreativeWindowCategory[] Categories => categories;
    public Storage AllItems => storageAll;
    public Storage SelectedStorage => selectedStorage;
    public Storage ShownStorage => IsSearching ? _searchResults : selectedStorage;
    public bool HasNoResults => IsSearching && _matches.Count == 0;
    public string AppliedSearch => _appliedSearch;
    public string SearchText => _search;

    public int TotalItems
    {
        get
        {
            int total = 0;
            if (categories != null) foreach (var category in categories) total += category.ItemsCount;
            return total;
        }
    }

    public void Search(string text)
    {
        _search = text ?? "";
        string query = _search.Trim();
        if (query != _appliedSearch) ApplySearch(query);
    }

    public void PlayHoverSound(int slotId)
    {
        if (SpaceboxWindow.Instance != null && !SpaceboxWindow.Instance.IsFocused) return;
        if (hovered == slotId || scrollAudio == null) return;

        hovered = slotId;
        if (scrollAudio.IsPlaying) scrollAudio.Stop();
        scrollAudio.Play();
    }

    public void Initialize(LocalAstronaut player)
    {
        this.player = player;
        storageAll = GameAssets.CreateCreativeStorage(Columns, GameAssets.Items.Values.ToList());

        categories = GetCategories0()
         .Select(t => new CreativeWindowCategory(t))
         .OrderByDescending(c => c.Items.Count)
         .ToArray();

        selectedStorage = storageAll;
        _search = "";
        _appliedSearch = "";
        _searchResults = null;

        scrollAudio = new AudioSource(Resources.Get<AudioClip>("scroll"), AudioChannel.UI);
    }

    public void Reset()
    {
        scrollAudio?.Dispose();
        scrollAudio = null;
        player = null;
    }

    private static Type[] GetCategories0()
    {
        HashSet<Type> cats = new HashSet<Type>();
        foreach (var it in GameAssets.Items.Values)
        {
            Type t = it is CreativeToolItem ? typeof(CreativeToolItem) : it.GetType();
            cats.Add(t);
        }
        return cats.ToArray();
    }

    private void ApplySearch(string query)
    {
        _appliedSearch = query;
        _matches.Clear();
        if (query.Length == 0)
        {
            _searchResults = null;
            return;
        }

        foreach (var item in GameAssets.Items.Values)
        {
            if (item.Id <= 1) continue;
            int score = ItemSearch.Score(query, item.Name, SearchKeywords(item));
            if (score != ItemSearch.NoMatch) _matches.Add((item, score));
        }

        _matches.Sort((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score) : a.Item.Id.CompareTo(b.Item.Id));

        var items = new List<Item>(_matches.Count);
        for (int i = 0; i < _matches.Count; i++) items.Add(_matches[i].Item);
        _searchResults = GameAssets.CreateCreativeStorage(Columns, items);
    }

    private static string SearchKeywords(Item item)
    {
        return $"{item.Id_string} {item.Category} {item.GetType().Name}";
    }

    public void Select(Storage storage)
    {
        selectedStorage = storage;
        _search = "";
        ApplySearch("");
    }

    public void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem)
        {
            if (player != null)
            {
                if (Input.IsAction("storage_item_quick_transfer"))
                {
                    if (player.Panel.TryAddItem(slot.Item, slot.Item.StackSize)) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, slot.Item.StackSize);
                    }
                }
                else if (Input.IsKey(Keys.LeftControl))
                {
                    if (player.Panel.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2))) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, (byte)(slot.Item.StackSize / 2));
                    }
                }
                else
                {
                    if (player.Panel.TryAddItem(slot.Item, 1)) { }
                    else
                    {
                        player.Inventory.TryAddItem(slot.Item, 1);
                    }
                }
            }
        }
    }
}