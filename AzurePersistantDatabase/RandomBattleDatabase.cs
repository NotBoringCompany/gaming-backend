using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

public class NBMonBattleDatabase
{
    public ObjectId _id { get; set; }
    public int dataId;
    //Monster Level Range
    public int LevelRange;
    //Information of The Monster
    public List<NBMonData> MonsterDatas;
}

public class NBMonData
{
    public string MonsterID;
    public string MonsterNickname;
    public List<string> EquipSkill;
    public List<string> InheritedSkill;
    public List<string> Passive;
}

public class RandomBattleDatabase
{   
    public static NBMonBattleDatabase GetWildBattleData(int dataId)
    {
        List<NBMonBattleDatabase> wildBattleDatabase = JsonConvert.DeserializeObject<List<NBMonBattleDatabase>>(RandomBattleDatabaseJsonString);
        return dataId < wildBattleDatabase.Count? wildBattleDatabase[dataId] : wildBattleDatabase[0];
        //MongoDB
        
        // // Set the MongoDB server API version
        // MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        
        // // Query the collection for the desired data
        // var filter = Builders<NBMonBattleDatabase>.Filter.Eq(x => x.dataId, dataId);
        // var collection = MongoHelper.db.GetCollection<NBMonBattleDatabase>("wildData");
        // var wildBattleData = collection.Find(filter).FirstOrDefault();
        
        // return wildBattleData;
    }


    //Database JsonString
    public static string RandomBattleDatabaseJsonString = "[{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Pfufu\",\"monsterNickname\":\"Pfufu\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Tsunami\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Hydration\"]},{\"monsterID\":\"Pfufu\",\"monsterNickname\":\"Pfufu\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Tsunami\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Hydration\"]}],\"dataId\":0},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Roggo\",\"monsterNickname\":\"Roggo\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Double Down (Sp Attack)\",\"Defense Dance\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Roggo\",\"monsterNickname\":\"Roggo\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Double Down (Sp Attack)\",\"Defense Dance\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":1},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Trufo\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Paralysis Dance\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Trufo\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Paralysis Dance\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":2},{\"levelRange\":9,\"monsterDatas\":[{\"monsterID\":\"Wild Boar\",\"monsterNickname\":\"Wild Boar\",\"equipSkill\":[\"Poison Breath\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Wild Boar\",\"monsterNickname\":\"Wild Boar\",\"equipSkill\":[\"Poison Breath\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":3},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Peanut\",\"monsterNickname\":\"Peanut\",\"equipSkill\":[\"Seismic Wave\",\"Tail Whip\",\"Acrobatics\",\"Double Down (Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]},{\"monsterID\":\"Peanut\",\"monsterNickname\":\"Peanut\",\"equipSkill\":[\"Seismic Wave\",\"Tail Whip\",\"Acrobatics\",\"Double Down (Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]}],\"dataId\":4},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Firefly\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Defense Dance\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Firefly\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Defense Dance\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":5},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Prawdek\",\"monsterNickname\":\"Prawdek\",\"equipSkill\":[\"Head Slam\",\"Toxic Breath\",\"Slap\",\"Cleanse\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity (Enhanced)\"]},{\"monsterID\":\"Prawdek\",\"monsterNickname\":\"Prawdek\",\"equipSkill\":[\"Head Slam\",\"Toxic Breath\",\"Slap\",\"Cleanse\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity (Enhanced)\"]}],\"dataId\":6},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Morath\",\"monsterNickname\":\"Morath\",\"equipSkill\":[\"Tail Whip (Reptile)\",\"Poison Fangs\",\"Slap\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Skin Shed\"]},{\"monsterID\":\"Morath\",\"monsterNickname\":\"Morath\",\"equipSkill\":[\"Tail Whip (Reptile)\",\"Poison Fangs\",\"Slap\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Skin Shed\"]}],\"dataId\":7},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Fairwoole\",\"monsterNickname\":\"Fairwoole\",\"equipSkill\":[\"Sleep Powder\",\"Slap\",\"Slash\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Sparkle Absorption\"]},{\"monsterID\":\"Fairwoole\",\"monsterNickname\":\"Fairwoole\",\"equipSkill\":[\"Sleep Powder\",\"Slap\",\"Slash\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Sparkle Absorption\"]}],\"dataId\":8},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Razer\",\"monsterNickname\":\"Razer\",\"equipSkill\":[\"Claw Slash\",\"Tail Whip (Spirit)\",\"Double Down (Attack)\",\"Acrobatics\"],\"inheritedSkill\":[],\"Passive\":[\"Razer Sharp\",\"Ultra Speed\"]},{\"monsterID\":\"Razer\",\"monsterNickname\":\"Razer\",\"equipSkill\":[\"Claw Slash\",\"Tail Whip (Spirit)\",\"Double Down (Attack)\",\"Acrobatics\"],\"inheritedSkill\":[],\"Passive\":[\"Razer Sharp\",\"Ultra Speed\"]}],\"dataId\":9},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Schoggi\",\"monsterNickname\":\"Schoggi\",\"equipSkill\":[\"Defense Dance\",\"Slap\",\"Slash\",\"Acrobatics\"],\"inheritedSkill\":[],\"Passive\":[\"Resilience\"]},{\"monsterID\":\"Schoggi\",\"monsterNickname\":\"Schoggi\",\"equipSkill\":[\"Defense Dance\",\"Slap\",\"Slash\",\"Acrobatics\"],\"inheritedSkill\":[],\"Passive\":[\"Resilience\"]}],\"dataId\":10},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Milnas\",\"monsterNickname\":\"Milnas\",\"equipSkill\":[\"Scratch\",\"Dragon Dance\"],\"inheritedSkill\":[],\"Passive\":[\"Attack Push\"]}],\"dataId\":11},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Birvo\",\"monsterNickname\":\"Birvo\",\"equipSkill\":[\"Slap\",\"Dexterity\"],\"inheritedSkill\":[],\"Passive\":[\"Speed Push\"]}],\"dataId\":12},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Heree\",\"monsterNickname\":\"Heree\",\"equipSkill\":[\"Slap\",\"Heal\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]}],\"dataId\":13},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Trufo (Purple)\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Paralysis Dance\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Trufo (Purple)\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Paralysis Dance\",\"Head Slam\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":14},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Firefly (Blue)\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Defense Dance\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Firefly (Blue)\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Small Chop\",\"Slash\",\"Defense Dance\",\"Double Down (Sp Attack)\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":15}]";
}
