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
        //============================================================
        // MONGODB Logic
        // battleData can be npcData or bossData
        //============================================================
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("dataId", dataId);
        //Setting for Collection
        var collection = MongoHelper.db.GetCollection<BsonDocument>(battleData).Find(filter).FirstOrDefault().AsEnumerable();
        var wildBattleData = new FixedNBMonBattleDatabase();

        //Convert the Result into desire Class
        wildBattleData = BsonSerializer.Deserialize<FixedNBMonBattleDatabase>(collection.ToBsonDocument());
        return wildBattleData;
    }

    //A static function to Convert JsonString into a Class
    public static void GetData()
    {
        NPCBattleData = JsonConvert.DeserializeObject<List<FixedNBMonBattleDatabase>>(NPCBattleDatabaseJsonString);
        BossBattleData = JsonConvert.DeserializeObject<List<FixedNBMonBattleDatabase>>(BossBattleDatabaseJsonString);
    }

    public static string NPCBattleDatabaseJsonString = "[{\"OwnerName\":\"Andy\",\"MonsterTeam\":[{\"owner\":\"Andy\",\"nickName\":\"Roggo\",\"monsterId\":\"Roggo\",\"uniqueId\":\"158179818081940\",\"gender\":0,\"level\":7,\"fertility\":0,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":0,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":7813,\"fainted\":false,\"statusEffectList\":[{\"statusEffect\":10,\"counter\":1,\"stacks\":2},{\"statusEffect\":11,\"counter\":1,\"stacks\":2},{\"statusEffect\":14,\"counter\":1,\"stacks\":3}],\"skillList\":[\"Cut\",\"Leaf Slash\",\"Water Pressure\",\"Beat Down\"],\"uniqueSkillList\":[],\"passiveList\":[\"Defender\",\"Sword And Shield\"],\"temporaryPassives\":[],\"hp\":4,\"energy\":115,\"maxHp\":100,\"maxEnergy\":160,\"speed\":47,\"battleSpeed\":47,\"attack\":40,\"specialAttack\":47,\"defense\":26,\"specialDefense\":33,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":7,\"maxEnergyPotential\":2,\"speedPotential\":18,\"attackPotential\":17,\"specialAttackPotential\":14,\"defensePotential\":8,\"specialDefensePotential\":4,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0},{\"owner\":\"Andy\",\"nickName\":\"Heree\",\"monsterId\":\"Heree\",\"uniqueId\":\"dqwdqw\",\"gender\":0,\"level\":7,\"fertility\":0,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":0,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":7813,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Cut\",\"Leaf Slash\",\"Absorb\",\"Heal\"],\"uniqueSkillList\":[],\"passiveList\":[\"Botanic Healing\",\"Camouflage\"],\"temporaryPassives\":[],\"hp\":10,\"energy\":212,\"maxHp\":136,\"maxEnergy\":352,\"speed\":42,\"battleSpeed\":42,\"attack\":28,\"specialAttack\":42,\"defense\":28,\"specialDefense\":42,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":50,\"maxEnergyPotential\":50,\"speedPotential\":50,\"attackPotential\":50,\"specialAttackPotential\":50,\"defensePotential\":50,\"specialDefensePotential\":50,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0},{\"owner\":\"Andy\",\"nickName\":\"Pfufu\",\"monsterId\":\"Pfufu\",\"uniqueId\":\"118947506\",\"gender\":1,\"level\":8,\"fertility\":3000,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":2,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":1000,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Slap\",\"Tsunami\",\"Water Pressure\"],\"uniqueSkillList\":[],\"passiveList\":[\"Water Bracer\"],\"temporaryPassives\":[],\"hp\":10,\"energy\":10,\"maxHp\":10,\"maxEnergy\":10,\"speed\":10,\"battleSpeed\":10,\"attack\":10,\"specialAttack\":10,\"defense\":10,\"specialDefense\":10,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":50,\"maxEnergyPotential\":50,\"speedPotential\":50,\"attackPotential\":50,\"specialAttackPotential\":50,\"defensePotential\":50,\"specialDefensePotential\":50,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}]}]";
    public static string BossBattleDatabaseJsonString = "[{\"OwnerName\":\"Boss\",\"MonsterTeam\":[{\"owner\":\"Boss\",\"nickName\":\"Black Licorine\",\"monsterId\":\"Black Licorine\",\"uniqueId\":\"WILD1.19593E+15\",\"gender\":0,\"level\":15,\"fertility\":0,\"selectSkillsByHP\":true,\"setSkillByHPBoundaries\":[{\"HPHasToBeLowerThan\":100,\"HPHasToBeBiggerThan\":85,\"SkillName\":\"Darken Fire\"},{\"HPHasToBeLowerThan\":85,\"HPHasToBeBiggerThan\":75,\"SkillName\":\"Heavy Slash\"},{\"HPHasToBeLowerThan\":75,\"HPHasToBeBiggerThan\":70,\"SkillName\":\"Dark Spike\"},{\"HPHasToBeLowerThan\":70,\"HPHasToBeBiggerThan\":55,\"SkillName\":\"Enrage\"},{\"HPHasToBeLowerThan\":55,\"HPHasToBeBiggerThan\":30,\"SkillName\":\"Heavy Slash\"},{\"HPHasToBeLowerThan\":30,\"HPHasToBeBiggerThan\":20,\"SkillName\":\"Miasma\"},{\"HPHasToBeLowerThan\":20,\"HPHasToBeBiggerThan\":10,\"SkillName\":\"Enrage\"},{\"HPHasToBeLowerThan\":10,\"HPHasToBeBiggerThan\":5,\"SkillName\":\"Darken Fire\"},{\"HPHasToBeLowerThan\":5,\"HPHasToBeBiggerThan\":0,\"SkillName\":\"Heavy Slash\"}],\"Quality\":0,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":3430,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"uniqueSkillList\":[],\"passiveList\":[\"Water Bracer\"],\"temporaryPassives\":[],\"hp\":104,\"energy\":390,\"maxHp\":104,\"maxEnergy\":390,\"speed\":22,\"battleSpeed\":22,\"attack\":28,\"specialAttack\":22,\"defense\":40,\"specialDefense\":28,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":7,\"maxEnergyPotential\":0,\"speedPotential\":13,\"attackPotential\":6,\"specialAttackPotential\":1,\"defensePotential\":15,\"specialDefensePotential\":2,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}]},{\"OwnerName\":\"Boss\",\"MonsterTeam\":[{\"owner\":\"Boss\",\"nickName\":\"King Pfufu\",\"monsterId\":\"King Pfufu\",\"uniqueId\":\"WILD8.556078E+15\",\"gender\":0,\"level\":12,\"fertility\":0,\"selectSkillsByHP\":false,\"setSkillByHPBoundaries\":[],\"Quality\":1,\"MutationAcquired\":false,\"MutationType\":0,\"NBMonLevelUp\":false,\"expMemoryStorage\":0,\"currentExp\":0,\"nextLevelExpRequired\":3430,\"fainted\":false,\"statusEffectList\":[],\"skillList\":[\"Slap\",\"Beat Down\",\"Tsunami\",\"Defense Dance\"],\"uniqueSkillList\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"passiveList\":[\"Water Bracer\"],\"temporaryPassives\":[],\"hp\":107,\"energy\":409,\"maxHp\":107,\"maxEnergy\":409,\"speed\":23,\"battleSpeed\":23,\"attack\":28,\"specialAttack\":23,\"defense\":39,\"specialDefense\":28,\"criticalHit\":0,\"attackBuff\":0,\"specialAttackBuff\":0,\"defenseBuff\":0,\"specialDefenseBuff\":0,\"criticalBuff\":0,\"ignoreDefenses\":0,\"damageReduction\":0,\"energyShieldValue\":0,\"energyShield\":0,\"surviveLethalBlow\":0,\"totalIgnoreDefense\":0,\"mustCritical\":0,\"immuneCritical\":0,\"elementDamageReduction\":0,\"maxHpPotential\":23,\"maxEnergyPotential\":25,\"speedPotential\":25,\"attackPotential\":14,\"specialAttackPotential\":26,\"defensePotential\":2,\"specialDefensePotential\":4,\"maxHpEffort\":0,\"maxEnergyEffort\":0,\"speedEffort\":0,\"attackEffort\":0,\"specialAttackEffort\":0,\"defenseEffort\":0,\"specialDefenseEffort\":0}]}]";
}