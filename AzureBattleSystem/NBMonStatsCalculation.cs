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

//
public static void StatsCalculation(NBMonBattleDataSave ThisMonsterInfo, NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase)
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