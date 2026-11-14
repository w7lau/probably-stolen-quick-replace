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

        private static GameItem GetCharger(GameItem incoming, GameItem hovered, GameInventory hoveredInventory)
        {
            if (!Rechargeable(incoming) || incoming.unitCount != 1
                || !GeneralHelper.IsItemOwned(incoming) || !incoming.MayRemove()
                || incoming.MaxNumRemove() < 1) return null;
            // Auto-selection belongs only to a drop on the appliance body. Inside its
            // window, preserve native placement and battery-on-battery exchanges.
            var charger = hovered != null && hovered.IsID("recharger") ? hovered : null;
            if (charger == null || !charger.IsID("recharger") || !GeneralHelper.IsItemOwned(charger)
                || incoming.parentInventory?.GetParentItem() == charger) return null;
            return charger;
        }

        internal static SlotMarker FindSpace(GameItem incoming, GameItem hovered, GameInventory hoveredInventory)
        {
            var charger = GetCharger(incoming, hovered, hoveredInventory);
            if (charger == null) return null;
            var inventories = new List<GameInventory>();
            foreach (var child in charger.children) Inventories(child, charger, inventories);
            foreach (var inventory in inventories)
            {
                if (!GeneralHelper.MayPlayerInsertInto(inventory)) continue;
                // Use native shape/orientation rules, not the number of empty grid cells.
                var space = inventory.TryFindOneValidInventorySlot(incoming);
                if (space != null && space.inventory == inventory && space.targetItem == null
                    && space.numTransfer == 1 && space.itemGridShape != null) return space;
            }
            return null;
        }

        internal static GameItem Find(GameItem incoming, GameItem hovered, GameInventory hoveredInventory)
        {
            // Fill usable empty space before selecting any battery to exchange.
            // Dragging within the same charger remains ordinary rearrangement.
            var charger = GetCharger(incoming, hovered, hoveredInventory);
            if (charger == null || FindSpace(incoming, hovered, hoveredInventory) != null) return null;

            var inventories = new List<GameInventory>();
            foreach (var child in charger.children) Inventories(child, charger, inventories);
            GameItem best = null;
            int bestEnergy = Energy(incoming);
            GameItem bestFull = null;
            bool needsCharging = !PowerHelper.IsPowerSourceFull(incoming);
            foreach (var inventory in inventories)
                foreach (var candidate in inventory.childItems)
                {
                    if (!Rechargeable(candidate) || !SwapLogic.Eligible(incoming, candidate)) continue;
                    int energy = Energy(candidate);
                    // A completed battery can yield its charging space even when its
                    // capacity is smaller. Among completed batteries, take the most kJ.
                    if (needsCharging && PowerHelper.IsPowerSourceFull(candidate)
                        && (bestFull == null || energy > Energy(bestFull))) bestFull = candidate;
                    // Rank by remaining kJ, not capacity or percentage. Keep traversal order on ties.
                    if (energy > bestEnergy) { best = candidate; bestEnergy = energy; }
                }
            return bestFull ?? best;
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
