using System.Collections.Generic;
using Newtonsoft.Json;

public class QuestRewardDatabaseJson
{
    public static string questRewardJson = 
    "[{\"questId\":1,\"questName\":\"Test Reward\",\"shardReward\":0,\"coinReward\":0,\"itemRewards\":[{\"itemName\":\"Ladder\",\"itemQuantity\":1}]},{\"questId\":2,\"questName\":\"Julia Egg Related\",\"shardReward\":0,\"coinReward\":0,\"itemRewards\":[{\"itemName\":\"Julia Egg\",\"itemQuantity\":1}]}]"
    ;    
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