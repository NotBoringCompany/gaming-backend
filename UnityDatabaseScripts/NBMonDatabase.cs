using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NBMonDatabase
{
    public enum NBMonTierType
    {
        Origin,
        Wild,
        Hybrid
    }

    //Generic Base Stats Modifier
    public static int MaxHP = 8;
    public static int MaxEnergy = 20;
    public static int Speed = 3;
    public static int Attack = 3;
    public static int SPAttack = 3;
    public static int Defense = 3;
    public static int SPDefense = 3;

    //Normalized Stats
    public static float Norm_MaxHP = 0.7f;
    public static float Norm_MaxEnergy = 2.5f;
    public static float Norm_Speed = 0.15f;
    public static float Norm_Attack = 0.15f;
    public static float Norm_SPAttack = 0.15f;
    public static float Norm_Defense = 0.15f;
    public static float Norm_SPDefense = 0.15f;

    public static float Potentialmodifier = 0.002f;
    public static float EffortModifier = 0.0015f;

    //Stores relevant information related to the monster Database
    public ElementDatabase elementDatabase;
    public PassiveDatabase passiveDatabase;
    public SkillsDataBase allSkills;
    public StatusEffectIconDatabase statusEffectIconDatabase;
    public NBMonProperties nBMonProperties;


    [System.Serializable]
    public class MonsterInfoPlayFab
    {
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
        MonstersPlayFabList TempData = new MonstersPlayFabList();

        //Convertion from Json to Class
        var MonsterJsonData = NBMonDatabaseJson.MonsterDatabaseJson;
        TempData = JsonConvert.DeserializeObject<MonstersPlayFabList>(MonsterJsonData);

        //Make the original variable filled with the converted data.
        var monsters = TempData.monstersPlayFab;

        foreach (var monster in monsters)
        {
            if (monsterName == monster.monsterName)
            {
                return monster;
            }
        }
        
        return null;
    }
}
