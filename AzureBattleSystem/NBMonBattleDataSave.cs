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
    public float attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses, damageReduction, energyShieldValue, absorbDamageValue;
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
    public static NBMonBattleDataSave FindNBMonDataUsingUniqueID(string UniqueID, List<NBMonBattleDataSave> Team)
    {
        foreach (var Monster in Team)
        {
            if (Monster.uniqueId == UniqueID)
            {
                return Monster;
            }
        }

        return null;
    }

        // public static int FindNBMonTeamPositionUsingUniqueID(string UniqueID, List<NBMonBattleDataSave> Team)
        // {   
        //     int Count = new int();

        //     foreach(var Monster in Team)
        //     {
        //         if(Monster.uniqueId == UniqueID)
        //         {
        //             return Count;
        //         }

        //         Count++;
        //     }

        //     //Indicate this is Not Found
        //     return -1;
        // }
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
    public static void StatsPercentageChange(NBMonBattleDataSave monster, NBMonProperties.StatsType statsType, int percentageChange)
    {
        int value;

        switch (statsType)
        {
            case NBMonProperties.StatsType.Hp:
                value = (int)Math.Floor((float)(monster.maxHp * percentageChange) / 100);
                monster.hp = Math.Clamp(monster.hp + value, 0, monster.maxHp);
                break;
            case NBMonProperties.StatsType.Energy:
                value = (int)Math.Floor((float)(monster.maxEnergy * percentageChange) / 100);
                monster.energy = Math.Clamp(monster.energy + value, 0, monster.maxEnergy);
                break;
            case NBMonProperties.StatsType.Attack:
                monster.attackBuff += (int)Math.Floor((float)(monster.attack * percentageChange) / 100);
                break;
            case NBMonProperties.StatsType.Defense:
                monster.defenseBuff += (int)Math.Floor((float)(monster.defense * percentageChange) / 100);
                break;
            case NBMonProperties.StatsType.SpecialAttack:
                monster.specialAttackBuff += (int)Math.Floor((float)(monster.specialAttack * percentageChange) / 100);
                break;
            case NBMonProperties.StatsType.SpecialDefense:
                monster.specialDefenseBuff += (int)Math.Floor((float)(monster.specialDefense * percentageChange) / 100);
                break;
            case NBMonProperties.StatsType.Speed:
                monster.battleSpeed += (int)Math.Floor((float)(monster.speed * percentageChange) / 100);
                break;
            default:
                break;
        }
    }

    //Change the stats based on value
    public static void StatsValueChange(NBMonBattleDataSave monster, NBMonProperties.StatsType statsType, int value)
    {
        switch (statsType)
        {
            case NBMonProperties.StatsType.Hp:
                monster.hp = Math.Clamp(monster.hp + value, 0, monster.maxHp);
                break;
            case NBMonProperties.StatsType.Energy:
                monster.energy = Math.Clamp(monster.energy + value, 0, monster.maxEnergy);
                break;
            case NBMonProperties.StatsType.Attack:
                monster.attackBuff += value;
                break;
            case NBMonProperties.StatsType.Defense:
                monster.defenseBuff += value;
                break;
            case NBMonProperties.StatsType.SpecialAttack:
                monster.specialAttackBuff += value;
                break;
            case NBMonProperties.StatsType.SpecialDefense:
                monster.specialDefenseBuff += value;
                break;
            case NBMonProperties.StatsType.Speed:
                monster.battleSpeed += value;
                break;
        }
    }
}

