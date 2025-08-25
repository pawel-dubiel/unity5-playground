public sealed class SlavsGameSnapshot
{
    public int TurnIndex { get; }
    public string Season { get; }
    public int Population { get; }
    public int Warriors { get; }
    public int FoodStores { get; }
    public int Livestock { get; }
    public int Prestige { get; }
    public int Faith { get; }
    public int Luck { get; }

    public SlavsGameSnapshot(
        int turnIndex, string season, int population, int warriors, int foodStores,
        int livestock, int prestige, int faith, int luck)
    {
        TurnIndex = turnIndex; Season = season; Population = population; Warriors = warriors;
        FoodStores = foodStores; Livestock = livestock; Prestige = prestige; Faith = faith; Luck = luck;
    }
}

