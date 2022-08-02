using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class NBMonDataSave
{
    public bool debugTextShow;

    public string owner = "Jason";
    public string nickName = "Jason";
    public string monsterId = "Floweria"; //genus in the game
    public string uniqueId = "dqwdqw"; //nbmonId in the block chain
    public NBMonProperties.Gender gender;
    public int level = 1;
    public int fertility;
    public bool selectSkillsByHP = false;

    [System.Serializable]
    public class SkillByHP
    {
        public int HPHasToBeLowerThan;
        public int HPHasToBeBiggerThan;
        public string SkillName;
    }

    public List<SkillByHP> setSkillByHPBoundaries;
    public MonsterQuality Quality;
    public enum MonsterQuality
    {
        Common,
        Uncommon,
        Rare,
        Elite,
        Legend
    }

    public bool MutationAcquired;

    //Related to Level Up Logic.
    public int expMemoryStorage;
    public int currentExp = 0;
    public int nextLevelExpRequired = 1000;
    public bool fainted;
    public List<NBMonProperties.StatusEffectCountGroup> statusEffectList;
    public List<string> skillList; //Basically Equipped Skills
    public List<string> uniqueSkillList;
    
    // Related with the stats
    public List<string> passiveList;
    public List<string> temporaryPassives;

    // Related with the stats
    public int hp = 10;
    public int energy = 10;
    public int maxHp = 10;
    public int maxEnergy = 10;
    public int speed = 10;
    public int battleSpeed = 10;
    public int attack = 10;
    public int specialAttack = 10;
    public int defense = 10;
    public int specialDefense = 10;
    public int criticalHit = 0;
    public float attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses, damageReduction, energyShieldValue;
    public int energyShield, surviveLethalBlow, totalIgnoreDefense, mustCritical, immuneCritical;
    public int elementDamageReduction;


    // Related with the caching the data
    private int maxHpCached = 10;
    private int maxEnergyCached = 10;
    private int speedCached = 10;
    private int attackCached = 10;
    private int specialAttackCached = 10;
    private int defenseCached = 10;
    private int specialDefenseCached = 10;

    public int maxHpPotential = 50;
    public int maxEnergyPotential = 50;
    public int speedPotential = 50;
    public int attackPotential = 50;
    public int specialAttackPotential = 50;
    public int defensePotential = 50;
    public int specialDefensePotential = 50;
    public int maxHpEffort = 0;
    public int maxEnergyEffort = 0;
    public int speedEffort = 0;
    public int attackEffort = 0;
    public int specialAttackEffort = 0;
    public int defenseEffort = 0;
    public int specialDefenseEffort = 0;

    public class ListsTeamInformation{
        public List<NBMonDataSave> thisPlayerInformation;
        public List<NBMonDataSave> enemyPlayerInformation;
    }


    //Change the stats based on percentage
    public void StatsPercentageChange(NBMonProperties.StatsType statsType, int percentageChange)
    {
        if (statsType == NBMonProperties.StatsType.Hp)
        {
            hp += (int)Math.Floor((float)(maxHp * percentageChange) / 100);

            if (hp > maxHp)
                hp = maxHp;

            if (hp < 0)
                hp = 0;
        } 
        else if (statsType == NBMonProperties.StatsType.Energy)
        {
            energy += (int)Math.Floor((float)(maxEnergy * percentageChange) / 100);

            if (energy > maxEnergy)
                energy = maxEnergy;

            if (energy < 0)
                energy = 0;
        }
        else if (statsType == NBMonProperties.StatsType.Attack)
        {
            attack += (int)Math.Floor((float)(attack * percentageChange) / 100);
        }
        else if (statsType == NBMonProperties.StatsType.Defense)
        {
            defense += (int)Math.Floor((float)(defense * percentageChange) / 100);
        }
        else if (statsType == NBMonProperties.StatsType.SpecialAttack)
        {
            specialAttack += (int)Math.Floor((float)(specialAttack * percentageChange) / 100);
        }
        else if (statsType == NBMonProperties.StatsType.SpecialDefense)
        {
            specialDefense += (int)Math.Floor((float)(specialDefense * percentageChange) / 100);
        }
        else if (statsType == NBMonProperties.StatsType.Speed)
        {
            speed += (int)Math.Floor((float)(speed * percentageChange) / 100);
        }
    }


    //Change the stats based on value
    public void StatsValueChange(NBMonProperties.StatsType statsType, int value)
    {
        if (statsType == NBMonProperties.StatsType.Hp)
        {
            hp += value;

            if (hp > maxHp)
                hp = maxHp;

            if (hp < 0)
                hp = 0;
        }
        else if (statsType == NBMonProperties.StatsType.Energy)
        {
            energy += value;

            if (energy > maxEnergy)
                energy = maxEnergy;

            if (energy < 0)
                energy = 0;
        }
        else if (statsType == NBMonProperties.StatsType.Attack)
        {
            attack += value;
        }
        else if (statsType == NBMonProperties.StatsType.Defense)
        {
            defense += value;
        }
        else if (statsType == NBMonProperties.StatsType.SpecialAttack)
        {
            specialAttack += value;
        }
        else if (statsType == NBMonProperties.StatsType.SpecialDefense)
        {
            specialDefense += value;
        }
        else if (statsType == NBMonProperties.StatsType.Speed)
        {
            battleSpeed += value;
        }
    }



    ///
    ///
    /// <summary>
    /// Related with giving EXP
    /// </summary>
    /// 
    ///
    public void StatsChangeExpStorage(int value)
    {
        expMemoryStorage += value;
    }

    public void StatsResetExpStorage()
    {
        expMemoryStorage = 0;
    }
    /*
    public void StatsLevelUp()
    {
        //If Level reached Max level, Reset EXP to 0.
        if (level == BattleEXPManager.instances.MaxLevel)
        {
            currentExp = 0;
            StatsResetExpStorage();
            return;
        }

        //Add the level and reset the current exp
        level += 1;
        NBMonLevelUp = true;
        StatsResetExpStorage();

        GameObject NBMonStatsCalculation = GameObject.FindObjectOfType<NBMonStatsCalculation>().gameObject;

        ////Increase the required exp for the next level
        if (NBMonStatsCalculation != null)
            nextLevelExpRequired = NBMonStatsCalculation.GetComponent<NBMonStatsCalculation>().IncreaseExpRequirementFormula(level);
        
        if (debugTextShow)
        {
            Debug.Log(nickName + "is now level" + level);
        }

        //Check if the current EXP still higher than next level exp required (used for multi level up at once
        if (currentExp >= nextLevelExpRequired)
        {
            Debug.Log(nickName + " has enough EXP to level up again");
            currentExp -= nextLevelExpRequired;
            StatsLevelUp();
        }
    }

    public void StatsAddExp()
    {
        if(expMemoryStorage > 0)
        {
            //Increase EXP bar
            currentExp += expMemoryStorage;

            if(currentExp >= nextLevelExpRequired)
            {
                currentExp -= nextLevelExpRequired;

                StatsLevelUp();
            }

            StatsResetExpStorage();
        }

        /*
        //Add the exp when the current storage contains exp
        if (expMemoryStorage > 0)
        {
            //Stores the different to next level
            int requiredToLevelUp = nextLevelExpRequired - currentExp;

            //When the current exp store is smaller than the required to level up, just add it the current exp and reset the exp storage
            if (requiredToLevelUp > expMemoryStorage)
            {
                if (debugTextShow)
                {
                    Debug.Log(nickName + "'s EXP is :" + currentExp + "requiredExp is" + requiredToLevelUp);
                }

                currentExp += expMemoryStorage;

                if (debugTextShow)
                {
                    Debug.Log("EXP have been added, the new current EXP is :" + currentExp);
                }
            }

            //When the current exp store is bigger than the required to level up, we level up, keep checking for level up until the required  until it's lower
            else if (requiredToLevelUp <= expMemoryStorage)
            {
                if (nextLevelExpRequired <= expMemoryStorage)
                {
                    expMemoryStorage -= nextLevelExpRequired;

                    StatsLevelUp();

                    currentExp = 0;
                    currentExp += expMemoryStorage;
                    
                    if (debugTextShow)
                    {
                        Debug.Log("Exp Have Cause" + nickName + "To Level Up");
                    }
                }
            }

            StatsResetExpStorage();
        }
        
    }
    */

    /// <summary>
    /// Related with Status Condition
    /// </summary>
    /// <returns></returns>
    //Return the selected status condition counter information
    public NBMonProperties.StatusEffectCountGroup returnStatusCountGroup(NBMonProperties.StatusEffect inputName)
    {
        foreach (var statusCounter in statusEffectList)
        {
            if (statusCounter.statusEffect == inputName && statusCounter.counter > 0)
            {
                return statusCounter;
            }
        }
        return null;
    }

    public void RefreshStatusEffectList()
    {
        var tempList = new List<NBMonProperties.StatusEffectCountGroup>(statusEffectList);
        foreach (var statusCounter in statusEffectList.ToList())
        {
            if (statusCounter.counter < 0)
            {
                statusEffectList.Remove(statusCounter);
            }
        }
        statusEffectList = tempList;
    }

    /////
    ///// <summary>
    ///// Related with Stat Change when level up
    ///// </summary>
    ///// 
    public void updateMonsterStat()
    {
        //Debug.Log("Updating" + MockReferenceManager.instances.monsterDatabase.FindMonster(monsterId).monsterName);
        NBMonDatabase.MonsterBaseStat baseStat = new NBMonDatabase.MonsterBaseStat();

        //Determine the value of the use stat
        maxHp = calculateLevelFormula(maxHp, baseStat.maxHpBase, maxHpPotential);
        maxEnergy = calculateLevelFormula(maxEnergy, baseStat.maxEnergyBase, maxEnergyPotential);
        speed = calculateLevelFormula(speed, baseStat.speedBase, speedPotential);
        attack = calculateLevelFormula(attack, baseStat.attackBase, attackPotential);
        defense = calculateLevelFormula(defense, baseStat.defenseBase, defensePotential);
        specialAttack = calculateLevelFormula(specialAttack, baseStat.specialAttackBase, specialDefensePotential);
        specialDefense = calculateLevelFormula(specialDefense, baseStat.specialDefenseBase, specialDefensePotential);
    }


    //Formulas related to Next Level's EXP
    private void increaseExpRequirementFormula()
    {
        var ThisNBMon_Level = level;
        var BaseConstant = 200;
        var ConstantDivider = 3;
        var Acceleration_Type_A = 1.2f;
        var Acceleration_Type_B = AccelerationTypeBCalculation();

        //Next EXP Calculation;
        nextLevelExpRequired = (int)Math.Round((float)BaseConstant/(float)ConstantDivider + (float)BaseConstant * Acceleration_Type_A * Math.Sqrt((float)ThisNBMon_Level*Acceleration_Type_B));
    }

    private float AccelerationTypeBCalculation()
    {
        var ThisNBMon_Level = level;

        if (ThisNBMon_Level > 20 && ThisNBMon_Level <= 10)
            return 0.4f;

        if (ThisNBMon_Level > 30 && ThisNBMon_Level <= 20)
            return 0.35f;

        if (ThisNBMon_Level > 40 && ThisNBMon_Level <= 30)
            return 0.28f;

        if (ThisNBMon_Level > 50 && ThisNBMon_Level <= 40)
            return 0.23f;

        //Only works if this NBMon's Level = 1 ~ 9
        return 0.5f;
    }

    private int calculateLevelFormula(int useValue, int baseValue, int potential)
    {
        int returnValue = baseValue;
        for (int i = 0; i < level; i++)
        {
            // Raise base state 1/50 of the base value of monster
            int addBaseFormula = baseValue + (int)Math.Floor(useValue * 1 / 1000f);

            // Raise base by addBaseFormula - addbaseFormula * 1/evValue
            //int addBaseEV = addBaseFormula - (int)Math.Floor(addBaseFormula * 1 / (float)potential);

            returnValue += addBaseFormula;
        }

        return returnValue;

    }

    //Related with caching, storing all the current stats so we can change the stats in battle via skill and can return it once the battle is finished
    public void StoreCachedData()
    {
        maxHpCached = maxHp;
        maxEnergyCached = maxEnergy;
        speedCached = speed;
        attackCached = attack;
        specialAttackCached = specialAttack;
        defenseCached = defense;
        specialDefenseCached = specialDefense;
    }
    

    //Once battle is finished apply the cached value
    public void ApplyCachedData()
    {
        //Make sure that the hp and energy don't overflow once the battle is finished
        if (hp > maxHpCached)
        {
            hp = maxHpCached;
        }
        if (energy > maxEnergyCached)
        {
            energy = maxEnergyCached;
        }

        maxHp = maxHpCached;
        maxEnergy = maxEnergyCached;
        speed = speedCached;
        attack = attackCached;
        specialAttack = specialAttackCached;
        defense = specialDefenseCached;
        specialDefense = specialDefenseCached;
    }


    public void ResetsTemporaryStats()
    {
        //Temporary Stats
        attackBuff = 0;
        specialAttackBuff = 0;
        defenseBuff = 0;
        specialDefenseBuff = 0;
        criticalBuff = 0;
        ignoreDefenses = 0;
        damageReduction = 0;
        energyShieldValue = 0;

        //Other Parameters
        energyShield = 0;
        mustCritical = 0;
        surviveLethalBlow = 0;
        totalIgnoreDefense = 0;
        immuneCritical = 0;
        elementDamageReduction = 0;
    }

    public void ResetsTemporaryPassives()
    {
        temporaryPassives.Clear();
    }
}
