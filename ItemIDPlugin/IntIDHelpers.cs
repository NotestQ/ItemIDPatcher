using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Wrapper or helper, technically and practically both, went with helper because it's not totally necessary, just nice to have
namespace ItemIDPlugin
{
    public static class ItemHelper
    {
        public static int GetItemID(Item item)
        {
            return item.id;
        }

        public static void SetItemID(Item item, int id)
        {
            item.id = id;
        }
    }

    public static class PickupHandlerHelper
    {
        public static Pickup CreatePickup(int itemID, ItemInstanceData data, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
        {
            return PickupHandler.CreatePickup(itemID, data, pos, rot, vel, angVel);
        }

        public static Pickup CreatePickup(int itemID, ItemInstanceData data, Vector3 pos, Quaternion rot)
        {
            return PickupHandler.CreatePickup(itemID, data, pos, rot);
        }
    }

    public static class ItemDatabaseHelper
    {
        public static bool TryGetItemFromID(int id, out Item item)
        {
            return ItemDatabase.TryGetItemFromID(id, out item);
        }
    }

    public static class ShopHandlerHelper
    {
        public static bool TryGetShopItem(ShopHandler shopHandler, int index, ref ShopItem item)
        {
            return shopHandler.TryGetShopItem(index, ref item);
        }
    }

    public static class ItemInstanceDataHelper
    {
        public static ItemDataEntry GetEntryType(ItemInstanceData itemInstanceData, int identifier)
        {
            return itemInstanceData.GetEntryType(identifier);
        }
    }
}
