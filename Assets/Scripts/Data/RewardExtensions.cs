using System.Collections.Generic;

public static class RewardExtensions
{
	/// <summary>
	/// Coins for finishing a track with <paramref name="stars"/> stars when <paramref name="baseStars"/> were
	/// already earned before; <paramref name="rewardData"/> maps a star number (1-3) to its coins.
	/// </summary>
	public static int GetReward(this Dictionary<int, int> rewardData, int baseStars, int stars)
	{
		if (baseStars >= stars || baseStars >= 4 || stars >= 4)
		{
			return 0;
		}
		int coins = 0;
		for (int i = 0; i < stars; i++)
		{
			coins += rewardData[i + 1];
		}
		return coins;
	}
}
