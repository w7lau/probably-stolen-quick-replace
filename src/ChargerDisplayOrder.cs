using System.Collections.Generic;
using HarmonyLib;

namespace QuickReplace
{
    // The charger's casing sprites and indicator lights enumerate childItems, while its
    // window uses grid coordinates. Native Expel/UncheckedAccept appends the replacement
    // at the end of the backing list, even when it occupies the original grid cells.
    [HarmonyPatch(typeof(GameGridInventory), "get_childItems")]
    internal static class ChargerDisplayOrder
    {
        private static void Postfix(GameGridInventory __instance, ref List<GameItem> __result)
        {
            if (!ModEntry.Enabled || __result == null || __result.Count < 2
                || __instance.GetParentItem()?.IsID("recharger") != true) return;

            // Return a view in native grid order. Do not move items, mutate the backing list,
            // expel/reinsert anything, or fire inventory callbacks just to refresh a display.
            var ordered = new List<GameItem>(__result);
            // Stable insertion sort also preserves the native order of equal coordinates.
            for (int i = 1; i < ordered.Count; i++)
            {
                var item = ordered[i];
                int j = i - 1;
                while (j >= 0 && Compare(ordered[j], item) > 0)
                {
                    ordered[j + 1] = ordered[j];
                    j--;
                }
                ordered[j + 1] = item;
            }
            __result = ordered;
        }

        private static int Compare(GameItem a, GameItem b)
        {
            var first = a?.modifiedShape;
            var second = b?.modifiedShape;
            if (first == null) return second == null ? 0 : 1;
            if (second == null) return -1;
            int row = first.minY.CompareTo(second.minY);
            return row != 0 ? row : first.minX.CompareTo(second.minX);
        }
    }
}
