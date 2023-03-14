using System;
using System.Collections.Generic;
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
        //Get Battle Environment Value.
        var BattleEnvironment = AttackFunction.BattleEnvironment;

        //Set the local data value 
        originMonsterMemory = originMonsterPass; //Current Used Monster
        targetMonsterMemory = targetMonsterPass; //Target Monster

        //Check all the passive from the original monster
        if (targetType == PassiveDatabase.TargetType.originalMonster || targetType == PassiveDatabase.TargetType.both)
        {
            useMonsterMemory = originMonsterMemory;

            foreach (var passive in originMonsterPass.passiveList)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, BattleEnvironment, seedClass);
                //To do, change the PassiveDatabase.FindPassiveSkill(passive) into the variable you created, applies to other as well.
            }

            foreach (var tempPassive in originMonsterPass.temporaryPassives)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(tempPassive), skill, BattleEnvironment, seedClass);
            }
        }

        //Check all the passive from the target monster
        if (targetType == PassiveDatabase.TargetType.targetedMonster || targetType == PassiveDatabase.TargetType.both)
        {
            useMonsterMemory = targetMonsterMemory;

            foreach (var passive in targetMonsterPass.passiveList)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, BattleEnvironment, seedClass);
            }

            foreach (var tempPassive in targetMonsterPass.temporaryPassives)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(tempPassive), skill, BattleEnvironment, seedClass);
            }
        }
    }

    //Apply the passive, this is the logic we'd like to call!
    public static void PassiveExecutionLogic(PassiveDatabase.ExecutionPosition executionPosition, PassiveDatabase.PassiveInfoPlayFab passiveInfo, SkillsDataBase.SkillInfoPlayFab skill, string BattleEnvironment, RNGSeedClass seedClass)
    {
        //Loop between Passive Detail
        foreach (var passiveDetail in passiveInfo.passiveDetail)
        {
            //Only Check The Passive When The Execution Position Is Correct
            if (executionPosition == passiveDetail.executionPosition && passiveInfo != null)
            {
                //Checks If Passive Requirement is Correct
                if (CheckPassiveRequirement(passiveDetail, passiveDetail.requirements, skill, BattleEnvironment))
                {
                    foreach(var checkTarget in passiveDetail.requirements)
                    {
                        if (checkTarget.monsterTargetingCheck && targetMonsterMemory != null && !checkTarget.monsterTargeting_EffectToSelf)
                            useMonsterMemory = targetMonsterMemory;
                        else
                            useMonsterMemory = originMonsterMemory;
                    }

                    //Apply Each Passive Int The List
                    foreach (var passive in passiveDetail.effect)
                    {
                        //Do the passive
                        DoPassive(passive, skill, seedClass);
                    }

                }
            }
        }
    }

    //Checks the list of modifier requirements
    private static bool CheckPassiveRequirement(PassiveDatabase.PassiveDetail passiveDetail, List<PassiveDatabase.Requirements> passiveRequirements, SkillsDataBase.SkillInfoPlayFab skill, string BattleEnvironment) 
    { 
        //Declare New Variable
        List<NBMonProperties.StatusEffect> checkStatusListMemory = new List<NBMonProperties.StatusEffect>();

        //Declare default returnValue as true;
        bool returnValue = true;

        //Requirement Logic
        foreach (var requirements in passiveRequirements)
        {
            //Logic filters out all the requirement. When at least one requirement do not match, make the return value false;
            if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionUsingElement)
            {
                //Checks if all the skill element is correct, if not it will change the retur value to false!
                return (skill.skillElement == requirements.skillElement);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingElement)
            {
                //Checks if all the skill element is correct, if not it will change the return value to false!
                return (skill.skillElement == requirements.skillElement);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionUsingTechnique)
            {
                //Check if the used skills are inside the Skill List from the passive
                return requirements.skillLists.Contains(skill.skillName);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingTechnique)
            {
                //Check if the used skills are inside the Skill List from the passive
                return requirements.skillLists.Contains(skill.skillName);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.StatsValueReq)
            {
                return StatsValueRequirementLogic(returnValue, requirements);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.HaveStatusEffect) //Has Status Effect
            {
                return HaveStatusEffectRequirementLogic(returnValue, requirements);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionGivingStatusEffect)
            {
                //Create the temporary list memory based on the status effect group of the action
                return StatusEffectRequirementLogic(skill, checkStatusListMemory, requirements);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingStatusEffect)
            {
                //Create the temporary list memory based on the status effect group of the action
                return StatusEffectRequirementLogic(skill, checkStatusListMemory, requirements);
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.Fainted)
            {
                //Check if useMonster is fainted
                return useMonsterMemory.fainted;
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.Environment)
            {
                return requirements.EnvinromentLists.Contains(BattleEnvironment);
            }
        }

        return returnValue;
    }

    //Find This Monster Status Effect
    public static StatusEffectList FindNBMonStatusEffect(NBMonBattleDataSave ThisMonster, NBMonProperties.StatusEffect StatusEffect)
    {
        foreach(var StatusInMonster in ThisMonster.statusEffectList)
        {
            if(StatusInMonster.statusEffect == (int)StatusEffect && StatusInMonster.counter > 0)
            {
                return StatusInMonster;
            }
        }

        //If none found, return False.
        return null;
    }

    //Apply the passive effect and change the monster stats based on it
    private static void DoPassive(PassiveDatabase.EffectInfo passiveEffect, SkillsDataBase.SkillInfoPlayFab skill, RNGSeedClass seedClass)
    {
        if (passiveEffect.effectType == PassiveDatabase.EffectType.StatusEffect)
        {
            PassiveStatusEffectLogic(passiveEffect, seedClass);

        }
        else if (passiveEffect.effectType == PassiveDatabase.EffectType.StatsPercentage)
        {
            //Apply the stats change to the useMonster
            NBMonTeamData.StatsPercentageChange(useMonsterMemory, passiveEffect.statsType, passiveEffect.valueChange);
        }
        else if (passiveEffect.effectType == PassiveDatabase.EffectType.Stats)
        {
            //Apply the stats change to the useMonster
            NBMonTeamData.StatsValueChange(useMonsterMemory, passiveEffect.statsType, passiveEffect.valueChange);
        }
        else if (passiveEffect.effectType == PassiveDatabase.EffectType.DuringBattle)
        {
            PassiveDuringBattleLogic(passiveEffect, skill, seedClass);
        }
        else if (passiveEffect.effectType == PassiveDatabase.EffectType.ApplySelfTemporaryPassive)
        {
            ApplySelfTempPassive(passiveEffect);
        }
    }

    //Apply Passive that gives Status Effect
    private static void PassiveStatusEffectLogic(PassiveDatabase.EffectInfo passiveEffect, RNGSeedClass seedClass)
    {
        //Add the Status Effect to the Monster.
        UseItem.ApplyStatusEffect(useMonsterMemory, passiveEffect.statusEffectInfoList, null, true, seedClass);

        //Remove the Status Effect to the Monster.
        UseItem.RemoveStatusEffect(useMonsterMemory, passiveEffect.removeStatusEffectInfoList);

        AttackFunction.HardCodedRemoveStatusEffect(useMonsterMemory, passiveEffect.statusRemoveType_Self);

        //Default Condition
        var alliesTeam = NBMonTeamData.PlayerTeam;
        var enemyTeam = NBMonTeamData.EnemyTeam;

        //Check if the Original Monster is an Enemy Team
        if (enemyTeam.Contains(useMonsterMemory))
        {
            alliesTeam = NBMonTeamData.EnemyTeam;
            enemyTeam = NBMonTeamData.PlayerTeam;
        }

        //Self Team 
        foreach (var monster in alliesTeam)
        {
            if (monster == originMonsterMemory)
                continue;

            UseItem.ApplyStatusEffect(monster, passiveEffect.teamStatusEffectInfoList, null, true, seedClass);
            UseItem.RemoveStatusEffect(monster, passiveEffect.removeEnemyTeamStatusEffectInfoList);
            AttackFunction.HardCodedRemoveStatusEffect(monster, passiveEffect.statusRemoveType_Allies);
        }

        //Opposing Team
        foreach (var monster in enemyTeam)
        {
            UseItem.ApplyStatusEffect(monster, passiveEffect.teamStatusEffectInfoList, null, true, seedClass);
            UseItem.RemoveStatusEffect(monster, passiveEffect.removeEnemyTeamStatusEffectInfoList);
            AttackFunction.HardCodedRemoveStatusEffect(monster, passiveEffect.statusRemoveType_Enemies);
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
        //Check if the passive's status effect requirement is not None.
        if (requirements.statusEffect != NBMonProperties.StatusEffect.None)
        {
            returnValue = FindNBMonStatusEffect(useMonsterMemory, requirements.statusEffect) != null;
        }
        else //If its None, check if the passive requires having any Negative Status Effect or Not.
        {
            switch (requirements.statusRemovalRequirement)
            {
                case SkillsDataBase.RemoveStatusEffectType.Negative:
                {
                    //Do not trigger passive if the monster has negative status effect.
                    foreach (var statusEffect in useMonsterMemory.statusEffectList)
                    {
                        var statusEffectData = UseItem.FindStatusEffectFromDatabase(statusEffect.statusEffect);

                        if (statusEffectData.statusEffectType == SkillsDataBase.RemoveStatusEffectType.Negative)
                            returnValue = false;
                    }

                    break;
                }

                case SkillsDataBase.RemoveStatusEffectType.Positive:
                {
                    //Do not trigger passive if the monster has positive status effect.
                    foreach (var statusEffect in useMonsterMemory.statusEffectList)
                    {
                        var statusEffectData = UseItem.FindStatusEffectFromDatabase(statusEffect.statusEffect);

                        if (statusEffectData.statusEffectType == SkillsDataBase.RemoveStatusEffectType.Positive)
                            returnValue = false;
                    }

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
            if (statsRequirement.stats == NBMonProperties.InputChange.HP_Percent) //Check by HP vs Max HP Value
            {
                switch (statsRequirement.Numerator)
                {
                    case PassiveDatabase.Numerator.Same:
                        returnValue = (useMonsterMemory.hp == useMonsterMemory.maxHp * statsRequirement.value / 100);
                        break;
                    case PassiveDatabase.Numerator.BiggerThan:
                        returnValue = (useMonsterMemory.hp >= useMonsterMemory.maxHp * statsRequirement.value / 100);
                        break;
                    case PassiveDatabase.Numerator.SmallerThan:
                        returnValue = (useMonsterMemory.hp < useMonsterMemory.maxHp * statsRequirement.value / 100);
                        break;
                }
            }
            else //Check by Energy vs Max Energy Value
            {
                switch (statsRequirement.Numerator)
                {
                    case PassiveDatabase.Numerator.Same:
                        returnValue = (useMonsterMemory.energy == useMonsterMemory.maxEnergy * statsRequirement.value / 100);
                        break;
                    case PassiveDatabase.Numerator.BiggerThan:
                        returnValue = (useMonsterMemory.energy >= useMonsterMemory.maxEnergy * statsRequirement.value / 100);
                        break;
                    case PassiveDatabase.Numerator.SmallerThan:
                        returnValue = (useMonsterMemory.energy <= useMonsterMemory.maxEnergy * statsRequirement.value / 100);
                        break;
                }
            }
        }

        return returnValue;
    }
}