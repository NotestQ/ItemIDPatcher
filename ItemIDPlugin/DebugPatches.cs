using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Mono.Cecil;
using Zorro.Core;
using Zorro.Core.CLI;


namespace ItemIDPlugin
{
    //[HarmonyPatch(typeof(ShopHandler))]
    [HarmonyPatch(typeof(HotbarSlotUI))]
    internal static class DebugPatches
    {
        [HarmonyPrefix]

        [HarmonyPatch("SetData", [typeof(InventorySlot), typeof(bool)])]
        public static void setdata(ref InventorySlot slot)
        {
            /*if (slot.ItemInSlot.item != null)
            {
                ItemIDPlugin.Logger.LogError(
                    $"!! {Enumerable.FirstOrDefault<char>(slot.ItemInSlot.item.name)}, {slot.ItemInSlot.item.name}");
            }*/
        }
        /*[HarmonyPrefix]
        [HarmonyPatch("BuyItem", [typeof(ShoppingCart)])]
        public static void buyitem(ShopHandler __instance)
        {
            foreach (var VARIABLE in __instance.GetItemsInCart())
            {
                /*ItemDatabase.TryGetItemFromID(VARIABLE.ItemID, out Item item);
                ItemIDPlugin.Logger.LogError($"{VARIABLE.DisplayName} ({VARIABLE.Item.name}) = {item.displayName} ({item.name}) : ShopItem.ItemID {VARIABLE.ItemID} = ShopItem.Item.id {VARIABLE.Item.id} = TryGetItemFromID {item.id}");
                */
        /*ItemIDPlugin.Logger.LogError($"{VARIABLE.DisplayName} ({VARIABLE.Item.name}) : ShopItem.ItemID {VARIABLE.ItemID} = ShopItem.Item.id {VARIABLE.Item.id}");
      }
    }

    [HarmonyPrefix]
    [HarmonyPatch("BuyItem", [typeof(int), typeof(int[]), typeof(float), typeof(float), typeof(float)])]
    public static void buyitem(ref int[] itemIDs)
    {
        int[] newIDs = new int[itemIDs.Length];
        foreach (var VARIABLE in itemIDs)
        {
            /*ItemDatabase.TryGetItemFromID(VARIABLE, out Item item);
            ItemIDPlugin.Logger.LogError($"{VARIABLE} : {item.displayName} ({item.name})");*/
        /*ItemIDPlugin.Logger.LogError($"{VARIABLE}");
        }
        ItemIDPlugin.Logger.LogWarning("===============================");
        for (int i = 0; i < itemIDs.Length; i++)
        {
            newIDs[i] = itemIDs[i];
            Item item;
            if (ItemDatabase.TryGetItemFromID(itemIDs[i], out item))
            {
                ItemIDPlugin.Logger.LogWarning("Buying " + item.name);
            }
        }

        foreach (var VARIABLE in newIDs)
        {
            ItemIDPlugin.Logger.LogError($"{VARIABLE}");
        }
        itemIDs = newIDs;
    }
}

[HarmonyPatch(typeof(ItemDatabase))]
internal static class FileName2
{
    [HarmonyPrefix]
    [HarmonyPatch("TryGetItemFromID")]
    public static void trygetitemfromid(ref int id)
    {
        ItemIDPlugin.Logger.LogWarning("===========================================");
        foreach (Item item2 in SingletonAsset<ItemDatabase>.Instance.Objects)
        {
            ItemIDPlugin.Logger.LogError($"{item2.id} ({item2.name} | {item2.displayName}) == {id} : {item2.id == id}");
            if (item2.id == id)
                break;
        }
        ItemIDPlugin.Logger.LogWarning("===========================================");
    }
}*/
    }
}