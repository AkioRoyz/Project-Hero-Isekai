using System;

[Serializable]
public struct RewardData
{
    public int Exp;

    // Используй это поле для выдачи золота.
    // Не добавляй золото как Item в массив Items.
    public int Gold;

    public RewardItemData[] Items;

    public RewardData(int exp, int gold)
    {
        Exp = exp;
        Gold = gold;
        Items = Array.Empty<RewardItemData>();
    }

    public RewardData(int exp, int gold, RewardItemData[] items)
    {
        Exp = exp;
        Gold = gold;
        Items = items ?? Array.Empty<RewardItemData>();
    }
}