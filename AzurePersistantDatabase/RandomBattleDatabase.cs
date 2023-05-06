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
    public static string RandomBattleDatabaseJsonString = "[{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Pfufu\",\"monsterNickname\":\"Pfufu\",\"equipSkill\":[\"Ram\",\"Cry\",\"Aqua Burst\"],\"inheritedSkill\":[],\"Passive\":[\"Hydration\"]},{\"monsterID\":\"Pfufu\",\"monsterNickname\":\"Pfufu\",\"equipSkill\":[\"Ram\",\"Cry\",\"Aqua Burst\"],\"inheritedSkill\":[],\"Passive\":[\"Hydration\"]}],\"dataId\":0},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Roggo\",\"monsterNickname\":\"Roggo\",\"equipSkill\":[\"Bump\",\"Cry\",\"Life Drain\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Roggo\",\"monsterNickname\":\"Roggo\",\"equipSkill\":[\"Bump\",\"Cry\",\"Life Drain\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":1},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Trufo\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Life Drain\",\"Startle Strike\",\"Toxic Cloud\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Trufo\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Life Drain\",\"Startle Strike\",\"Toxic Cloud\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":2},{\"levelRange\":7,\"monsterDatas\":[{\"monsterID\":\"Burbuff\",\"monsterNickname\":\"Burbuff\",\"equipSkill\":[\"Ram\",\"Intimidating Stare\",\"Thrust\",\"Provoke\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Burbuff\",\"monsterNickname\":\"Burbuff\",\"equipSkill\":[\"Ram\",\"Intimidating Stare\",\"Thrust\",\"Provoke\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":3},{\"levelRange\":7,\"monsterDatas\":[{\"monsterID\":\"Peancoon\",\"monsterNickname\":\"Peancoon\",\"equipSkill\":[\"Ram\",\"Cry\",\"Defense Dance\",\"Reckless Charge\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]},{\"monsterID\":\"Peanut\",\"monsterNickname\":\"Peancoon\",\"equipSkill\":[\"Ram\",\"Cry\",\"Defense Dance\",\"Reckless Charge\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]}],\"dataId\":4},{\"levelRange\":6,\"monsterDatas\":[{\"monsterID\":\"Raixie\",\"monsterNickname\":\"Raixie\",\"equipSkill\":[\"Magical Energy\",\"Defense Dance\",\"Hypnotic Gaze\",\"Magical Beam\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Raixie\",\"monsterNickname\":\"Raixie\",\"equipSkill\":[\"Magical Energy\",\"Defense Dance\",\"Hypnotic Gaze\",\"Magical Beam\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":5},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Pippleaf\",\"monsterNickname\":\"Pippleaf\",\"equipSkill\":[\"Head Slam\",\"Slap\",\"Cleanse\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Pippleaf\",\"monsterNickname\":\"Pippleaf\",\"equipSkill\":[\"Head Slam\",\"Slap\",\"Cleanse\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":6},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Morath\",\"monsterNickname\":\"Morath\",\"equipSkill\":[\"Sand Attack\",\"Venomous Whip\",\"Focus\",\"Bite\"],\"inheritedSkill\":[],\"Passive\":[\"Skin Shed\"]},{\"monsterID\":\"Morath\",\"monsterNickname\":\"Morath\",\"equipSkill\":[\"Sand Attack\",\"Venomous Whip\",\"Focus\",\"Bite\"],\"inheritedSkill\":[],\"Passive\":[\"Skin Shed\"]}],\"dataId\":7},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Fairwoole\",\"monsterNickname\":\"Fairwoole\",\"equipSkill\":[\"Ram\",\"Pixie Gust\",\"Root Bind\",\"Mystic Barrier\"],\"inheritedSkill\":[],\"Passive\":[\"Sparkle Absorption\"]},{\"monsterID\":\"Fairwoole\",\"monsterNickname\":\"Fairwoole\",\"equipSkill\":[\"Ram\",\"Pixie Gust\",\"Root Bind\",\"Mystic Barrier\"],\"inheritedSkill\":[],\"Passive\":[\"Sparkle Absorption\"]}],\"dataId\":8},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Razer\",\"monsterNickname\":\"Razer\",\"equipSkill\":[\"Thunder Strike\",\"Flame Strike\",\"Frost Strike\",\"Shadow Step\"],\"inheritedSkill\":[],\"Passive\":[\"Razer Sharp\",\"Ultra Speed\"]},{\"monsterID\":\"Razer\",\"monsterNickname\":\"Razer\",\"equipSkill\":[\"Thunder Strike\",\"Flame Strike\",\"Frost Strike\",\"Shadow Step\"],\"inheritedSkill\":[],\"Passive\":[\"Razer Sharp\",\"Ultra Speed\"]}],\"dataId\":9},{\"levelRange\":9,\"monsterDatas\":[{\"monsterID\":\"Pippleaf\",\"monsterNickname\":\"Pippleaf\",\"equipSkill\":[\"Scratch\",\"Distraction\"],\"inheritedSkill\":[],\"Passive\":[\"Attack Push\"]},{\"monsterID\":\"Racoon\",\"monsterNickname\":\"Racoon\",\"equipSkill\":[\"Pippleaf\",\"Pippleaf\"],\"inheritedSkill\":[],\"Passive\":[\"Attack Push\"]}],\"dataId\":10},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Cicoru\",\"monsterNickname\":\"Cicoru\",\"equipSkill\":[\"Wind Slicer\",\"Intimidating Stare\",\"Flame Burst\",\"Protective Aura\"],\"inheritedSkill\":[],\"Passive\":[\"Attack Push\"]}],\"dataId\":11},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Toots\",\"monsterNickname\":\"Toots\",\"equipSkill\":[\"Aqua Burst\",\"Distraction\",\"Boost\",\"Hydro Bubble\"],\"inheritedSkill\":[],\"Passive\":[\"Speed Push\"]}],\"dataId\":12},{\"levelRange\":5,\"monsterDatas\":[{\"monsterID\":\"Deerbie\",\"monsterNickname\":\"Deerbie\",\"equipSkill\":[\"Quick Attack\",\"Intimidating Stare\",\"Boost\",\"Magic Leaf\"],\"inheritedSkill\":[],\"Passive\":[\"Defense Push\"]}],\"dataId\":13},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Trufo (Purple)\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Slumber Mist\",\"Toxic Cloud\",\"Vitality Drain\",\"Lunar Glow\"],\"inheritedSkill\":[],\"Passive\":[]},{\"monsterID\":\"Trufo (Purple)\",\"monsterNickname\":\"Trufo\",\"equipSkill\":[\"Slumber Mist\",\"Toxic Cloud\",\"Vitality Drain\",\"Lunar Glow\"],\"inheritedSkill\":[],\"Passive\":[]}],\"dataId\":14},{\"levelRange\":4,\"monsterDatas\":[{\"monsterID\":\"Firefly (Blue)\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Magical Headbutt\",\"Mystic Barrier\",\"Bind\",\"Magical Beam\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]},{\"monsterID\":\"Firefly (Blue)\",\"monsterNickname\":\"Firefly\",\"equipSkill\":[\"Magical Headbutt\",\"Mystic Barrier\",\"Bind\",\"Magical Beam\"],\"inheritedSkill\":[],\"Passive\":[\"Toxic Immunity\"]}],\"dataId\":15}]";
}
