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
    public static void CalculateNBMonStatsAfterLevelUp(NBMonBattleDataSave Monster)
    {
        //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
        NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(Monster.monsterId);

        //Make this Monster Level Up = true for UI.
        Monster.NBMonLevelUp = true;

        if(MonsterFromDatabase != null)
        {
            //Stats Calculation
            StatsCalculation(Monster, MonsterFromDatabase);
        }
    }   
    
    //Generate NBMon Level
    public static void GenerateRandomLevel(NBMonBattleDataSave ThisMonsterInfo, int LevelAsked)
    {
        //Random Variable
        Random R = new Random();
        int LowerBase = LevelAsked - 2;
        int UpperBase = LevelAsked + 2;

        //Normalized Lower Base
        if(LowerBase <= 0)
            LowerBase = 1;

        //Normalized Upper Base
        if(UpperBase >= AttackFunction.MaxLevel)
            UpperBase = AttackFunction.MaxLevel;

        ThisMonsterInfo.level = R.Next(LowerBase, UpperBase);
    }

    //Generate Wild NBmon Unique ID
    public static void GenerateWildMonsterCredential(NBMonBattleDataSave ThisMonsterInfo)
    {
        //Random Variable
        Random R = new Random();

        ThisMonsterInfo.uniqueId = R.Next(0, 999999999).ToString();
    }

    public static void GenerateThisMonsterQuality(NBMonBattleDataSave ThisMonsterInfo)
    {
        //Random Variable
        Random R = new Random();

        int RNG = R.Next(0, 100000);

        //45% Chance to be Common
        if(RNG >= 0 && RNG < 45000)
            ThisMonsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Common;
        
        //30% Chance to be Uncommon
        if(RNG >= 45000 && RNG < 75000)
            ThisMonsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Uncommon;

        //15% Chance to be Rare
        if(RNG >= 75000 && RNG < 90000)
            ThisMonsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Rare;

        //9% Chance to be Elite
        if(RNG >= 90000 && RNG < 99000)
            ThisMonsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Elite;

        //1% Chance to be Elite
        if(RNG >= 99000 && RNG <= 100000)
            ThisMonsterInfo.Quality = (int)NBMonDataSave.MonsterQuality.Legend;        
    }

    //Generate Random Potential Value
    public static void GenerateRandomPotentialValue(NBMonBattleDataSave ThisMonsterInfo, NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase)
    {
        //Random Variable
        Random R = new Random();

        //Let's Generate Max Possible Potential Value
        var MaxEffortValue = 0;

        if(ThisMonsterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Common)
            MaxEffortValue = 20;

        if(ThisMonsterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Uncommon)
            MaxEffortValue = 30;

        if(ThisMonsterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Rare)
            MaxEffortValue = 40;

        if(ThisMonsterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Elite)
            MaxEffortValue = 50;
        
        if(ThisMonsterInfo.Quality == (int) NBMonDataSave.MonsterQuality.Legend)
            MaxEffortValue = 65;

        if(MonsterFromDatabase.Tier == NBMonDatabase.NBMonTierType.Wild || MonsterFromDatabase.Tier == NBMonDatabase.NBMonTierType.Hybrid)
        {
            if(MaxEffortValue > 50)
                MaxEffortValue = 50;
        }

        //After Defining This Monster's Max Possible Effort Value
        ThisMonsterInfo.maxHpPotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.maxEnergyPotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.speedPotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.attackPotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.specialAttackPotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.defensePotential = R.Next(0, MaxEffortValue);
        ThisMonsterInfo.specialDefensePotential = R.Next(0, MaxEffortValue);

    }

    //Calculate Base Stats (Used for Read Data from Database, during Level Up)).
    public static void StatsCalculation(NBMonBattleDataSave ThisMonsterInfo, NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase, bool ResetMonsterStats = false)
    {
        ThisMonsterInfo.maxHp =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.maxHpBase,
            NBMonDatabase.MaxHP,
            NBMonDatabase.Norm_MaxHP,
            ThisMonsterInfo.level,
            ThisMonsterInfo.maxHpPotential,
            ThisMonsterInfo.maxHpEffort);

        ThisMonsterInfo.maxEnergy =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.maxEnergyBase,
            NBMonDatabase.MaxEnergy,
            NBMonDatabase.Norm_MaxEnergy,
            ThisMonsterInfo.level,
            ThisMonsterInfo.maxEnergyPotential,
            ThisMonsterInfo.maxEnergyEffort);

        ThisMonsterInfo.speed =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.speedBase,
            NBMonDatabase.Speed,
            NBMonDatabase.Norm_Speed,
            ThisMonsterInfo.level,
           ThisMonsterInfo.speedPotential,
            ThisMonsterInfo.speedEffort);

        ThisMonsterInfo.attack =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.attackBase,
            NBMonDatabase.Attack,
            NBMonDatabase.Norm_Attack,
            ThisMonsterInfo.level,
            ThisMonsterInfo.attackPotential,
            ThisMonsterInfo.attackEffort);

        ThisMonsterInfo.specialAttack =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.specialAttackBase,
            NBMonDatabase.SPAttack,
            NBMonDatabase.Norm_SPAttack,
            ThisMonsterInfo.level,
            ThisMonsterInfo.specialAttackPotential,
            ThisMonsterInfo.specialAttackEffort);

        ThisMonsterInfo.defense =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.defenseBase,
            NBMonDatabase.Defense,
            NBMonDatabase.Norm_Defense,
            ThisMonsterInfo.level,
            ThisMonsterInfo.defensePotential,
            ThisMonsterInfo.defenseEffort);

        ThisMonsterInfo.specialDefense =
            EachStatsCalculationMethod(MonsterFromDatabase.monsterBaseStat.specialDefenseBase,
            NBMonDatabase.SPDefense,
            NBMonDatabase.Norm_SPDefense,
            ThisMonsterInfo.level,
            ThisMonsterInfo.specialDefensePotential,
            ThisMonsterInfo.specialDefenseEffort);

        //Recovery HP and Energy if ResetMonsterStats Bool is True
        if(ResetMonsterStats)
        {
            ThisMonsterInfo.hp = ThisMonsterInfo.maxHp;
            ThisMonsterInfo.energy = ThisMonsterInfo.maxEnergy;
        }

        //Next EXP Required Calculation
        ThisMonsterInfo.nextLevelExpRequired = IncreaseExpRequirementFormula(ThisMonsterInfo.level);
    }

    //Calculation Method
    private static int EachStatsCalculationMethod(int BaseStats, int BaseMultiplier, float LevelMultiplier, int ThisMonsterLevel, int Potential, int Effort)
    {
        var BaseValue = (int) Math.Floor((float)BaseStats * (float)BaseMultiplier);
        var LevelValue = (int) Math.Floor(((float)ThisMonsterLevel - 1f) * (float)BaseStats * (float)LevelMultiplier);
        var PotentialValue = (int) Math.Floor(((float)BaseValue + (float)LevelValue) * (float)Potential * NBMonDatabase.Potentialmodifier);
        var EffortValue = (int) Math.Floor(((float)BaseValue + (float)LevelValue) * (float)Effort * NBMonDatabase.EffortModifier);

        var TotalValue = BaseValue + LevelValue + PotentialValue + EffortValue;

        return TotalValue;
    } 

    //Formulas related to Next Level's EXP
    public static int IncreaseExpRequirementFormula(int level)
    {
        var ThisNBMon_Level = level;
        var BaseConstant = 4;
        var ConstantDivider = 8;

        //Next EXP Calculation;
        var NextLevelEXPRequired = (int) Math.Ceiling(BaseConstant*Math.Pow(ThisNBMon_Level, 3)/ConstantDivider);

        return NextLevelEXPRequired;
    }
}