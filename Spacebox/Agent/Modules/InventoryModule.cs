using Engine.Web;
using Spacebox.Game;
using Spacebox.Game.GUI;

namespace Spacebox.Agent.Modules;

public sealed class InventoryModule : IAgentModule
{
    public void Map(AgentRouter router)
    {
        router.Get("/inventory", "Hotbar (panel) and inventory slots with items, selected hotbar slot", _ =>
        {
            var player = AgentGame.RequirePlayer();
            return AgentJson.Response(new
            {
                selectedSlot = PanelUI.SelectedSlotId,
                panel = Slots(player.Panel),
                inventory = Slots(player.Inventory)
            });
        });

        router.Post("/inventory/select", "Select a hotbar slot: ?slot=0..(hotbar slots - 1)", request =>
        {
            AgentGame.RequirePlayer();
            int slot = AgentQuery.Int(request, "slot", -1);
            int slots = PanelUI.ActiveSlots;
            if (slot < 0 || slot >= slots) throw new AgentRequestException($"slot must be 0..{slots - 1}");
            PanelUI.SetSelectedSlot((short)slot);
            return AgentJson.Response(new { selectedSlot = PanelUI.SelectedSlotId, heldItem = AgentGame.DescribeItem(PanelUI.HeldItem, PanelUI.CurrentSlot()?.Count ?? 0) });
        });
    }

    private static IEnumerable<object> Slots(Storage storage)
    {
        if (storage == null) yield break;
        foreach (var slot in storage.GetAllSlots())
        {
            if (!slot.HasItem) continue;
            yield return new { slot = slot.SlotId, item = AgentGame.DescribeItem(slot.Item, slot.Count) };
        }
    }
}
