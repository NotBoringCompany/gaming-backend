using System.Collections.Generic;
using Newtonsoft.Json;

public class QuestRewardDatabaseJson
{
    public static string questRewardJson = "[{\"questId\":1,\"questName\":\"A Fascinating Trio\",\"shardReward\":100,\"coinReward\":0,\"itemRewards\":[]},{\"questId\":2,\"questName\":\"A Small Detour\",\"shardReward\":0,\"coinReward\":0,\"itemRewards\":[{\"itemName\":\"Small Healing Potion\",\"itemQuantity\":5},{\"itemName\":\"Small Energy Potion\",\"itemQuantity\":5}]},{\"questId\":3,\"questName\":\"The Man In Black\",\"shardReward\":100,\"coinReward\":0,\"itemRewards\":[{\"itemName\":\"Complete Recovery Potion\",\"itemQuantity\":0}]},{\"questId\":10,\"questName\":\"Danger, Ranger!\",\"shardReward\":250,\"coinReward\":0,\"itemRewards\":[]}]";
}

public class QuestRewardDatabase
{
    public static QuestData FindQuestUsingID(int questID)
    {
        List<QuestData> convertedQuestData = JsonConvert.DeserializeObject<List<QuestData>>(QuestRewardDatabaseJson.questRewardJson);

        foreach(var data in convertedQuestData)
        {
            if(data.questId == questID)
                return data;
        }

        return null;
    }
}

//Data Structure
[System.Serializable]
public class QuestData{
    public int questId;
    public string questName;
    public float shardReward;
    public float coinReward;
    public List<ItemReward> itemRewards;
}

[System.Serializable]
public class ItemReward{
    public string itemName;
    public int itemQuantity;

}