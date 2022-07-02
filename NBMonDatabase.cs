using System.Collections;
using System.Collections.Generic;

public class NBMonDatabase
{
    public enum NBMonTierType
    {
        Origin,
        Wild,
        Hybrid
    }

    public int MaxHP, MaxEnergy, Speed, Attack, SPAttack, Defense, SPDefense;
    public float Norm_MaxHP, Norm_MaxEnergy, Norm_Speed, Norm_Attack, Norm_SPAttack, Norm_Defense, Norm_SPDefense;
    public float Potentialmodifier, EffortModifier;

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
    public MonsterInfoPlayFab FindMonster(string monsterName)
    {
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
