using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

[System.Serializable]
public class FixedNBMonBattleDatabase
{
    public ObjectId _id { get; set; }
    //Owner Name Information
    public string OwnerName;
    //Monster Team Information
    public List<NBMonBattleDataSave> MonsterTeam;
    public int dataId;
}

public class FixedBattleDatabase
{
    public static List<FixedNBMonBattleDatabase> NPCBattleData;
    public static List<FixedNBMonBattleDatabase> BossBattleData;

    public static FixedNBMonBattleDatabase GetBattleData(string battleData, int dataId)
    {
        NPCBattleData = JsonConvert.DeserializeObject<List<FixedNBMonBattleDatabase>>(NPCBattleDatabaseJsonString);
        BossBattleData = JsonConvert.DeserializeObject<List<FixedNBMonBattleDatabase>>(BossBattleDatabaseJsonString);

        if(battleData == "npcData")
        {
            foreach(var npcData in NPCBattleData)
            {
                if(npcData.dataId == dataId)
                return npcData;
            }

            return NPCBattleData[0];
        }
        else
        {
            foreach(var bossData in BossBattleData)
            {
                if(bossData.dataId == dataId)
                return bossData;
            }

            return BossBattleData[0];
        }

        // // Set the MongoDB server API version
        // MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        // var collection = MongoHelper.db.GetCollection<FixedNBMonBattleDatabase>(battleData);
        // var filter = Builders<FixedNBMonBattleDatabase>.Filter.Eq(x => x.dataId, dataId);
        // var result = collection.Find(filter).FirstOrDefault();

        // return result;
    }

    public static string NPCBattleDatabaseJsonString = "[{\"ownerName\":\"Lech\",\"monsterTeam\":[{\"owner\":\"Lech\",\"nickName\":\"Morath\",\"monsterId\":\"Morath\",\"uniqueId\":\"Morath_Lech\",\"gender\":0,\"level\":15,\"fertility\":3000,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":4,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"newSkillLearned\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":1150,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Venomous Whip\",\"Earth Slam\",\"Bite\",\"Sandstorm\"],\"uniqueSkillList\":[],\"passiveList\":[\"Skin Shed\"],\"temporaryPassives\":[],\"hp\":105,\"energy\":30,\"maxHp\":105,\"maxEnergy\":30,\"speed\":81,\"battleSpeed\":81,\"attack\":79,\"specialAttack\":86,\"defense\":82,\"specialDefense\":83,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"absorbDamageValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":0,\"maxEnergyPotential\":0,\"speedPotential\":0,\"attackPotential\":0,\"specialAttackPotential\":0,\"defensePotential\":0,\"specialDefensePotential\":0,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0},{\"owner\":\"Lech\",\"nickName\":\"Razer\",\"monsterId\":\"Razer\",\"uniqueId\":\"Razer_Lech\",\"gender\":0,\"level\":15,\"fertility\":3000,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":4,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"newSkillLearned\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":1150,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Thunder Strike\",\"Flame Strike\",\"Frost Strike\",\"Shadow Step\"],\"uniqueSkillList\":[],\"passiveList\":[\"Razer Sharp\",\"Ultra Speed\"],\"temporaryPassives\":[],\"hp\":103,\"energy\":34,\"maxHp\":103,\"maxEnergy\":34,\"speed\":84,\"battleSpeed\":84,\"attack\":86,\"specialAttack\":81,\"defense\":78,\"specialDefense\":79,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"absorbDamageValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":0,\"maxEnergyPotential\":0,\"speedPotential\":0,\"attackPotential\":0,\"specialAttackPotential\":0,\"defensePotential\":0,\"specialDefensePotential\":0,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}],\"dataId\":0}]";
    
    public static string BossBattleDatabaseJsonString = "[{\"OwnerName\":\"Boss\",\"MonsterTeam\":[{\"owner\":\"Boss\",\"nickName\":\"Black Licorine\",\"monsterId\":\"Black Licorine\",\"uniqueId\":\"WILD1.19593E+15\",\"gender\":0,\"level\":15,\"fertility\":0,\"selectSkillsByHP\":true,\"setSkillByHPBoundaries\":[{\"HPHasToBeLowerThan\":100,\"HPHasToBeBiggerThan\":85,\"SkillName\":\"Darken Fire\"},{\"HPHasToBeLowerThan\":85,\"HPHasToBeBiggerThan\":75,\"SkillName\":\"Heavy Slash\"},{\"HPHasToBeLowerThan\":75,\"HPHasToBeBiggerThan\":70,\"SkillName\":\"Dark Spike\"},{\"HPHasToBeLowerThan\":70,\"HPHasToBeBiggerThan\":55,\"SkillName\":\"Enrage\"},{\"HPHasToBeLowerThan\":55,\"HPHasToBeBiggerThan\":30,\"SkillName\":\"Heavy Slash\"},{\"HPHasToBeLowerThan\":30,\"HPHasToBeBiggerThan\":20,\"SkillName\":\"Miasma\"},{\"HPHasToBeLowerThan\":20,\"HPHasToBeBiggerThan\":10,\"SkillName\":\"Enrage\"},{\"HPHasToBeLowerThan\":10,\"HPHasToBeBiggerThan\":5,\"SkillName\":\"Darken Fire\"},{\"HPHasToBeLowerThan\":5,\"HPHasToBeBiggerThan\":0,\"SkillName\":\"Heavy Slash\"}],\"Quality\":0,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":3430,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"uniqueSkillList\":[],\"passiveList\":[\"Water Bracer\"],\"temporaryPassives\":[],\"hp\":104,\"energy\":390,\"maxHp\":104,\"maxEnergy\":390,\"speed\":22,\"battleSpeed\":22,\"attack\":28,\"specialAttack\":22,\"defense\":40,\"specialDefense\":28,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":7,\"maxEnergyPotential\":0,\"speedPotential\":13,\"attackPotential\":6,\"specialAttackPotential\":1,\"defensePotential\":15,\"specialDefensePotential\":2,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}]},{\"OwnerName\":\"Boss\",\"MonsterTeam\":[{\"owner\":\"Boss\",\"nickName\":\"King Pfufu\",\"monsterId\":\"King Pfufu\",\"uniqueId\":\"WILD8.556078E+15\",\"gender\":0,\"level\":12,\"fertility\":0,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":1,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":3430,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Slap\",\"Beat Down\",\"Tsunami\",\"Defense Dance\"],\"uniqueSkillList\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"passiveList\":[\"Water Bracer\"],\"temporaryPassives\":[],\"hp\":107,\"energy\":409,\"maxHp\":107,\"maxEnergy\":409,\"speed\":23,\"battleSpeed\":23,\"attack\":28,\"specialAttack\":23,\"defense\":39,\"specialDefense\":28,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":23,\"maxEnergyPotential\":25,\"speedPotential\":25,\"attackPotential\":14,\"specialAttackPotential\":26,\"defensePotential\":2,\"specialDefensePotential\":4,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}]}]";
}