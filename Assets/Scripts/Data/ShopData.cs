using System;
using System.Collections.Generic;
using System.Linq;
using Banzai.Json;
using Newtonsoft.Json.Linq;

public class ShopData : BaseUserHolder<ShopData>
{
	private Dictionary<string, OwnedItem> _items = new Dictionary<string, OwnedItem>();

	protected override string JSONName => "ShopData";

	public event Action<ShopData> Updated;

	public override void ParseData()
	{
		_items.Clear();
		var token = _userjObject["Items"];
		if (token == null)
		{
			return;
		}
		if (token.Type == JTokenType.String)
		{
			// Old saves stored a JSON string holding [{ "id", "count", "isEquip" }, ...].
			foreach (var item in JArray.Parse(token.AsString()))
			{
				var id = (string)item["id"];
				_items[id] = new OwnedItem(id, (int?)item["count"] ?? 0, (bool?)item["isEquip"] ?? false);
			}
			return;
		}
		foreach (var item in token.Children<JProperty>())
		{
			_items[item.Name] = new OwnedItem(item.Name, (int?)item.Value["Count"] ?? 0, (bool?)item.Value["Equipped"] ?? false);
		}
	}

	// Saved as "Items": { "<item id>": { "Count": 1, "Equipped": true }, ... }
	public override void SaveData()
	{
		var items = new JObject();
		foreach (var item in _items.Values.OrderBy(i => i.id, StringComparer.Ordinal))
		{
			items[item.id] = new JObject { ["Count"] = item.count, ["Equipped"] = item.isEquip };
		}
		_userjObject["Items"] = items;
		UserDataManager.Instance.SaveUserDate();
		Updated?.Invoke(this);
	}

	public void Add(string itemId, int count, bool equip = true, bool saveDataNow = true)
	{
		if (!_items.ContainsKey(itemId))
		{
			_items[itemId] = new OwnedItem(itemId, count, equip);
		}
		else
		{
			_items[itemId].AddCount(count, equip);
		}
		SaveData();
	}

	public int GetCount(string itemId)
	{
		if (!_items.ContainsKey(itemId))
		{
			return 0;
		}
		return _items[itemId].count;
	}

	public bool Contains(string itemId)
	{
		return _items.ContainsKey(itemId);
	}

	public bool IsBought(string itemId)
	{
        if (!_items.ContainsKey(itemId))
        {
            return false;
        }
        return _items[itemId].count > 0;
    }

	public bool IsEquipped(string itemId)
	{
        if (!_items.ContainsKey(itemId))
        {
            return false;
        }
        if (_items[itemId].count == 0)
        {
            return false;
        }
        return _items[itemId].isEquip;
    }

	public bool IsEquippedGadget(string gadgetID = "GADGET_FORCEBLASTER")
	{
        if (!_items.ContainsKey(gadgetID))
        {
            return false;
        }
		if (_items[gadgetID].count == 0)
		{
			return false;
		}
        return _items[gadgetID].isEquip;
    }

	public bool IsEquippedGadgetNotEmpty(string gadgetID = "GADGET_FORCEBLASTER")
	{
		if (IsEquipped(gadgetID))
		{
			return GetCount(gadgetID) > 0;
		}
		return false;
	}

	public int GetGadgetCount(string gadgetID = "GADGET_FORCEBLASTER")
	{
        if (!_items.ContainsKey(gadgetID))
        {
            return 0;
        }
        return _items[gadgetID].count;
    }

	public void Equip(string itemId)
	{
		_items[itemId].SetEquip(true);
		SaveData();
	}

	public void Unequip(string itemId)
	{
        _items[itemId].SetEquip(false);
        SaveData();
    }

	public void Consume(string itemId)
	{
		_items[itemId].Spend(1);
		SaveData();
	}

}
