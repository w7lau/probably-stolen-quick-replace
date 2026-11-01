using System.Collections.Generic;

namespace QuickReplace
{
    internal static class ChargerTargets
    {
        internal static int Energy(GameItem item)
            => item.GetTagReadonly(PowerHelper.POWER_SOURCE_ITEM_ENERGY).GetInt();

        private static bool Rechargeable(GameItem item)
            => item != null && item.IsTag(PowerHelper.POWER_SOURCE_ITEM_TAG)
                && !PowerHelper.IsDisposablePowerSource(item)
                && item.GetTagReadonly(PowerHelper.POWER_SOURCE_ITEM_MAX_ENERGY).GetInt() > 0;

        internal static GameItem Find(GameItem incoming, GameItem hovered, GameInventory hoveredInventory)
        {
            // Collect a battery only when it has more remaining energy than the incoming one.
            // Dragging within the same charger remains ordinary rearrangement.
            if (!Rechargeable(incoming) || incoming.unitCount != 1) return null;
            var charger = hovered != null && hovered.IsID("recharger")
                ? hovered : (hovered?.parentInventory ?? hoveredInventory)?.GetParentItem();
            if (charger == null || !charger.IsID("recharger") || !GeneralHelper.IsItemOwned(charger)
                || incoming.parentInventory?.GetParentItem() == charger) return null;

            var inventories = new List<GameInventory>();
            foreach (var child in charger.children) Inventories(child, charger, inventories);
            GameItem best = null;
            int bestEnergy = Energy(incoming);
            foreach (var inventory in inventories)
                foreach (var candidate in inventory.childItems)
                {
                    if (!Rechargeable(candidate) || !SwapLogic.Eligible(incoming, candidate)) continue;
                    int energy = Energy(candidate);
                    // Rank by remaining kJ, not capacity or percentage. Keep traversal order on ties.
                    if (energy > bestEnergy) { best = candidate; bestEnergy = energy; }
                }
            return best;
        }

        private static void Inventories(GraphNodeStorage node, GameItem owner, List<GameInventory> found)
        {
            if (node == null || (node is GameItem && node != owner)) return;
            if (node is GameInventory inventory)
            {
                if (inventory.GetParentItem() == owner) found.Add(inventory);
                return; // Never search inside stored items or nested bags.
            }
            foreach (var child in node.children) Inventories(child, owner, found);
        }
    }
}
