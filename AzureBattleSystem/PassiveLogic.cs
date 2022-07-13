using System;
using System.Collections.Generic;

public class PassiveLogic
{
    private static NBMonBattleDataSave originMonsterMemory;
    private static NBMonBattleDataSave targetMonsterMemory;
    private static NBMonBattleDataSave useMonsterMemory;

    //Logics
    //Apply the passive according to the targetting type in script
    public static void ApplyPassive(PassiveDatabase.ExecutionPosition executionPosition, PassiveDatabase.TargetType targetType, NBMonBattleDataSave originMonsterPass, NBMonBattleDataSave targetMonsterPass, SkillsDataBase.SkillInfoPlayFab skill, string BattleEnvironment = "")
    {
        //Set the local data value 
        originMonsterMemory = originMonsterPass; //Current Used Monster
        targetMonsterMemory = targetMonsterPass; //Target Monster

        //Check all the passive from the original monster
        if (targetType == PassiveDatabase.TargetType.originalMonster || targetType == PassiveDatabase.TargetType.both)
        {
            useMonsterMemory = originMonsterMemory;

            foreach (var passive in originMonsterPass.passiveList)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, BattleEnvironment);
            }

            foreach (var tempPassive in originMonsterPass.temporaryPassives)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(tempPassive), skill, BattleEnvironment);
            }
        }

        //Check all the passive from the target monster
        if (targetType == PassiveDatabase.TargetType.targetedMonster || targetType == PassiveDatabase.TargetType.both)
        {
            useMonsterMemory = targetMonsterMemory;

            foreach (var passive in targetMonsterPass.passiveList)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(passive), skill, BattleEnvironment);
            }

            foreach (var tempPassive in targetMonsterPass.temporaryPassives)
            {
                PassiveExecutionLogic(executionPosition, PassiveDatabase.FindPassiveSkill(tempPassive), skill, BattleEnvironment);
            }
        }
    }

    //Apply the passive, this is the logic we'd like to call!
    public static void PassiveExecutionLogic(PassiveDatabase.ExecutionPosition executionPosition, PassiveDatabase.PassiveInfoPlayFab passiveInfo, SkillsDataBase.SkillInfoPlayFab skill, string BattleEnvironment)
    {
        //Only Check The Passive When The Execution Position Is Correct
        if (executionPosition == passiveInfo.executionPosition && passiveInfo != null)
        {
            //Checks If Passive Requirement is Correct
            //Debug.Log(passiveInfo.name + " " + passiveInfo.executionPosition + " " + targetMonsterMemory.name);

            foreach (var passiveDetail in passiveInfo.passiveDetail)
            {
                if (CheckPassiveRequirement(passiveDetail, passiveDetail.requirements, skill, BattleEnvironment))
                {
                    foreach(var checkTarget in passiveDetail.requirements)
                    {
                        if (checkTarget.monsterTargetingCheck && targetMonsterMemory != null)
                            useMonsterMemory = targetMonsterMemory;
                    }

                    //Apply Each Passive Int The List
                    foreach (var passive in passiveDetail.effect)
                    {
                        //Do the passive
                        DoPassive(passive, skill);
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

        bool returnValue = true;

        //Requirement Logic
        foreach (var requirements in passiveRequirements)
        {
            //Logic filters out all the requirement. When at least one requirement do not match, make the return value false;
            if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionUsingElement)
            {
                //Checks if all the skill element is correct, if not it will change the retur value to false!
                if (skill.skillElement != requirements.skillElement)
                {
                    returnValue = false;
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingElement)
            {
                //Checks if all the skill element is correct, if not it will change the return value to false!
                if (skill.skillElement != requirements.skillElement)
                {
                    returnValue = false;
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionUsingTechnique)
            {
                if(requirements.skillLists.Count != 0)
                {
                    //Check if the used skills are inside the Skill List from the passive
                    if (requirements.skillLists.Contains(skill.skillName))
                    {
                        returnValue = true;
                    }
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingTechnique)
            {
                
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.StatsValueReq)
            {
                foreach (var statsRequirement in requirements.statsValueIsRequired)
                {
                    if (statsRequirement.stats == NBMonProperties.InputChange.HP_Percent) //Check by HP vs Max HP Value
                    {
                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.Same)
                        {
                            if (useMonsterMemory.hp == useMonsterMemory.maxHp * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }

                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.BiggerThan)
                        {
                            if (useMonsterMemory.hp > useMonsterMemory.maxHp * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }

                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.SmallerThan)
                        {
                            if (useMonsterMemory.hp < useMonsterMemory.maxHp * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }
                    }

                    if (statsRequirement.stats == NBMonProperties.InputChange.Energy_Percent) //Check by Energy vs Max Energy Value
                    {
                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.Same)
                        {
                            if (useMonsterMemory.energy == useMonsterMemory.maxEnergy * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }

                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.BiggerThan)
                        {
                            if (useMonsterMemory.energy > useMonsterMemory.maxEnergy * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }

                        if (statsRequirement.Numerator == PassiveDatabase.Numerator.SmallerThan)
                        {
                            if (useMonsterMemory.energy < useMonsterMemory.maxEnergy * statsRequirement.value / 100)
                                returnValue = true;
                            else
                                returnValue = false;
                        }
                    }
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.HaveStatusEffect) //Has Status Effect
            {
                //Checks if the useMonster have the status or not
                if (FindNBMonStatusEffect(useMonsterMemory, requirements.statusEffect) == null)
                {
                    returnValue = false;
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionGivingStatusEffect)
            {
                //Create the temporary list memory based on the status effect group of the action
                checkStatusListMemory.Clear();
                foreach (var statusEffectGroup in skill.statusEffectList)
                {
                    checkStatusListMemory.Add(statusEffectGroup.statusEffect);
                }

                if (!checkStatusListMemory.Contains(requirements.statusEffect))
                {
                    //If the status effect list do not contain the status effect, returns false
                    returnValue = false;
                }

                return true;

            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.ActionReceivingStatusEffect)
            {
                //Create the temporary list memory based on the status effect group of the action
                checkStatusListMemory.Clear();
                foreach (var statusEffectGroup in skill.statusEffectList)
                {
                    checkStatusListMemory.Add(statusEffectGroup.statusEffect);
                }

                if (!checkStatusListMemory.Contains(requirements.statusEffect))
                {
                    //If the status effect list do not contain the status effect, returns false
                    returnValue = false;
                }

            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.Fainted)
            {
                //Check if useMonster is fainted
                if (!useMonsterMemory.fainted)
                {
                    returnValue = false;
                }
            }
            else if (requirements.requirementTypes == PassiveDatabase.RequirementTypes.Environment)
            {
                if (!requirements.EnvinromentLists.Contains(BattleEnvironment))
                    return false;
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
    private static void DoPassive(PassiveDatabase.EffectInfo passiveEffect, SkillsDataBase.SkillInfoPlayFab skill)
    {
        if (passiveEffect.effectType == PassiveDatabase.EffectType.StatusEffect)
        {
            //Add the Status Effect to the Monster.
            UseItem.ApplyStatusEffect(useMonsterMemory, passiveEffect.statusEffectInfoList, null, true);
            
            //Remove the Status Effect to the Monster.
            UseItem.RemoveStatusEffect(useMonsterMemory, passiveEffect.removeStatusEffectInfoList);

            //Default Condition
            var SelfTeam = NBMonTeamData.PlayerTeam;
            var EnemyTeam = NBMonTeamData.EnemyTeam;

            //Check if the Original Monster is an Enemy Team
            if(EnemyTeam.Contains(useMonsterMemory))
            {
                SelfTeam = NBMonTeamData.EnemyTeam;
                EnemyTeam = NBMonTeamData.PlayerTeam;
            }

            //Self Team 
            foreach(var Monsters in SelfTeam)
            {
                if (Monsters == originMonsterMemory)
                    continue;

                UseItem.ApplyStatusEffect(Monsters, passiveEffect.teamStatusEffectInfoList, null, true);
                UseItem.RemoveStatusEffect(Monsters, passiveEffect.removeEnemyTeamStatusEffectInfoList);
            }

            //Opposing Team
            foreach(var Monsters in EnemyTeam)
            {
                UseItem.ApplyStatusEffect(Monsters, passiveEffect.teamStatusEffectInfoList, null, true);
                UseItem.RemoveStatusEffect(Monsters, passiveEffect.removeEnemyTeamStatusEffectInfoList);
            }
            
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
            //RNG for Passive Effects that works During Battle
            Random R = new Random(); 
            var RNG = R.Next(1, 100);


            if(RNG <= passiveEffect.triggerChance)
            {
                //Apply the Temporary Stats to the mosnter.
                useMonsterMemory.attackBuff += (float)passiveEffect.attackBuff;
                useMonsterMemory.specialAttackBuff += (float)passiveEffect.specialAttackBuff;
                useMonsterMemory.defenseBuff += (float)passiveEffect.defenseBuff;
                useMonsterMemory.specialDefenseBuff += (float)passiveEffect.specialDefenseBuff;
                useMonsterMemory.criticalBuff += (float)passiveEffect.criticalBuff;
                useMonsterMemory.ignoreDefenses += (float)passiveEffect.ignoreDefenses;
                useMonsterMemory.damageReduction += (float)passiveEffect.damageReduction;
                useMonsterMemory.energyShieldValue += passiveEffect.energyShieldValue;

                //Bool Section but using Integer
                useMonsterMemory.energyShield += passiveEffect.energyShield;
                useMonsterMemory.mustCritical += passiveEffect.mustCritical;
                useMonsterMemory.surviveLethalBlow += passiveEffect.surviveLethalBlow;
                useMonsterMemory.totalIgnoreDefense += passiveEffect.totalIgnoreDefense;
                useMonsterMemory.immuneCritical += passiveEffect.immuneCritical;

                //Elemental Damage Reduction
                foreach(var ElementDamageReduction in passiveEffect.ElementalDamageReductions)
                {
                    if(skill.skillElement == ElementDamageReduction.SkillElementTaken)
                    {
                        useMonsterMemory.elementDamageReduction += ElementDamageReduction.DamageReductionValue;
                    }
                }
            }
        }
    }
}