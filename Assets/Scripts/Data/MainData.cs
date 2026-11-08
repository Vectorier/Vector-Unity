using System;
using Banzai.Json;

/// <summary>The player's coin balance.</summary>
public class MainData : BaseUserHolder<MainData>
{
	private int _coins;

	protected sealed override string JSONName => "MainData";

	/// <summary>(old balance, new balance)</summary>
	public event Action<int, int> CoinsChangedEvent;

	public override void ParseData()
	{
		_coins = _userjObject.GetInt("Coins");
	}

	public override void SaveData()
	{
		_userjObject["Coins"] = _coins;
		UserDataManager.Instance.SaveUserDate();
	}

	public void SetCoins(int newValue)
	{
		CoinsChangedEvent?.Invoke(_coins, newValue);
		_coins = newValue;
		SaveData();
	}

	public void AddCoins(int value)
	{
		SetCoins(_coins + value);
	}

	public int GetCoins()
	{
		return _coins;
	}
}
