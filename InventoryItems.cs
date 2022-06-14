using System.Collections;
using System.Collections.Generic;

public enum ItemCategory
{
    Usable,
    Capture,
    Artifacts,
    Key
}

public enum ArtifactType
{
    Hold_Item,
    Shared_Item
}

public enum ConsumableType
{
    UseAnywhere,
    BattleOnly
}

public enum CaptureItemType
{
    Null,
    NBMonCaptureItem,
    NBMonLureItem,
    NBMonDistractionItem
}

public enum ArtifactTier
{
    E,
    D,
    C,
    B,
    A,
    S
}

[System.Serializable]
public class ItemsPlayFab
{
    public string Name;
    public ItemCategory Type;
    public string Information;

    public ConsumableType ConsumableType;
    public int HPRecovery;
    public int HPRecovery_Percentage;
    public int EnergyRecover;
    public int EnergyRecover_Percentage;
    public List<NBMonProperties.StatusEffectInfo> AddStatusEffects;
    public List<NBMonProperties.StatusEffectInfo> RemovesStatusEffects;

    public CaptureItemType CaptureItemType;
    public float CaptureRate = 0.1f;
    public ArtifactTier ArtifactTier;
    public ArtifactType ArtifactType;
    public string ArtifactEffect;
}
public class InventoryItemsPlayFabLists
{
    public List<ItemsPlayFab> ItemDataBasePlayFab;
    public List<ItemsPlayFab> ArtifactDataBasePlayFab;
}

public class InventoryItems
{
    public List<ItemsPlayFab> ItemDataBase;

    public List<ItemsPlayFab> ArtifactDatabase;

    public ItemsPlayFab FindItemFromDataBase(string ItemName)
    {
        foreach (var Items in ItemDataBase)
        {
            if (Items.Name == ItemName)
            {
                return Items;
            }
        }

        return null;
    }

    public ItemsPlayFab FindArtifactFromDataBase(string ItemName)
    {
        foreach (var Items in ArtifactDatabase)
        {
            if (Items.Name == ItemName)
            {
                return Items;
            }
        } 

        return null;
    }
}
