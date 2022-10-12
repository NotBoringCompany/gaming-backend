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
using Microsoft.Azure.Documents.Client;

public static class AttackFunction
{
    //Static Variables
    public static bool VS_NPC = new bool();
    public static bool VS_Boss = new bool(); 
    public static int MaxLevel = 40;
    public static string BattleEnvironment = string.Empty;

    //Cloud Function
    [FunctionName("AttackLogic")]
    public static async Task<dynamic> AttackLogic([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
    ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "Team1UniqueID_BF", "BattleEnvironment"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave AttackerMonster = new NBMonBattleDataSave();
        List<NBMonBattleDataSave> DefenderMonsters = new List<NBMonBattleDataSave>();
        DataFromUnity UnityData = new DataFromUnity();
        DataSendToUnity DataFromAzureToClient = new DataSendToUnity();
        List<string> Team1UniqueID_BF = new List<string>();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        Team1UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team1UniqueID_BF"].Value);
        
        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Insert Data to Static Variable for (Passive Purpose)
        NBMonTeamData.PlayerTeam = PlayerTeam;
        NBMonTeamData.EnemyTeam = EnemyTeam;

        //Get Data From Unity and Convert it back to Class.
        if(args["UnityDataInput"] != null)
        {
            string ArgumentString = args["UnityDataInput"];

            UnityData = JsonConvert.DeserializeObject<DataFromUnity>(ArgumentString);
        }

        //Get Variable if it's an NPC Battle
        if(args["IsNPCBattle"] != null)
        {
            VS_NPC = (bool)args["IsNPCBattle"];
        }

        //Get Variable if it's an NPC Battle
        if(args["IsBossBattle"] != null)
        {
            VS_Boss = (bool)args["IsBossBattle"];
        }

        //Get Attacker Data from Unity Input
        AttackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, PlayerTeam);
        
        //If Attacker not found in Player Team, Find Attacker on Enemy Team
        if(AttackerMonster == null)
            AttackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, EnemyTeam);

        //Insert Attacker Monster Unique ID.
        DataFromAzureToClient.AttackerMonsterUniqueID = AttackerMonster.uniqueId;

        //Declare Skill Slot
        var SkillSlot = UnityData.SkillSlot;

        //Check Skill Slot Value, prevent using Value that is not 0 ~ 3
        if(SkillSlot < 0)
            SkillSlot = 0;

        if(SkillSlot > AttackerMonster.skillList.Count - 1)
            SkillSlot = AttackerMonster.skillList.Count - 1;

        //Get all Defender Data from Unity Input
        foreach(string TargetID in UnityData.TargetUniqueIDs){
            var Monster = UseItem.FindMonster(TargetID, PlayerTeam);

            if(Monster == null)
            {
                //If Target Monster not found, find this from Enemy Team 
                Monster = UseItem.FindMonster(TargetID, EnemyTeam);
            }

            //Add Monster into DefenderMonster List
            DefenderMonsters.Add(Monster);
        }

        log.LogInformation($"Code A: 1st Step, Get Attacker Skill Data");

        //Let's get Attacker Data like Skill
        SkillsDataBase.SkillInfoPlayFab AttackerSkillData = SkillsDataBase.FindSkill(AttackerMonster.skillList[SkillSlot]);

        //Deduct Attacker Monster Energy
        NBMonTeamData.StatsValueChange(AttackerMonster, NBMonProperties.StatsType.Energy, -AttackerSkillData.energyRequired);

        log.LogInformation($"Code B: 2nd Step, Apply Passive for Attacker");

        //Let's Apply Passive to Attacker Monster before Attacking (ActionBefore)
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionBefore, PassiveDatabase.TargetType.originalMonster, AttackerMonster, null, AttackerSkillData);

        log.LogInformation($"Code C: 3rd Step, Looping for each Monster Target");

        //After Applying passive, let's call Apply Skill for each Target Monster
        foreach(var TargetMonster in DefenderMonsters)
        {
            if(TargetMonster.fainted || TargetMonster.hp <= 0)
                continue;

            log.LogInformation($"{TargetMonster.nickName}, Apply Skill");

            //Apply Skill
            ApplySkill(AttackerSkillData, AttackerMonster, TargetMonster, DataFromAzureToClient, log);

            log.LogInformation($"{TargetMonster.nickName}, Apply Status Effect Immediately");

            //Apply status effect logic like Anti Poison/Anti Stun.
            StatusEffectIconDatabase.ApplyStatusEffectImmediately(TargetMonster);

            log.LogInformation($"{TargetMonster.nickName}, Check Monster Died");

            //Let's Check Target Monster wether it's Fainted or not
            CheckTargetDied(TargetMonster, AttackerMonster, AttackerSkillData, PlayerTeam, EnemyTeam, Team1UniqueID_BF, DataFromAzureToClient);
        }

        log.LogInformation($"Code D: 4th Step, Apply and Remove Status Effect from Attacker Monster");

        //Apply Status Effect to Attacker Monster
        UseItem.ApplyStatusEffect(AttackerMonster, AttackerSkillData.statusEffectListSelf, null, false);

        //Remove Status Effect to Attacker Monster
        UseItem.RemoveStatusEffect(AttackerMonster, AttackerSkillData.removeStatusEffectListSelf);

        log.LogInformation($"Code E: 5th Step, Reset Combat Related Stats");

        //After the Combat, let's resets the Monster's Temporary Stats that only works in Combat Phase
        ResetTemporaryStatsAfterAttacking(AttackerMonster);

        foreach(var TargetMonster in DefenderMonsters)
            ResetTemporaryStatsAfterAttacking(TargetMonster);

        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );

        //Lets turn DataFromAzureToClient into JsonString.
        string DataInJsonString = JsonConvert.SerializeObject(DataFromAzureToClient);

        log.LogInformation($"{DataInJsonString}");

        return DataFromAzureToClient;
    }

    //Data Send to Client
    public class DamageData
    {
        public string DefenderMonsterUniqueID; //Defender Monster Unique ID
        public int Damage; //Normal Damage
        public int SPDamage; //Special Damage
        public int EnergyDamage; //Energy Damage
        public int EnergySPDamage; //Energy Damage from Special Attack
        public bool IsCritical; //Determine if the Damage is Critical Hit.
        public bool DamageImmune; //Determine if the Monster is Immune to the Damage.
        public float ElementalVariable; //Determine if the Monster is Very Effective or Not.
        public int TotalHPRecovered;
        public int TotalEnergyRecovered;

        //HP and Energy Drain to Attack Monster
        public int HPDrained;
        public int EnergyDrained;
    }

    public class MonsterObtainedEXP
    {
        public string MonsterUniqueID;
        public int MonsterGetEXP;
    }

    //Data We gonna send to Client
    public class DataSendToUnity
    {
        public string AttackerMonsterUniqueID;
        public List<DamageData> DamageDatas = new List<DamageData>();
        public List<MonsterObtainedEXP> EXPDatas = new List<MonsterObtainedEXP>();
    }

    //Data Send From Client to Azure
    public class DataFromUnity
    {
        public string AttackerMonsterUniqueID;
        public int SkillSlot;
        public List<string> TargetUniqueIDs;
    }

    //Apply Skill Function
    public static void ApplySkill(SkillsDataBase.SkillInfoPlayFab Skill, NBMonBattleDataSave AttackerMonster, NBMonBattleDataSave DefenderMonster, DataSendToUnity dataFromAzureToClient, ILogger log)
    {
        //Declare New DamageData
        DamageData ThisMonsterDamageData = new DamageData();

        log.LogInformation("Apply Passive Logic: Action Receiving");

        //Apply passive related with receiving element input before get hit!
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionReceiving, PassiveDatabase.TargetType.targetedMonster, AttackerMonster, DefenderMonster, Skill);

        //Insert Defender Monster Unique ID.
        ThisMonsterDamageData.DefenderMonsterUniqueID = DefenderMonster.uniqueId;
        
        //When skill type is damage, calculate the damage based on element modifier
        if(Skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            log.LogInformation("Calculate and Do Damage");
            
            CalculateAndDoDamage(Skill, AttackerMonster, DefenderMonster, ThisMonsterDamageData);
        }

        log.LogInformation("Apply Passive Logic: Action After");

        //Apply passive effect related to after action to the original monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionAfter, PassiveDatabase.TargetType.originalMonster, AttackerMonster, DefenderMonster, Skill);

        //Apply passive effect that inflict status effect from this monster to attacker monster (aka original monster), make sure the attacker is not itself and the skill's type is damage
        if (AttackerMonster != DefenderMonster && Skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            log.LogInformation("Apply Passive Logic: Inflict Status Effect");

            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.InflictStatusEffect, PassiveDatabase.TargetType.originalMonster, DefenderMonster, AttackerMonster, Skill);
        }

        //If the skill is Healing Skill, e.g HP and Energy Recovery
        if(Skill.actionType == SkillsDataBase.ActionType.StatsRecovery)
        {
            log.LogInformation("Status Recovery Logic");
            StatsRecoveryLogic(Skill, DefenderMonster, ThisMonsterDamageData);
        }

        log.LogInformation("Apply Status and Remove Status Effects");

        //Apply Status Effect to Target
        UseItem.ApplyStatusEffect(DefenderMonster, Skill.statusEffectList, null, false);

        //Remove Status Effect to Target
        UseItem.RemoveStatusEffect(DefenderMonster, Skill.removeStatusEffectList);

        log.LogInformation("Modify ThisMonsterDamageData");

        //Add ThisMonsterDamageData to DataFromAzureToClient
        dataFromAzureToClient.DamageDatas.Add(ThisMonsterDamageData);
    }

    //Stats Recovery Logic
    private static void StatsRecoveryLogic(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave ThisMonster, DamageData thisMonsterDamageData)
    {
        var HPRecoveryInPercent = (int)Math.Floor(ThisMonster.maxHp * skill.hpPercent / 100);
        var EnergyRecoveryInPercent = (int)Math.Floor(ThisMonster.maxEnergy * skill.energyPercent / 100);

        int TotalHPRecovery = HPRecoveryInPercent + skill.hp;
        int TotalEnergyRecovery = EnergyRecoveryInPercent + skill.energy;

        //HP and Energy Recovery
        NBMonTeamData.StatsValueChange(ThisMonster, NBMonProperties.StatsType.Hp, skill.hp);
        NBMonTeamData.StatsValueChange(ThisMonster, NBMonProperties.StatsType.Energy, skill.energy);

        //HP and Energy Recovery but in Percentage
        NBMonTeamData.StatsPercentageChange(ThisMonster, NBMonProperties.StatsType.Hp, HPRecoveryInPercent);
        NBMonTeamData.StatsPercentageChange(ThisMonster, NBMonProperties.StatsType.Energy, EnergyRecoveryInPercent);

        //Add HP and Energy Recovery into This Monster Damage Data
        thisMonsterDamageData.TotalHPRecovered = TotalHPRecovery;
        thisMonsterDamageData.TotalEnergyRecovered = TotalEnergyRecovery;
    }

    //Damage Function
    //Calculate and do the damage to the monster
    public static void CalculateAndDoDamage(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave AttackerMonster, NBMonBattleDataSave DefenderMonster, DamageData thisMonsterDamageData)
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

                //Damage becomes 0 if This_NBMon_Damage_Reduction_Modifier = 1
                if(This_NBMon_Damage_Reduction_Modifier == 1)
                    damageAttack = 0;

                //Normal Damage
                NBMonTeamData.StatsValueChange(DefenderMonster ,NBMonProperties.StatsType.Hp, damageAttack * -1);

                //Energy Damage
                if (This_NBMon_EnergyShield_IsActive >= 1)
                {
                    NBMonTeamData.StatsValueChange(DefenderMonster ,NBMonProperties.StatsType.Energy, energyDamageAttack * -1);
                }

                //HP and Energy Drain Logic -> Only for Skill with TechniqueType of Attack
                HP_EN_DrainFunction(skill, damageAttack, AttackerMonster, thisMonsterDamageData);
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

            //Damage becomes 0 if This_NBMon_Damage_Reduction_Modifier = 1
            if(This_NBMon_Damage_Reduction_Modifier == 1)
                damageSpAttack = 0;

            //Normal SP Damage
            NBMonTeamData.StatsValueChange(DefenderMonster, NBMonProperties.StatsType.Hp, damageSpAttack * -1);

            if (This_NBMon_EnergyShield_IsActive >= 1)
            {
                //Energy Damage
                NBMonTeamData.StatsValueChange(DefenderMonster, NBMonProperties.StatsType.Energy, energyDamageSPAttack * -1);
            }
        }

        //Let's get all necessary data from Calculate And Do Damage into This Monster Damage Data.
        thisMonsterDamageData.Damage = damageAttack;
        thisMonsterDamageData.SPDamage = damageSpAttack;
        thisMonsterDamageData.EnergyDamage = energyDamageAttack;
        thisMonsterDamageData.EnergySPDamage = energyDamageSPAttack;
        thisMonsterDamageData.IsCritical = (CriticalHitMultiplier > 1); //If Critical Hit Multiplier is higher than 1, it's Critical Hit.
        thisMonsterDamageData.DamageImmune = (ElementModifier == 0); //This Monster will deal 0 Damage if Element Modifier = 0.
        thisMonsterDamageData.ElementalVariable = ElementModifier;
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

    private static void HP_EN_DrainFunction(SkillsDataBase.SkillInfoPlayFab skill, int damageDone, NBMonBattleDataSave AttackerMonster, DamageData thisMonsterDamageData)
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

        //Insert HP and Energy Drain into This Monster Damage Data
        thisMonsterDamageData.HPDrained = HPDrained;
        thisMonsterDamageData.EnergyDrained = EnergyDrained;
    }

    //Monster Target Defeated
    public static void CheckTargetDied(NBMonBattleDataSave TargetMonster, NBMonBattleDataSave AttackerMonster, SkillsDataBase.SkillInfoPlayFab AttackerSkillData, List<NBMonBattleDataSave> PlayerTeam, List<NBMonBattleDataSave> EnemyTeam, List<string> Team1UniqueID_BF, DataSendToUnity dataFromAzureToClient)
    {
        //Let's Check if the TargetMonster's HP reached 0.
        if(TargetMonster.hp <= 0)
        {
            //TargetMonster becomes Fainted
            TargetMonster.fainted = true;

            //Apply EXP to Every Single Monster (For Player Team Only), Check if the Attacker Monster is from Player 1 and the Killed Monster is not from Player 1
            if(Team1UniqueID_BF.Contains(AttackerMonster.uniqueId) && !Team1UniqueID_BF.Contains(TargetMonster.uniqueId))
            {
                foreach(var Player1Monster in PlayerTeam)
                {
                    if(Team1UniqueID_BF.Contains(Player1Monster.uniqueId))
                    {
                        AddEXP(TargetMonster, Player1Monster, dataFromAzureToClient);
                    }
                }
            }
        }
    }

    //Add EXP Function
    public static void AddEXP(NBMonBattleDataSave TargetMonster, NBMonBattleDataSave AttackerMonster, DataSendToUnity dataFromAzureToClient)
    {
        MonsterObtainedEXP ThisMonsterEXPData = new MonsterObtainedEXP();

        //Level of Human Player's NBMon
        var ThisMonsterLevel = AttackerMonster.level;

        //Level of Defeated NBMon
        var DefeatedMonsterLevel = TargetMonster.level;

        //Get Target Monster's Base EXP from Monster Database
        float DefeatedMonsterBaseEXP = NBMonDatabase.FindMonster(TargetMonster.monsterId).baseEXP;

        //EXP Multiplier Depending On Team's Type (Wild, NPC, or Boss)
        var Variable_A = 1f;

        if(VS_Boss)
        {
            Variable_A = 5f;
        }

        if(VS_NPC)
        {
            Variable_A = 1.5f;
        }

        //EXPMultiplier Logic Based on Level
        var EXPMultiplierByLevelDifferences = EXPMultiplierEquation(ThisMonsterLevel, DefeatedMonsterLevel);

        //Step by Step Calculation
        float Func_1 = (4f * (float)Math.Pow((float)DefeatedMonsterLevel, 3f) / 8f);
        float Func_2 = (2f * (float)DefeatedMonsterLevel + 10f);
        float Func_3 = (float)DefeatedMonsterLevel + (float)ThisMonsterLevel + 10f;
        float Function = (float)(Math.Pow(Func_2/Func_3, 2.5f) / 2.3f);

        //EXP Equation
        int EXPEquation = (int)Math.Floor(
            EXPMultiplierByLevelDifferences *
            Func_1 *
            Variable_A *
            DefeatedMonsterBaseEXP * Function);

        //Do Not Get Any EXP if defeating same team or Reached Max Level.
        if (ThisMonsterLevel >= MaxLevel)
            EXPEquation = 0;

        //Add EXP to Monster (EXP Memory Storage)
        AttackerMonster.expMemoryStorage += EXPEquation;

        //Insert Necessary Data into ThisMonsterEXPData
        ThisMonsterEXPData.MonsterUniqueID = AttackerMonster.uniqueId;
        ThisMonsterEXPData.MonsterGetEXP = EXPEquation;

        //Add ThisMonsterEXPData into dataFromAzureToClient.
        dataFromAzureToClient.EXPDatas.Add(ThisMonsterEXPData);
    }

    //Related to EXPMultiplier variable in AddToEXPList function
    private static float EXPMultiplierEquation(int ThisMonsterLevel, int monsterDiedLevel)
    {
        if (ThisMonsterLevel <= monsterDiedLevel + 10 && ThisMonsterLevel >= monsterDiedLevel - 10)
            return 1f;

        return 1f;
    }

    //Function to Resets Temporary Stats
    public static void ResetTemporaryStatsAfterAttacking(NBMonBattleDataSave Monster)
    {
        //TemporaryStats
        Monster.attackBuff = 0;
        Monster.specialAttackBuff = 0;
        Monster.defenseBuff = 0;
        Monster.specialDefenseBuff = 0;
        Monster.criticalBuff = 0;
        Monster.ignoreDefenses = 0;
        Monster.damageReduction = 0;
        Monster.energyShieldValue = 0;

        //Other Parameters
        Monster.energyShield = 0;
        Monster.mustCritical = 0;
        Monster.surviveLethalBlow = 0;
        Monster.totalIgnoreDefense = 0;
        Monster.immuneCritical = 0;
        Monster.elementDamageReduction = 0;
    }
}