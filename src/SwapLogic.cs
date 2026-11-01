using System;

namespace QuickReplace
{
    internal static class SwapLogic
    {
        internal static bool Eligible(GameItem incoming, GameSlotInventory slot)
        {
            var old = slot?.childItem;
            var owner = slot?.GetParentItem();
            return incoming != null && old != null && incoming != old && owner != null
                && incoming.unitCount == 1 && old.unitCount == 1
                && incoming.parentInventory != null && incoming.parentInventory != slot
                && slot.mayInventoryAddItemFunc != null
                && GeneralHelper.IsItemOwned(incoming) && GeneralHelper.IsItemOwned(old)
                && GeneralHelper.IsItemOwned(owner)
                && incoming.MayRemove() && old.MayRemove()
                && incoming.MaxNumRemove() >= 1 && old.MaxNumRemove() >= 1
                && GeneralHelper.MayPlayerInsertInto(slot)
                && GeneralHelper.MayPlayerInsertInto(incoming.parentInventory)
                && slot.MayHaveValidInventorySlot(incoming)
                && incoming.parentInventory.MayHaveValidInventorySlot(old);
        }

        internal static bool TrySwap(GameItem incoming, GameSlotInventory target)
        {
            if (!Eligible(incoming, target)) return false;
            var old = target.childItem;
            var source = incoming.parentInventory;
            var incomingShape = incoming.modifiedShape.Clone();
            var oldShape = old.modifiedShape.Clone();
            bool success = false;
            try
            {
                if (!source.Expel(incoming) || !target.Expel(old)) return false;
                var putNew = target.TryInventorySlot(incoming, 1, incoming.modifiedShape);
                if (!Valid(putNew, target)) return false;

                // Try the vacated position first, preserving the old item's own dimensions.
                var replacementShape = new GridShapeBuilder(old.modifiedShape).SetTransform(incomingShape).Build();
                var putOld = source.TryInventorySlot(old, 1, replacementShape);
                if (!Valid(putOld, source)) putOld = source.TryFindOneValidInventorySlot(old);
                if (!Valid(putOld, source)) return false;

                // Preserve object identity, charge, durability and genetics; never clone/stack/destroy.
                if (putOld.AcceptUnchecked() != 1) return false;
                // Returning the old item can trigger native shape/state callbacks. Revalidate.
                putNew = target.TryInventorySlot(incoming, 1, incoming.modifiedShape);
                if (!Valid(putNew, target) || putNew.AcceptUnchecked() != 1) return false;
                success = incoming.parentInventory == target && old.parentInventory == source;
                return success;
            }
            finally
            {
                if (!success)
                {
                    // Clear partially committed destinations before restoring exact original shapes.
                    if (old.parentInventory != target) old.parentInventory?.Expel(old);
                    if (incoming.parentInventory != source) incoming.parentInventory?.Expel(incoming);
                    Restore(incoming, source, incomingShape);
                    Restore(old, target, oldShape);
                }
            }
        }

        private static bool Valid(SlotMarker marker, GameInventory inventory) => marker != null
            && marker.inventory == inventory && marker.targetItem == null
            && marker.numTransfer == 1 && marker.itemGridShape != null;

        private static void Restore(GameItem item, GameInventory inventory, GridShape shape)
        {
            item.modifiedShape = shape;
            if (item.parentInventory != inventory && !inventory.UncheckedAccept(item))
                throw new InvalidOperationException("QuickReplace could not restore an original inventory position.");
            item.Validate();
        }
    }
}
