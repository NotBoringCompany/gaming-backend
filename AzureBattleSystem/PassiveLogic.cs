using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Client;

public class PassiveLogic
{
    private static NBMonBattleDataSave originMonsterMemory;
    private static NBMonBattleDataSave targetMonsterMemory;
    private static NBMonBattleDataSave useMonsterMemory;

    //Logics
    //Apply the passive according to the targetting type in script
    public static void ApplyPassive(PassiveDatabase.ExecutionPosition executionPosition, PassiveDatabase.TargetType targetType, NBMonBattleDataSave originMonsterPass, NBMonBattleDataSave targetMonsterPass, SkillsDataBase.SkillInfoPlayFab skill, RNGSeedClass seedClass)
{
    // Get Battle Environment Value.
    var battleEnvironment = AttackFunction.BattleEnvironment;
    var useMonsterMemory = new NBMonBattleDataSave();

    // Check all the passive from the original monster
    if (targetType == PassiveDatabase.TargetType.originalMonster || targetType == PassiveDatabase.TargetType.both)
    {
        useMonsterMemory = originMonsterPass;

        foreach (var passive in originMonsterPass.passiveList.Concat(originMonsterPass.temporaryPassives))
        {
            PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, battleEnvironment, seedClass);
        }
    }

    // Check all the passive from the target monster
    if (targetType == PassiveDatabase.TargetType.targetedMonster || targetType == PassiveDatabase.TargetType.both)
    {
        useMonsterMemory = targetMonsterPass;

        foreach (var passive in targetMonsterPass.passiveList.Concat(targetMonsterPass.temporaryPassives))
        {
            PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, battleEnvironment, seedClass);
        }
    }
}

        //Apply the passive, this is the logic we'd like to call!
    public static void PassiveExecutionLogic(PassiveDatabase.ExecutionPosition executionPosition, PassiveDatabase.PassiveInfoPlayFab passiveInfo, SkillsDataBase.SkillInfoPlayFab skill, string battleEnvironment, RNGSeedClass seedClass)
    {
        if (passiveInfo == null)
            return;

        foreach (var passiveDetail in passiveInfo.passiveDetail)
        {
            if (executionPosition != passiveDetail.executionPosition)
                continue;

            if (!CheckPassiveRequirement(passiveDetail, passiveDetail.requirements, skill, battleEnvironment))
                continue;

            useMonsterMemory = (passiveDetail.requirements.Any(x => x.monsterTargetingCheck && targetMonsterMemory != null && !x.monsterTargeting_EffectToSelf)) ? targetMonsterMemory : originMonsterMemory;

            foreach (var passive in passiveDetail.effect)
            {
                DoPassive(passive, skill, seedClass);
            }
        }
    }

    //Checks the list of modifier requirements
    private static bool CheckPassiveRequirement(PassiveDatabase.PassiveDetail passiveDetail, List<PassiveDatabase.Requirements> passiveRequirements, SkillsDataBase.SkillInfoPlayFab skill, string BattleEnvironment) 
    { 
        foreach (var requirements in passiveRequirements)
        {
            switch (requirements.requirementTypes)
            {
                case PassiveDatabase.RequirementTypes.ActionUsingElement:
                case PassiveDatabase.RequirementTypes.ActionReceivingElement:
                    if (skill.skillElement != requirements.skillElement) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.ActionUsingTechnique:
                case PassiveDatabase.RequirementTypes.ActionReceivingTechnique:
                    if (!requirements.skillLists.Contains(skill.skillName)) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.StatsValueReq:
                    if (!StatsValueRequirementLogic(true, requirements)) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.HaveStatusEffect:
                    if (!HaveStatusEffectRequirementLogic(true, requirements)) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.ActionGivingStatusEffect:
                case PassiveDatabase.RequirementTypes.ActionReceivingStatusEffect:
                    if (!StatusEffectRequirementLogic(skill, new List<NBMonProperties.StatusEffect>(), requirements)) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.Fainted:
                    if (!useMonsterMemory.fainted) 
                        return false;
                    break;

                case PassiveDatabase.RequirementTypes.Environment:
                    if (!requirements.EnvinromentLists.Contains(BattleEnvironment)) 
                        return false;
                    break;

                default:
                    break;
            }
        }

        return true;
    }

    //Find This Monster Status Effect
    public static StatusEffectList FindNBMonStatusEffect(NBMonBattleDataSave monster, NBMonProperties.StatusEffect statusEffect)
    {
        foreach (var statusInMonster in monster.statusEffectList)
        {
            if (statusInMonster.statusEffect == (int)statusEffect && statusInMonster.counter > 0)
            {
                return statusInMonster;
            }
        }

        return null;
    }


    //Apply the passive effect and change the monster stats based on it
    private static void DoPassive(PassiveDatabase.EffectInfo passiveEffect, SkillsDataBase.SkillInfoPlayFab skill, RNGSeedClass seedClass)
{
    switch (passiveEffect.effectType)
    {
        case PassiveDatabase.EffectType.StatusEffect:
            PassiveStatusEffectLogic(passiveEffect, seedClass);
            break;
        case PassiveDatabase.EffectType.StatsPercentage:
            NBMonTeamData.StatsPercentageChange(useMonsterMemory, passiveEffect.statsType, passiveEffect.valueChange);
            break;
        case PassiveDatabase.EffectType.Stats:
            NBMonTeamData.StatsValueChange(useMonsterMemory, passiveEffect.statsType, passiveEffect.valueChange);
            break;
        case PassiveDatabase.EffectType.DuringBattle:
            PassiveDuringBattleLogic(passiveEffect, skill, seedClass);
            break;
        case PassiveDatabase.EffectType.ApplySelfTemporaryPassive:
            ApplySelfTempPassive(passiveEffect);
            break;
    }
}


    //Apply Passive that gives Status Effect
    private static void PassiveStatusEffectLogic(PassiveDatabase.EffectInfo passiveEffect, RNGSeedClass seedClass)
    {
        // Apply status effect to useMonster
        UseItem.ApplyStatusEffect(useMonsterMemory, passiveEffect.statusEffectInfoList, null, true, seedClass);
        UseItem.RemoveStatusEffect(useMonsterMemory, passiveEffect.removeStatusEffectInfoList);
        AttackFunction.HardCodedRemoveStatusEffect(useMonsterMemory, passiveEffect.statusRemoveType_Self);

        // Determine teams
        var alliesTeam = NBMonTeamData.PlayerTeam;
        var enemyTeam = NBMonTeamData.EnemyTeam;
        if (enemyTeam.Contains(useMonsterMemory))
        {
            alliesTeam = NBMonTeamData.EnemyTeam;
            enemyTeam = NBMonTeamData.PlayerTeam;
        }

        // Apply status effects and remove enemy status effects on allies team
        TeamStatusEffectLogic(alliesTeam, originMonsterMemory, passiveEffect, seedClass, passiveEffect.statusRemoveType_Allies);

        // Apply status effects and remove enemy status effects on opposing team
        TeamStatusEffectLogic(enemyTeam, null, passiveEffect, seedClass, passiveEffect.statusRemoveType_Enemies);
    }

    private static void TeamStatusEffectLogic(List<NBMonBattleDataSave> team, NBMonBattleDataSave exceptMonster, PassiveDatabase.EffectInfo passiveEffect, RNGSeedClass seedClass, SkillsDataBase.RemoveStatusEffectType statusRemoveType)
    {
        foreach (var monster in team)
        {
            if (monster == exceptMonster)
                continue;

            UseItem.ApplyStatusEffect(monster, passiveEffect.teamStatusEffectInfoList, null, true, seedClass);
            UseItem.RemoveStatusEffect(monster, passiveEffect.removeEnemyTeamStatusEffectInfoList);
            AttackFunction.HardCodedRemoveStatusEffect(monster, statusRemoveType);
        }
    }


    //Apply Passive that works During Battle
    private static void PassiveDuringBattleLogic(PassiveDatabase.EffectInfo passiveEffect, SkillsDataBase.SkillInfoPlayFab skill, RNGSeedClass seedClass)
    {
        //RNG for Passive Effects that works During Battle
        var rng = EvaluateOrder.ConvertSeedToRNG(seedClass);

        if (rng > passiveEffect.triggerChance)
            return;

        //Apply the Temporary Stats to the mosnter.
        useMonsterMemory.attackBuff += (float)passiveEffect.attackBuff;
        useMonsterMemory.specialAttackBuff += (float)passiveEffect.specialAttackBuff;
        useMonsterMemory.defenseBuff += (float)passiveEffect.defenseBuff;
        useMonsterMemory.specialDefenseBuff += (float)passiveEffect.specialDefenseBuff;
        useMonsterMemory.criticalBuff += (float)passiveEffect.criticalBuff;
        useMonsterMemory.ignoreDefenses += (float)passiveEffect.ignoreDefenses;
        useMonsterMemory.damageReduction += (float)passiveEffect.damageReduction;
        useMonsterMemory.energyShieldValue += (float)passiveEffect.energyShieldValue;
        useMonsterMemory.absorbDamageValue += (float)passiveEffect.absorbDamageValue;

        //Bool Section but using Integer
        useMonsterMemory.energyShield += passiveEffect.energyShield;
        useMonsterMemory.mustCritical += passiveEffect.mustCritical;
        useMonsterMemory.surviveLethalBlow += passiveEffect.surviveLethalBlow;
        useMonsterMemory.totalIgnoreDefense += passiveEffect.totalIgnoreDefense;
        useMonsterMemory.immuneCritical += passiveEffect.immuneCritical;

        //Elemental Damage Reduction
        foreach (var elementDamageReduction in passiveEffect.ElementalDamageReductions)
        {
            if (skill.skillElement == elementDamageReduction.SkillElementTaken)
            {
                useMonsterMemory.elementDamageReduction += elementDamageReduction.DamageReductionValue;
            }
        }
    }

    //Apply Self Temporary Passive
    private static void ApplySelfTempPassive(PassiveDatabase.EffectInfo passiveEffect)
    {
        foreach (var tempPassive in passiveEffect.newTemporaryPassives)
        {
            if (!useMonsterMemory.temporaryPassives.Contains(tempPassive))
            {
                useMonsterMemory.temporaryPassives.Add(tempPassive);
            }
        }
    }

    //Related to Passive Requirement Booleans
    private static bool StatusEffectRequirementLogic(SkillsDataBase.SkillInfoPlayFab skill, List<NBMonProperties.StatusEffect> checkStatusListMemory, PassiveDatabase.Requirements requirements)
    {
        checkStatusListMemory.Clear();
        foreach (var statusEffectGroup in skill.statusEffectList)
        {
            checkStatusListMemory.Add(statusEffectGroup.statusEffect);
        }

        return checkStatusListMemory.Contains(requirements.statusEffect);
    }

    private static bool HaveStatusEffectRequirementLogic(bool returnValue, PassiveDatabase.Requirements requirements)
    {
        if (requirements.statusEffect != NBMonProperties.StatusEffect.None)
        {
            returnValue = FindNBMonStatusEffect(useMonsterMemory, requirements.statusEffect) != null;
        }
        else
        {
            var effectType = requirements.statusRemovalRequirement;
            foreach (var statusEffect in useMonsterMemory.statusEffectList)
            {
                var statusEffectData = UseItem.FindStatusEffectFromDatabase(statusEffect.statusEffect);
                if (statusEffectData.statusEffectType == effectType)
                {
                    returnValue = false;
                    break;
                }
            }
        }

        return returnValue;
    }


    private static bool StatsValueRequirementLogic(bool returnValue, PassiveDatabase.Requirements requirements)
    {
        foreach (var statsRequirement in requirements.statsValueIsRequired)
        {
            var statValue = statsRequirement.stats == NBMonProperties.InputChange.HP_Percent ? useMonsterMemory.hp : useMonsterMemory.energy;
            var requiredValue = statsRequirement.value * (statsRequirement.stats == NBMonProperties.InputChange.HP_Percent ? useMonsterMemory.maxHp : useMonsterMemory.maxEnergy) / 100;
            
            switch (statsRequirement.Numerator)
            {
                case PassiveDatabase.Numerator.Same:
                    returnValue = (statValue == requiredValue);
                    break;
                case PassiveDatabase.Numerator.BiggerThan:
                    returnValue = (statValue >= requiredValue);
                    break;
                case PassiveDatabase.Numerator.SmallerThan:
                    returnValue = (statValue <= requiredValue);
                    break;
            }
        }

        return returnValue;
    }
}