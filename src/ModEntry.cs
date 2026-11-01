using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QuickReplace
{
    public sealed class ModEntry : IMod
    {
        internal static bool Enabled;
        public void Init(ModManifest manifest) { new ModLog(manifest).Log("Quick Replace " + manifest.ModVersion); }
        public void OnEnable() { Enabled = true; }
        public void OnDisable() { Enabled = false; }
    }

    internal static class Targets
    {
        internal static void Slots(GraphNodeStorage node, GameItem owner, List<GameSlotInventory> found)
        {
            if (node == null) return;
            if (node is GameItem && node != owner) return; // Do not enter nested bags or mounted items.
            if (node is GameInventory inventory)
            {
                if (inventory is GameSlotInventory slot && slot.GetParentItem() == owner
                    && slot.mayInventoryAddItemFunc != null) found.Add(slot);
                return;
            }
            foreach (var child in node.children) Slots(child, owner, found);
        }

        internal static GameSlotInventory Find(GameItem incoming, GameItem hovered, GameInventory hoveredInventory)
        {
            var direct = hoveredInventory as GameSlotInventory ?? hovered?.parentInventory as GameSlotInventory;
            if (direct != null && (hovered == null || direct.childItem == hovered))
                return SwapLogic.Eligible(incoming, direct) ? direct : null;
            if (hovered == null || hovered == incoming || !GeneralHelper.IsItemOwned(hovered)) return null;
            var slots = new List<GameSlotInventory>();
            // Start below the item itself, but stop at every inventory/item boundary.
            foreach (var child in hovered.children) Slots(child, hovered, slots);
            // Preserve normal insertion when this device has an empty compatible slot.
            foreach (var slot in slots)
                if (slot.childItem == null && slot.MayHaveValidInventorySlot(incoming)) return null;
            foreach (var slot in slots)
                if (SwapLogic.Eligible(incoming, slot)) return slot;
            return null;
        }
    }

    [HarmonyPatch(typeof(ItemMouseDragHandler), "EndDrag")]
    internal static class ReplaceOnDrop
    {
        private static void Prefix(GameItemElement ___currentItem, bool ___isDragging,
            ref GameItem ___lastItem, ref GameInventory ___lastInventory, ref int ___currentRemove)
        {
            if (!ModEntry.Enabled || !___isDragging || ___currentRemove != 1 || ___currentItem == null
                || ___currentItem.IsDestroyed() || ___currentItem.isDebugMenu) return;
            // EndDrag is also called by reset/cancel. A reset clears both targets first.
            if (___lastItem == null && ___lastInventory == null) return;
            var old = ChargerTargets.Find(___currentItem, ___lastItem, ___lastInventory)
                ?? Targets.Find(___currentItem, ___lastItem, ___lastInventory)?.childItem;
            if (old == null) return;
            SwapLogic.TrySwap(___currentItem, old);
            // A rejected replacement is a cancelled drop, not a second native use action.
            ___currentRemove = 0;
            ___lastItem = null;
            ___lastInventory = null;
            // Let the native method finish its normal drag visual/audio cleanup.
        }
    }

    [HarmonyPatch(typeof(ItemMouseDragHandler), "Drag")]
    internal static class ReplaceHint
    {
        private static readonly RichTextBuilder Text = new RichTextBuilder(60);
        private static void Postfix(ItemMouseDragHandler __instance, GameItemElement ___currentItem,
            bool ___isDragging, GameItem ___lastItem, GameInventory ___lastInventory, int ___currentRemove)
        {
            if (!ModEntry.Enabled || !___isDragging || ___currentRemove != 1 || ___currentItem == null
                || ___currentItem.isDebugMenu) return;
            var charged = ChargerTargets.Find(___currentItem, ___lastItem, ___lastInventory);
            var old = charged ?? Targets.Find(___currentItem, ___lastItem, ___lastInventory)?.childItem;
            if (old == null) return;
            bool chinese = LocHelper.GetCurrentLocaleCode().StartsWith("zh", System.StringComparison.OrdinalIgnoreCase);
            Text.Clear();
            if (charged != null && chinese)
                Text.AddLine("松开鼠标，换出电量最多的电池")
                    .AddLine(charged.name + "（" + ChargerTargets.Energy(charged) + " kJ）")
                    .AddLine("拖入的留下充电，换出的放回原物品栏。")
                    .AddLine("放不下就不换。");
            else if (charged != null)
                Text.AddLine("Release to take the battery with the most charge")
                    .AddLine(charged.name + " (" + ChargerTargets.Energy(charged) + " kJ)")
                    .AddLine("The battery you dragged in stays to charge.")
                    .AddLine("The charged one returns to the source inventory.")
                    .AddLine("If there is no room, nothing is swapped.");
            else if (chinese)
                Text.AddLine("松开鼠标，换下「" + old.name + "」")
                    .AddLine("旧的放回原来的物品栏，放不下就不换。");
            else
                Text.AddLine("Release to replace " + old.name)
                    .AddLine("The old item goes back where you took the new one from.")
                    .AddLine("If there is no room, nothing is swapped.");
            OverlayHandler.current.ShowTooltip(Input.mousePosition, __instance.tooltipTranslation, Text.Build());
        }
    }
}
