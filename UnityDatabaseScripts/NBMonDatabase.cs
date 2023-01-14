using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

public class NBMonDatabase
{
    public enum NBMonTierType
    {
        Origin,
        Wild,
        Hybrid
    }

    //Stores relevant information related to the monster Database
    public ElementDatabase elementDatabase;
    public PassiveDatabase passiveDatabase;
    public SkillsDataBase allSkills;
    public StatusEffectIconDatabase statusEffectIconDatabase;
    public NBMonProperties nBMonProperties;


    [System.Serializable]
    public class MonsterInfoPlayFab
    {
        public ObjectId _id { get; set; }
        public string monsterName;
        public string monsterDescription;
        public float baseEXP;
        public NBMonTierType Tier;
        public MonsterBaseStat monsterBaseStat;
        public List<ElementDatabase.Elements> elements, mutationElements;
        public List<NBMonDatabase.MonsterBaseSkillPassive> baseSkillAndPassive;
        public int RealmShardsMinimumm;
        public int RealmShardsMaximum;
        public int RealmCoinMinimum = 1;
        public int RealmCoinMaximum = 2;
        public List<MonsterLoot> LootLists;
    }
    
    [System.Serializable]
    public class PlayerItemSlotsData
    {
        public string ItemName;
        public string PlayFabItemID;
        public int Quantity;
    }

    [System.Serializable]
    public class MonsterLoot
    {
        public float RNGChance;
        public PlayerItemSlotsData ItemDrop;
    }

    public List<MonsterInfoPlayFab> monsters;

    public class MonstersPlayFabList
    {
        public List<MonsterInfoPlayFab> monstersPlayFab;
    }

    //Related with monster stats, skills, passives
    [System.Serializable]
    public class MonsterBaseStat
    {
        public int maxHpBase = 20;
        public int maxEnergyBase = 20;
        public int speedBase = 20;
        public int attackBase = 20;
        public int specialAttackBase = 20;
        public int defenseBase = 20;
        public int specialDefenseBase = 20;

    }
    [System.Serializable]
    public class MonsterBaseSkillPassive
    {
        public List<string> allBaseSkill;
        public List<string> allBasePassive;

    }


    //Related with monster type
    public enum GrowthLevel
    {
        Slow,
        Medium,
        Fast
    }

    //Returns a way to find the NBMons From The Database
    public static MonsterInfoPlayFab FindMonster(string monsterName)
    {
        // //============================================================
        // // MONGODB Logic
        // //============================================================
        // MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // //Let's create a filter to query single data
        // var filter = Builders<BsonDocument>.Filter.Eq("monsterName", monsterName);
        // //Setting for Collection
        // var collection = MongoHelper.db.GetCollection<BsonDocument>("monsterData").Find(filter).FirstOrDefault().AsEnumerable();
        // var monsterData = new MonsterInfoPlayFab();

        // //Convert the Result into desire Class
        // monsterData = BsonSerializer.Deserialize<MonsterInfoPlayFab>(collection.ToBsonDocument());
        // return monsterData;

        //============================================================
        // ORIGINAL Logic
        //============================================================    
        MonstersPlayFabList tempData = new MonstersPlayFabList();

        //Convertion from Json to Class
        var monsterDatabaseJson = NBMonDatabaseJson.MonsterDatabaseJson;
        tempData = JsonConvert.DeserializeObject<MonstersPlayFabList>(monsterDatabaseJson);

        //Make the original variable filled with the converted data.
        var monsters = tempData.monstersPlayFab;

        foreach (var monster in monsters)
        {
            if (monsterName == monster.monsterName)
            {
                return monster;
            }
        }
        
        return null;
    }


    //EXP Table Related for Demo Purpose
    public class EXPTable
    {
        public int level;
        public int expRequired;
    }

    public static string demoEXPTable = "[{\"level\":1,\"expRequired\":200},{\"level\":2,\"expRequired\":400},{\"level\":3,\"expRequired\":600},{\"level\":4,\"expRequired\":800},{\"level\":5,\"expRequired\":1150},{\"level\":6,\"expRequired\":1500},{\"level\":7,\"expRequired\":1850},{\"level\":8,\"expRequired\":2200},{\"level\":9,\"expRequired\":2550},{\"level\":10,\"expRequired\":2900},{\"level\":11,\"expRequired\":3250},{\"level\":12,\"expRequired\":3600},{\"level\":13,\"expRequired\":3950},{\"level\":14,\"expRequired\":4300},{\"level\":15,\"expRequired\":4800},{\"level\":16,\"expRequired\":5300},{\"level\":17,\"expRequired\":5800},{\"level\":18,\"expRequired\":6300},{\"level\":19,\"expRequired\":6800},{\"level\":20,\"expRequired\":7300},{\"level\":21,\"expRequired\":7800},{\"level\":22,\"expRequired\":8300},{\"level\":23,\"expRequired\":8800},{\"level\":24,\"expRequired\":9300},{\"level\":25,\"expRequired\":10000}]";

    public static List<EXPTable> DemoEXPTableData()
    {
        return JsonConvert.DeserializeObject<List<EXPTable>>(demoEXPTable);
    }
}
