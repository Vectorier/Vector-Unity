/// <summary>Global game state shared between the preloader, menus and gameplay.</summary>
public static class Game
{
    /// <summary>True once the preloader has loaded animations, sounds and data; scenes reload the preloader otherwise.</summary>
    public static bool Loaded;

    public static bool IsTrickBought(string trickId)
    {
        return UserDataManager.Instance.ShopData.IsBought(trickId);
    }

    public static bool IsItemBought(string itemId)
    {
        return UserDataManager.Instance.ShopData.IsBought(itemId);
    }

    public static bool IsItemEquipped(string itemId)
    {
        return UserDataManager.Instance.ShopData.IsEquipped(itemId);
    }
}
