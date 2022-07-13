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

public static class AttackFunction
{
    //Helper Methods
    public static PlayFabServerInstanceAPI SetupServerAPI(dynamic args, FunctionExecutionContext<dynamic> context)
    {
        var apiSettings = new PlayFabApiSettings
        {
            TitleId = context.TitleAuthenticationContext.Id,
            DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
        };

        var authContext = new PlayFabAuthenticationContext
        {
            EntityId = context.TitleAuthenticationContext.EntityToken
        };

        return new PlayFabServerInstanceAPI(apiSettings, authContext);
    }

    //Data Send to Client
    public class DamageData
    {
        public string DefenderMonsterUniqueID; //Defender Monster Unique ID
        public int Damage; //Normal Damage
        public int SPDamage; //Special Damage
        public int EnergyDamage; //Energy Damage
        public bool IsCritical; //Determine if the Damage is Critical Hit.
        public bool DamageImmune; //Determine if the Monster is Immune to the Damage.
        public int ElementalVariable; //Determine if the Monster is Very Effective or Not.

        //HP and Energy Drain to Attack Monster
        public int HPDrained;
        public int EnergyDrained;
    }

    //Data We gonna send to Client
    public class DataSendToUnity
    {
        public string AttackerMonsterUniqueID;
        public List<DamageData> DamageDatas;
    }

    //Data Send From Client to Azure
    public class DataFromUnity
    {
        public string AttackerMonsterUniqueID;
        public int SkillSlot;
        public List<string> TargetUniqueIDs;

    }

    //Damage Function
    //Calculate and do the damage to the monster
    public static void CalculateAndDoDamage(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave AttackerMonster, NBMonBattleDataSave DefenderMonster)
    {
        //Declare Random Variable
        Random R = new Random();

        //Apply passives and artifact passives that only works during combat to Attacker
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.DuringCombat, PassiveDatabase.TargetType.originalMonster, AttackerMonster, null, null);
        //TO DO, Apply Artifact Passive to Attacker Monster.

        //Apply passives and artifact passives that only works during combat to This Monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.DuringCombat, PassiveDatabase.TargetType.originalMonster, DefenderMonster, null, null);
        //TO DO, Apply Artifact Passive to Defender Monster.

        //Calculate Critical Hit RNG
        int ThisNBMonCriticalHitRate = CriticalHitStatsCalculation(AttackerMonster, skill);
        int CriticalRNG = R.Next(0, 100);
        float CriticalHitMultiplier = 1f;
        int MustCritical = AttackerMonster.mustCritical;
        
        //Check Target's Immune to Critical or Not
        int ImmuneToCritical = DefenderMonster.immuneCritical;

        if (CriticalRNG <= ThisNBMonCriticalHitRate || MustCritical >= 1)
            CriticalHitMultiplier = (float)R.NextDouble() * (3f-1.5f) + 1.5f;
        else if (ImmuneToCritical >= 1)
            CriticalHitMultiplier = 1f;

        //Calculate Attacker's Attack Buffs Modifier from Status Effects
        float Attack_StatusEffect_Modifier = StatusEffectIconDatabase.AttackStatusEffectLogic(AttackerMonster);
        float SP_Attack_StatusEffect_Modifier = StatusEffectIconDatabase.SPAttackStatusEffectLogic(AttackerMonster);

        //Find Attacker's Attack Buffs from Passives
        float Attack_Passive_Modifier = AttackerMonster.attackBuff/100;
        float SP_Attack_Passive_Modifier = AttackerMonster.specialAttackBuff/100;
        float IgnoreDefenses = AttackerMonster.ignoreDefenses/100;
        int TotalIngoreDefense = AttackerMonster.totalIgnoreDefense;

        //Normalizations
        if (IgnoreDefenses > 1)
            IgnoreDefenses = 1f;

        //Calculate This Monster's Defense Buffs Modifier from Status Effects
        float This_NBMon_Def_Modifier = StatusEffectIconDatabase.DefenseStatusEffectLogic(DefenderMonster);
        float This_NBMon_SP_Def_Modifier = StatusEffectIconDatabase.SPDefenseStatusEffectLogic(DefenderMonster);

        //Find This Monster's Defense Buffs Modifier from Passives
        float This_NBMon_Def_Passive_Modifier = DefenderMonster.defenseBuff/100;
        float This_NBMon_SP_Def_Passive_Modifier = DefenderMonster.specialDefenseBuff/100;
        float This_NBMon_Damage_Reduction_Modifier = DefenderMonster.damageReduction/100;
        float This_NBMon_Damage_Reduction_From_Energy_Shield = DefenderMonster.energyShieldValue/100;
        int This_NBMon_EnergyShield_IsActive = DefenderMonster.energyShield;
        int This_NBMon_Must_Survive_Lethal_Blow = DefenderMonster.surviveLethalBlow;
        float ElementalDamageReduction = 1f - ((float)DefenderMonster.elementDamageReduction) / 100;

        //Normalizations Damage Reduction
        if (This_NBMon_Damage_Reduction_Modifier > 1f)
            This_NBMon_Damage_Reduction_Modifier = 1f;

        //Normalizations Damage Reduction from Energy Shield
        if (This_NBMon_Damage_Reduction_From_Energy_Shield > 1f)
            This_NBMon_Damage_Reduction_From_Energy_Shield = 1f;

        //Check if the attacker's Total Ignore Defense value is 1 or more (this integer act as Bool)
        if (TotalIngoreDefense >= 1)
        {
            //This NBMon's Damage Reduction set to 0f.
            This_NBMon_Damage_Reduction_Modifier = 0f;

            //Attacker's Ignore Defenses set to 1f (aka 100%).
            IgnoreDefenses = 1f;
        }

        //Check if this NBMon's energy shield is active or not (indicator: more than or same as 1 = true, less than 1 = false)
        if (This_NBMon_EnergyShield_IsActive < 1)
            This_NBMon_Damage_Reduction_From_Energy_Shield = 0f;

        //Calculate the base attack value
        int BaseAttack = (int)Math.Floor(Attack_StatusEffect_Modifier * (1f + Attack_Passive_Modifier) * ((float)skill.attack + (float)AttackerMonster.attack)/1.5f);
        int BaseSPAttack = (int)Math.Floor(SP_Attack_StatusEffect_Modifier * (1f + SP_Attack_Passive_Modifier) * ((float)skill.specialAttack + (float)AttackerMonster.specialAttack)/1.5f);

        //Calculate this NBMon's Def
        float ThisMonsterDef = (float)DefenderMonster.defense * This_NBMon_Def_Modifier * (1f + This_NBMon_Def_Passive_Modifier) * (1f - IgnoreDefenses) * ElementalDamageReduction;
        float ThisMonsterSPDef = (float)DefenderMonster.specialDefense * This_NBMon_SP_Def_Modifier * (1f + This_NBMon_SP_Def_Passive_Modifier) * (1f - IgnoreDefenses) * ElementalDamageReduction;

        //Skill output passive on offense + skill input passive on defense
        int BaseDamageAttack = BaseAttack - (int)Math.Floor(ThisMonsterDef);
        int BaseDamageSPAttack = BaseSPAttack - (int)Math.Floor(ThisMonsterSPDef);

        //Calculate the modifier value
        float ElementModifier = CalculateElementModifierForSkill_Prototype(skill, DefenderMonster);

        //Finishing Touch
        int damageAttack = (int)Math.Floor((float)BaseDamageAttack * (float)ElementModifier * CriticalHitMultiplier * (1f - This_NBMon_Damage_Reduction_Modifier) * (1f - This_NBMon_Damage_Reduction_From_Energy_Shield));
        int damageSpAttack = (int)Math.Floor((float)BaseDamageSPAttack * (float)ElementModifier * CriticalHitMultiplier * (1f - This_NBMon_Damage_Reduction_Modifier) * (1f - This_NBMon_Damage_Reduction_From_Energy_Shield));

        //Energy Damage from Energy Shield
        int energyDamageAttack = (int)Math.Floor((float)BaseDamageAttack * (float)ElementModifier * CriticalHitMultiplier * (1f - This_NBMon_Damage_Reduction_Modifier) * (This_NBMon_Damage_Reduction_From_Energy_Shield));
        int energyDamageSPAttack = (int)Math.Floor((float)BaseDamageSPAttack * (float)ElementModifier * CriticalHitMultiplier * (1f - This_NBMon_Damage_Reduction_Modifier) * (This_NBMon_Damage_Reduction_From_Energy_Shield));

        //Survive Lethal Blow Logic
        if(This_NBMon_Must_Survive_Lethal_Blow >= 1)
        {
            int This_Monster_HP = DefenderMonster.hp;

            if (This_Monster_HP > 1 && damageAttack > This_Monster_HP)
                damageAttack = This_Monster_HP - 1;

            if(This_Monster_HP > 1 && damageSpAttack > This_Monster_HP)
                damageSpAttack = This_Monster_HP - 1;
        }

        // Reduce target NBMon's HP
        if (skill.techniqueType == SkillsDataBase.TechniqueType.Attack)
        {
            if(ElementModifier != 0)
            {
                // If Damage is less than 0, make sure to return 1 damage
                if (damageAttack <= 0 && (This_NBMon_Damage_Reduction_Modifier != 1 || This_NBMon_Damage_Reduction_From_Energy_Shield != 1))
                    damageAttack = 1;

                //Normal Damage
                NBMonTeamData.StatsValueChange(DefenderMonster ,NBMonProperties.StatsType.Hp, damageAttack * -1);

                //Energy Damage
                if (This_NBMon_EnergyShield_IsActive >= 1)
                {
                    NBMonTeamData.StatsValueChange(DefenderMonster ,NBMonProperties.StatsType.Energy, energyDamageAttack * -1);
                }

                //HP and Energy Drain Logic -> Only for Skill with TechniqueType of Attack
                HP_EN_DrainFunction(skill, damageAttack, AttackerMonster);
            }
            else
            {
                //By Default, damageAttack value is 0.
            }
        }
        
        if(skill.techniqueType == SkillsDataBase.TechniqueType.SpecialAttack)
        {
            // If Damage is less than 0, make sure to return 1 damage
            if (damageSpAttack <= 0 && This_NBMon_Damage_Reduction_Modifier != 1)
                damageSpAttack = 1;

            //Normal SP Damage
            NBMonTeamData.StatsValueChange(DefenderMonster, NBMonProperties.StatsType.Hp, damageSpAttack * -1);

            if (This_NBMon_EnergyShield_IsActive >= 1)
            {
                //Energy Damage
                NBMonTeamData.StatsValueChange(DefenderMonster, NBMonProperties.StatsType.Energy, energyDamageSPAttack * -1);
            }
        }
    }

    private static int CriticalHitStatsCalculation(NBMonBattleDataSave originalMonster, SkillsDataBase.SkillInfoPlayFab skill)
    {
        int CriticalHit_Skill = skill.criticalRate;
        int CriticalHit_Passive = (int)Math.Floor(originalMonster.criticalBuff);
        int CriticalHit_NBMon = originalMonster.criticalHit;
        int CriticalHit_NBMon_From_StatusEffect = StatusEffectIconDatabase.CriticalStatusEffectLogic(originalMonster);

        return CriticalHit_NBMon + CriticalHit_NBMon_From_StatusEffect + CriticalHit_Skill + CriticalHit_Passive;

    }

    // Calculate and return the element modifier value
    private static float CalculateElementModifierForSkill_Prototype(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave DefenderMonster)
    {
        var MonsterInfoFromDatabase = NBMonDatabase.FindMonster(DefenderMonster.monsterId);

        float modifier_element_1 = ElementDatabase.FindElementValueModifier_Prototype(MonsterInfoFromDatabase.elements[0], skill.skillElement);
        float modifier_element_2 = ElementDatabase.FindElementValueModifier_Prototype(MonsterInfoFromDatabase.elements[1], skill.skillElement);

        return modifier_element_1 * modifier_element_2;
    }

    private static void HP_EN_DrainFunction(SkillsDataBase.SkillInfoPlayFab skill, int damageDone, NBMonBattleDataSave AttackerMonster)
    {
        int HPDrained = (int)Math.Floor((float)damageDone * skill.HPDrainInPercent / 100);
        int EnergyDrained = (int)Math.Floor((float)damageDone * skill.EnergyDrainInPercent / 100);

        if (skill.HPDrainInPercent != 0 && skill.EnergyDrainInPercent == 0)
        {
            if (HPDrained == 0)
                HPDrained = 1;
        }

        if (skill.HPDrainInPercent == 0 && skill.EnergyDrainInPercent != 0)
        {
            if (EnergyDrained == 0)
                EnergyDrained = 1;
        }

        if (skill.HPDrainInPercent != 0 && skill.EnergyDrainInPercent != 0)
        {
            if (HPDrained == 0)
                HPDrained = 1;

            if (EnergyDrained == 0)
                EnergyDrained = 1;

            //HP Drain
            NBMonTeamData.StatsValueChange(AttackerMonster ,NBMonProperties.StatsType.Hp, HPDrained);

            //Energy Drain
            NBMonTeamData.StatsValueChange(AttackerMonster ,NBMonProperties.StatsType.Energy, EnergyDrained);
        }
    }
}