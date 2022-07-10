using System;
using System.Collections.Generic;

public class NBMonBattleDataSave
{
    public string owner;
    public string nickName;
    public string monsterId;
    public string uniqueId;
    public int gender;
    public int level;
    public int fertility;
    public bool selectSkillsByHP;

    [System.Serializable]
    public class SkillByHP
    {
        public int HPHasToBeLowerThan;
        public int HPHasToBeBiggerThan;
        public string SkillName;
    }

    public List<SkillByHP> setSkillByHPBoundaries;
    public int Quality;
    public bool MutationAcquired;
    public int MutationType;
    public bool NBMonLevelUp;
    public int expMemoryStorage;
    public int currentExp;
    public int nextLevelExpRequired;
    public bool fainted;
    public List<StatusEffectList> statusEffectList;
    public List<string> skillList;
    public List<string> uniqueSkillList;
    public List<string> passiveList;
    public List<string> temporaryPassives;
    public int hp;
    public int energy;
    public int maxHp;
    public int maxEnergy;
    public int speed;
    public int battleSpeed;
    public int attack;
    public int specialAttack;
    public int defense;
    public int specialDefense;
    public int criticalHit;
    public float attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses, damageReduction, energyShieldValue;
    public int energyShield, surviveLethalBlow, totalIgnoreDefense, mustCritical, immuneCritical;
    public int elementDamageReduction;


    public int maxHpPotential;
    public int maxEnergyPotential;
    public int speedPotential;
    public int attackPotential;
    public int specialAttackPotential;
    public int defensePotential;
    public int specialDefensePotential;
    public int maxHpEffort;
    public int maxEnergyEffort;
    public int speedEffort;
    public int attackEffort;
    public int specialAttackEffort;
    public int defenseEffort;
    public int specialDefenseEffort;
}

public class NBMonDatabase_Azure
{
    public static NBMonBattleDataSave FindNBMonDataUsingUniqueID(string UniqueID, List<NBMonBattleDataSave> Database)
    {
        foreach(var Monster in Database)
        {
            if(Monster.uniqueId == UniqueID)
            {
                return Monster;
            }
        }

        return null;
    }
}

public class StatusEffectList
{
    public int statusEffect;
    public int counter;
    public int stacks;
}

public class NBMonTeamData
{
    public static List<NBMonBattleDataSave> PlayerTeam;
    public static List<NBMonBattleDataSave> EnemyTeam;

    //Change the stats based on percentage (Only for HP and Energy)
    public static void StatsPercentageChange(NBMonBattleDataSave Monster, NBMonProperties.StatsType statsType, int percentageChange)
    {
        if (statsType == NBMonProperties.StatsType.Hp)
        {
            Monster.hp += Convert.ToInt32(Math.Floor((float)(Monster.maxHp * percentageChange) / 100));

            if (Monster.hp > Monster.maxHp)
                Monster.hp = Monster.maxHp;

            if (Monster.hp < 0)
                Monster.hp = 0;
        } 
        else if (statsType == NBMonProperties.StatsType.Energy)
        {
            Monster.energy += Convert.ToInt32(Math.Floor((float)(Monster.maxEnergy * percentageChange) / 100));

            if (Monster.energy > Monster.maxEnergy)
                Monster.energy = Monster.maxEnergy;

            if (Monster.energy < 0)
                Monster.energy = 0;
        }
    }

    //Change the stats based on value
    public static void StatsValueChange(NBMonBattleDataSave Monster, NBMonProperties.StatsType statsType, int value)
    {
        if (statsType == NBMonProperties.StatsType.Hp)
        {
            Monster.hp += value;

            if (Monster.hp > Monster.maxHp)
                Monster.hp = Monster.maxHp;

            if (Monster.hp < 0)
                Monster.hp = 0;
        }
        else if (statsType == NBMonProperties.StatsType.Energy)
        {
            Monster.energy += value;

            if (Monster.energy > Monster.maxEnergy)
                Monster.energy = Monster.maxEnergy;

            if (Monster.energy < 0)
                Monster.energy = 0;
        }
    }
}

