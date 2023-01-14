using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Samples;
using System.Collections.Generic;
using PlayFab.EconomyModels;
using PlayFab.ServerModels;
using System.Net.Http;
using System.Net;
using System.Linq;

public static class NBMonStatsCalculation
{
    public static void CalculateNBMonStatsAfterLevelUp(NBMonBattleDataSave monster)
    {
        //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
        NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase = NBMonDatabase.FindMonster(monster.monsterId);

        //Make this Monster Level Up = true for UI.
        monster.NBMonLevelUp = true;

        if(monsterFromDatabase != null)
        {
            //Stats Calculation
            StatsCalculation(monster, monsterFromDatabase);
        }
    }   
    
    //Generate NBMon Level
    public static void GenerateRandomLevel(NBMonBattleDataSave monsterInfo, int levelAsked)
    {
        //Random Variable
        Random R = new Random();
        int LowerBase = levelAsked - 2;
        int UpperBase = levelAsked + 2;

        //Normalized Lower Base
        if(LowerBase <= 0)
            LowerBase = 1;

        //Normalized Upper Base
        if(UpperBase >= AttackFunction.MaxLevel)
            UpperBase = AttackFunction.MaxLevel;

        monsterInfo.level = R.Next(LowerBase, UpperBase);
    }

    //Generate Wild NBmon Unique ID
    public static void GenerateWildMonsterCredential(NBMonBattleDataSave monsterInfo)
    {
        //Random Variable
        Random R = new Random();

        monsterInfo.uniqueId = R.Next(0, 999999999).ToString();
    }

    public static void GenerateThisMonsterQuality(NBMonBattleDataSave monsterInfo)
    {
        //Random Variable
        Random R = new Random();

        int RNG = R.Next(0, 100000);

        //45% Chance to be Common
        if(RNG >= 0 && RNG < 45000)
            monsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Common;
        
        //30% Chance to be Uncommon
        if(RNG >= 45000 && RNG < 75000)
            monsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Uncommon;

        //15% Chance to be Rare
        if(RNG >= 75000 && RNG < 90000)
            monsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Rare;

        //9% Chance to be Elite
        if(RNG >= 90000 && RNG < 99000)
            monsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Elite;

        //1% Chance to be Elite
        if(RNG >= 99000 && RNG <= 100000)
            monsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Legend;        
    }

    //Generate Random Potential Value
    public static void GenerateRandomPotentialValue(NBMonBattleDataSave mosterInfo, NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase)
    {
        //Random Variable
        Random R = new Random();

        //Let's Generate Max Possible Potential Value
        var maxPotentialValue = 0;

        if(mosterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Common)
            maxPotentialValue = 20;

        if(mosterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Uncommon)
            maxPotentialValue = 30;

        if(mosterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Rare)
            maxPotentialValue = 40;

        if(mosterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Elite)
            maxPotentialValue = 50;
        
        if(mosterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Legend)
            maxPotentialValue = 65;

        if(monsterFromDatabase.Tier == NBMonDatabase.NBMonTierType.Wild || monsterFromDatabase.Tier == NBMonDatabase.NBMonTierType.Hybrid)
        {
            if(maxPotentialValue > 50)
                maxPotentialValue = 50;
        }

        //After Defining This Monster's Max Possible Effort Value
        mosterInfo.maxHpPotential = R.Next(0, maxPotentialValue);
        mosterInfo.maxEnergyPotential = R.Next(0, maxPotentialValue);
        mosterInfo.speedPotential = R.Next(0, maxPotentialValue);
        mosterInfo.attackPotential = R.Next(0, maxPotentialValue);
        mosterInfo.specialAttackPotential = R.Next(0, maxPotentialValue);
        mosterInfo.defensePotential = R.Next(0, maxPotentialValue);
        mosterInfo.specialDefensePotential = R.Next(0, maxPotentialValue);

    }

    //Calculate Base Stats (Used for Read Data from Database, during Level Up)).
    public static void StatsCalculation(NBMonBattleDataSave monsterInfo, NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase, bool resetMonsterStats = false)
    {
        int level = monsterInfo.level;

        monsterInfo.maxHp =
            HPStatCalculation(monsterFromDatabase.monsterBaseStat.maxHpBase, 
            level, 
            monsterInfo.maxHpPotential, 
            monsterInfo.maxHpEffort);

        monsterInfo.maxEnergy =
            EnergyStatCalculation(monsterFromDatabase.monsterBaseStat.maxEnergyBase, 
            level, 
            monsterInfo.maxEnergyPotential, 
            monsterInfo.maxEnergyEffort);

        monsterInfo.speed =
            OtherStatsCalculation(monsterFromDatabase.monsterBaseStat.speedBase, 
            level, 
            monsterInfo.speedPotential, 
            monsterInfo.speedEffort);

        monsterInfo.attack =
            OtherStatsCalculation(monsterFromDatabase.monsterBaseStat.attackBase, 
            level, 
            monsterInfo.attackPotential, 
            monsterInfo.attackEffort);

        monsterInfo.specialAttack =
            OtherStatsCalculation(monsterFromDatabase.monsterBaseStat.specialAttackBase, 
            level, 
            monsterInfo.specialAttackPotential, 
            monsterInfo.specialAttackEffort);


        monsterInfo.defense =
            OtherStatsCalculation(monsterFromDatabase.monsterBaseStat.defenseBase, 
            level, 
            monsterInfo.defensePotential, 
            monsterInfo.defenseEffort);

        monsterInfo.specialDefense =
            OtherStatsCalculation(monsterFromDatabase.monsterBaseStat.specialDefenseBase, 
            level, 
            monsterInfo.specialDefensePotential, 
            monsterInfo.specialDefenseEffort);

        //Recovery HP and Energy if ResetMonsterStats Bool is True
        if(resetMonsterStats)
        {
            //Recover HP and Energy
            monsterInfo.hp = monsterInfo.maxHp;
            monsterInfo.energy = monsterInfo.maxEnergy;

            //Reset Battle Speed
            monsterInfo.battleSpeed = monsterInfo.speed;
        }

        //Next EXP Required Calculation
        monsterInfo.nextLevelExpRequired = IncreaseExpRequirementFormula(monsterInfo.level);
    }

    private static int HPStatCalculation(int baseStat, int level, int potentialValue, int growthValue)
    {
        float func_A = (2 * baseStat) / 3;
        float func_B = (potentialValue * potentialValue / 34);
        float func_C = (growthValue / 17);
        float func_ABC = ((func_A + func_B + func_C) * level) / 95;
        float func_2 = (7 * level / 4) + 90;

        int sumUpValue = (int)(func_ABC + func_2);
        return sumUpValue;
    }

    private static int EnergyStatCalculation(int baseStat, int level, int potentialValue, int growthValue)
    {
        float func_A = (2 * baseStat) / 7;
        float func_B = 10 * (float)System.Math.Sqrt(level);
        float func_C = (float)System.Math.Sqrt(potentialValue * baseStat * level / 125);
        float func_D = (float)System.Math.Sqrt(growthValue * baseStat * level / 175);

        int sumUpValue = (int)(func_A + func_B + func_C + func_D);
        return sumUpValue;
    }

    private static int OtherStatsCalculation(int baseStat, int level, int potentialValue, int growthValue)
    {
        float func_A = (2 * baseStat) / 3;
        float func_B = (potentialValue * potentialValue / 35);
        float func_C = (growthValue / 19);
        float func_ABC = ((func_A + func_B + func_C) * level) / 100;

        int sumUpValue = (int)(func_ABC + 75);
        return sumUpValue;
    }

    //Formulas related to Next Level's EXP
    public static int IncreaseExpRequirementFormula(int level)
    {
        var demoEXPTable = NBMonDatabase.DemoEXPTableData();

        foreach(var expTable in demoEXPTable)
        {
                if (expTable.level == level)
                return expTable.expRequired;
        }

                //If there's no level requred left, then do this instead.
        return demoEXPTable[demoEXPTable.Count - 1].expRequired;

        var ThisNBMon_Level = level;
        var BaseConstant = 4;
        var ConstantDivider = 8;

        //Next EXP Calculation;
        var NextLevelEXPRequired = (int) Math.Ceiling(BaseConstant*Math.Pow(ThisNBMon_Level, 3)/ConstantDivider);

        return NextLevelEXPRequired;
    }
}