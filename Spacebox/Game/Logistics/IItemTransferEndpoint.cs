using OpenTK.Mathematics;

namespace Spacebox.Game.Logistics;

public interface IItemTransferEndpoint
{
    ItemSlot? FindExtractable(Predicate<Item> accept);
    void OnExtracted();
    bool CanInsert(Item item, Vector3i fromSide);
    bool Insert(Item item, Vector3i fromSide);
}
