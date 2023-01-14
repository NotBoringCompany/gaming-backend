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
using Azure;
using static InitialTeamSetup;

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
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "Team1UniqueID_BF", "BattleEnvironment", "RNGSeeds", "SortedOrder", "MoraleGaugeData", "HumanBattleData"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        HumanBattleData humanBattleData = new HumanBattleData();
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave AttackerMonster = new NBMonBattleDataSave();
        List<NBMonBattleDataSave> DefenderMonsters = new List<NBMonBattleDataSave>();
        DataFromUnity UnityData = new DataFromUnity();
        DataSendToUnity DataFromAzureToClient = new DataSendToUnity();
        List<string> Team1UniqueID_BF = new List<string>();
        List<String> SortedOrder = new List<string>();
        RNGSeedClass seedClass = new RNGSeedClass();
        BattleMoraleGauge.MoraleData moraleData = new BattleMoraleGauge.MoraleData();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        Team1UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team1UniqueID_BF"].Value);
        SortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);
        seedClass = JsonConvert.DeserializeObject<RNGSeedClass>(requestTeamInformation.Result.Data["RNGSeeds"].Value);
        moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);
        humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);
        
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

        //Check if monster can attack
        var monsterCanMove = EvaluateOrder.CheckBattleOrder(SortedOrder, UnityData.AttackerMonsterUniqueID);

        if(!monsterCanMove)
        {
            return $"No Monster in the turn order. Error Code: RH-0001";
        }

        //Get Attacker Data from Unity Input
        AttackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, PlayerTeam, humanBattleData);
        
        //If Attacker not found in Player Team, Find Attacker on Enemy Team
        if(AttackerMonster == null)
            AttackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, EnemyTeam, humanBattleData);

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
            var Monster = UseItem.FindMonster(TargetID, PlayerTeam, humanBattleData);

            if(Monster == null)
            {
                //If Target Monster not found, find this from Enemy Team 
                Monster = UseItem.FindMonster(TargetID, EnemyTeam, humanBattleData);
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
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionBefore, PassiveDatabase.TargetType.originalMonster, AttackerMonster, null, AttackerSkillData, seedClass);

        log.LogInformation($"Code C: 3rd Step, Looping for each Monster Target");

        //After Applying passive, let's call Apply Skill for each Target Monster
        foreach(var TargetMonster in DefenderMonsters)
        {
            if((TargetMonster.fainted || TargetMonster.hp <= 0) && DefenderMonsters.Count != 1)
                continue;

            log.LogInformation($"{TargetMonster.nickName}, Apply Skill");

            //Apply Skill
            ApplySkill(AttackerSkillData, AttackerMonster, TargetMonster, DataFromAzureToClient, seedClass, moraleData, PlayerTeam, EnemyTeam, humanBattleData);

            log.LogInformation($"{TargetMonster.nickName}, Apply Status Effect Immediately");

            //Apply status effect logic like Anti Poison/Anti Stun.
            StatusEffectIconDatabase.ApplyStatusEffectImmediately(TargetMonster);

            log.LogInformation($"{TargetMonster.nickName}, Check Monster Died");

            //Let's Check Target Monster wether it's Fainted or not
            CheckTargetDied(TargetMonster, AttackerMonster, AttackerSkillData, PlayerTeam, EnemyTeam, Team1UniqueID_BF, DataFromAzureToClient);
        }

        log.LogInformation($"Code D: 4th Step, Apply and Remove Status Effect from Attacker Monster");

        //Apply Status Effect to Attacker Monster
        UseItem.ApplyStatusEffect(AttackerMonster, AttackerSkillData.statusEffectListSelf, null, false, seedClass);

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
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)},
                 {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)},
                 {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)}
                }
            }
        );

        //Lets turn DataFromAzureToClient into JsonString.
        string DataInJsonString = JsonConvert.SerializeObject(DataFromAzureToClient);

        log.LogInformation($"{DataInJsonString}");

        return DataFromAzureToClient;
    }

    [FunctionName("SkipTurnLogic")]
    public static async Task<dynamic> SkipTurnLogic([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"SortedOrder"}
            }
        );

        //Declare Variable
        List<String> SortedOrder = new List<string>();
        string currMonsterUid = string.Empty;
        //Insert declared data
        SortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);
        if(args["currMonsterUid"] != null)
            currMonsterUid = args["currMonsterUid"];

        var monsterCanMove = EvaluateOrder.CheckBattleOrder(SortedOrder, currMonsterUid);
        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)}
                }
            }
        );

        return null;
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
    public static void ApplySkill(SkillsDataBase.SkillInfoPlayFab Skill, NBMonBattleDataSave AttackerMonster, NBMonBattleDataSave DefenderMonster, DataSendToUnity dataFromAzureToClient, RNGSeedClass seedClass, BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        //Declare New DamageData
        DamageData ThisMonsterDamageData = new DamageData();

        //Apply passive related with receiving element input before get hit!
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionReceiving, PassiveDatabase.TargetType.targetedMonster, AttackerMonster, DefenderMonster, Skill, seedClass);

        //Insert Defender Monster Unique ID.
        ThisMonsterDamageData.DefenderMonsterUniqueID = DefenderMonster.uniqueId;
        
        //When skill type is damage, calculate the damage based on element modifier
        if(Skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            CalculateAndDoDamage(Skill, AttackerMonster, DefenderMonster, ThisMonsterDamageData, seedClass, moraleData, playerTeam, enemyTeam, humanBattleData);
        }

        //Apply passive effect related to after action to the original monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ActionAfter, PassiveDatabase.TargetType.originalMonster, AttackerMonster, DefenderMonster, Skill, seedClass);

        //Apply passive effect that inflict status effect from this monster to attacker monster (aka original monster), make sure the attacker is not itself and the skill's type is damage
        if (AttackerMonster != DefenderMonster && Skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.InflictStatusEffect, PassiveDatabase.TargetType.originalMonster, DefenderMonster, AttackerMonster, Skill, seedClass);
        }

        //If the skill is Healing Skill, e.g HP and Energy Recovery
        if(Skill.actionType == SkillsDataBase.ActionType.StatsRecovery)
        {
            StatsRecoveryLogic(Skill, DefenderMonster, ThisMonsterDamageData);
        }

        //Apply Status Effect to Target
        UseItem.ApplyStatusEffect(DefenderMonster, Skill.statusEffectList, null, false, seedClass);

        //Remove Status Effect to Target
        UseItem.RemoveStatusEffect(DefenderMonster, Skill.removeStatusEffectList);

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
    public static void CalculateAndDoDamage(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave attackerMonster, NBMonBattleDataSave targetMonster, DamageData thisMonsterDamageData, RNGSeedClass seedClass, BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        //Declare Random Variable
        Random R = new Random();

        //Apply passives and artifact passives that only works during combat to Attacker
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.DuringCombat, PassiveDatabase.TargetType.originalMonster, attackerMonster, null, null, seedClass);
        //TO DO, Apply Artifact Passive to Attacker Monster.

        //Apply passives and artifact passives that only works during combat to This Monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.DuringCombat, PassiveDatabase.TargetType.originalMonster, targetMonster, null, null, seedClass);
        //TO DO, Apply Artifact Passive to Defender Monster.

        //Calculate Critical Hit RNG
        int attackerCriticalHitRate = CriticalHitStatsCalculation(attackerMonster, skill);
        int criticalRNG = EvaluateOrder.ConvertSeedToRNG(seedClass);
        float criticalHitMultiplier = 1f;
        int mustCritical = attackerMonster.mustCritical;

        //Check Target's Immune to Critical or Not
        int immuneToCritical = targetMonster.immuneCritical;

        if (criticalRNG <= attackerCriticalHitRate || mustCritical >= 1)
            criticalHitMultiplier = (float)EvaluateOrder.CriticalRNG(seedClass) * (3f - 1.5f) + 1.5f;
        else if (immuneToCritical >= 1)
            criticalHitMultiplier = 1f;

        //Calculate Attacker's Attack Buffs Modifier from Status Effects
        float attack_StatusEffect_Modifier = StatusEffectIconDatabase.AttackStatusEffectLogic(attackerMonster);
        float sp_Attack_StatusEffect_Modifier = StatusEffectIconDatabase.SPAttackStatusEffectLogic(attackerMonster);

        //Find Attacker's Attack Buffs from Passives
        float attack_Passive_Modifier = attackerMonster.attackBuff / 100f;
        float sp_Attack_Passive_Modifier = attackerMonster.specialAttackBuff / 100f;
        float ignoreDefenses = attackerMonster.ignoreDefenses / 100f;
        int totalIngoreDefense = attackerMonster.totalIgnoreDefense;

        //Normalizations
        if (ignoreDefenses > 1)
            ignoreDefenses = 1f;

        //Calculate This Monster's Defense Buffs Modifier from Status Effects
        float target_NBMon_Def_Modifier = StatusEffectIconDatabase.DefenseStatusEffectLogic(targetMonster);
        float target_NBMon_SP_Def_Modifier = StatusEffectIconDatabase.SPDefenseStatusEffectLogic(targetMonster);

        //Find This Monster's Defense Buffs Modifier from Passives
        float target_NBMon_Def_Passive_Modifier = targetMonster.defenseBuff / 100f;
        float target_NBMon_SP_Def_Passive_Modifier = targetMonster.specialDefenseBuff / 100f;
        float target_NBMon_Damage_Reduction_Modifier = targetMonster.damageReduction / 100f;
        float target_NBMon_Damage_Reduction_From_Energy_Shield = targetMonster.energyShieldValue / 100f;
        int target_NBMon_EnergyShield_IsActive = targetMonster.energyShield;
        int target_NBMon_Must_Survive_Lethal_Blow = targetMonster.surviveLethalBlow;
        float elementalDamageReduction = 1f - ((float)targetMonster.elementDamageReduction) / 100f;

        //Normalizations Damage Reduction
        if (target_NBMon_Damage_Reduction_Modifier >= 1f)
            target_NBMon_Damage_Reduction_Modifier = 1f;

        //Normalizations Damage Reduction from Energy Shield
        if (target_NBMon_Damage_Reduction_From_Energy_Shield > 1f)
            target_NBMon_Damage_Reduction_From_Energy_Shield = 1f;

        //Check if the attacker's Total Ignore Defense value is 1 or more (this integer act as Bool)
        if (totalIngoreDefense >= 1)
        {
            //This NBMon's Damage Reduction set to 0f.
            target_NBMon_Damage_Reduction_Modifier = 0f;

            //Attacker's Ignore Defenses set to 1f (aka 100%).
            ignoreDefenses = 1f;
        }

        //Check if this NBMon's energy shield is active or not (indicator: more than or same as 1 = true, less than 1 = false)
        if (target_NBMon_EnergyShield_IsActive < 1)
            target_NBMon_Damage_Reduction_From_Energy_Shield = 0f;

        //Calculate this NBMon's Def
        float targetMonsterDef = (float)targetMonster.defense * target_NBMon_Def_Modifier * (1f + target_NBMon_Def_Passive_Modifier) * (1f - ignoreDefenses) * elementalDamageReduction;
        float targetMonsterSPDef = (float)targetMonster.specialDefense * target_NBMon_SP_Def_Modifier * (1f + target_NBMon_SP_Def_Passive_Modifier) * (1f - ignoreDefenses) * elementalDamageReduction;

        //Normalization for Defense
        if (targetMonsterDef == 0)
            targetMonsterDef = 1;

        if (targetMonsterSPDef == 0)
            targetMonsterSPDef = 1;

        //Calculate the modifier value
        float elementModifier = CalculateElementModifier(skill, targetMonster);
        float sameTypeAttackBoost = SameTypeAttackBonusCalculation(skill, attackerMonster);
        float randomDamage = (float)EvaluateOrder.ConvertSeedToRNG(seedClass, 850, 1000) / 1000f;

        //Attack Logic
        float attackerAttackStats = attackerMonster.attack * (1f + attack_Passive_Modifier) * attack_StatusEffect_Modifier;
        int attackDamage = DamageCalculation(skill.attack, attackerMonster.level, attackerAttackStats, targetMonsterDef, sameTypeAttackBoost, elementModifier, (1f + attack_Passive_Modifier), criticalHitMultiplier, attack_StatusEffect_Modifier, randomDamage);
        int damageAttack = (int)Math.Floor(attackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (1f - target_NBMon_Damage_Reduction_From_Energy_Shield));
        int energyDamageAttack = (int)Math.Floor(attackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (target_NBMon_Damage_Reduction_From_Energy_Shield));

        //Special Attakc Logic
        float attackerSPAttackStats = attackerMonster.specialAttack * (1f + sp_Attack_Passive_Modifier) * sp_Attack_StatusEffect_Modifier;
        int spAttackDamage = DamageCalculation(skill.specialAttack, attackerMonster.level, attackerSPAttackStats, targetMonsterSPDef, sameTypeAttackBoost, elementModifier, (1f + sp_Attack_Passive_Modifier), criticalHitMultiplier, sp_Attack_StatusEffect_Modifier, randomDamage);
        int damageSpAttack = (int)Math.Floor(spAttackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (1f - target_NBMon_Damage_Reduction_From_Energy_Shield));
        int energyDamageSPAttack = (int)Math.Floor(spAttackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (target_NBMon_Damage_Reduction_From_Energy_Shield));

        //Survive Lethal Blow Logic
        if (target_NBMon_Must_Survive_Lethal_Blow >= 1)
        {
            int targetMonsterHP = targetMonster.hp;

            if (targetMonsterHP > 1 && damageAttack > targetMonsterHP)
                damageAttack = targetMonsterHP - 1;

            if (targetMonsterHP > 1 && damageSpAttack > targetMonsterHP)
                damageSpAttack = targetMonsterHP - 1;
        }

        //Changes morale Data for attacker (increase gauge by 5)
        ChangeMoraleGauge(moraleData, playerTeam, enemyTeam, attackerMonster, 5, humanBattleData);

        // Reduce target NBMon's HP
        DamageLogic(skill, attackerMonster, targetMonster, thisMonsterDamageData, moraleData, playerTeam, enemyTeam, humanBattleData, criticalHitMultiplier, target_NBMon_Damage_Reduction_Modifier, target_NBMon_Damage_Reduction_From_Energy_Shield, target_NBMon_EnergyShield_IsActive, elementModifier, ref damageAttack, energyDamageAttack, ref damageSpAttack, energyDamageSPAttack);
    }

    private static void DamageLogic(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave attackerMonster, NBMonBattleDataSave targetMonster, DamageData thisMonsterDamageData, BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData, float criticalHitMultiplier, float target_NBMon_Damage_Reduction_Modifier, float target_NBMon_Damage_Reduction_From_Energy_Shield, int target_NBMon_EnergyShield_IsActive, float elementModifier, ref int damageAttack, int energyDamageAttack, ref int damageSpAttack, int energyDamageSPAttack)
    {
        if (skill.techniqueType == SkillsDataBase.TechniqueType.Attack)
        {
            if (elementModifier != 0)
            {
                //Damage becomes 0 if This_NBMon_Damage_Reduction_Modifier = 1
                if (target_NBMon_Damage_Reduction_Modifier == 1)
                    damageAttack = 0;

                //Normal Damage
                NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Hp, damageAttack * -1);
                ChangeMoraleGauge(moraleData, playerTeam, enemyTeam, targetMonster, damageAttack, humanBattleData);

                //Energy Damage
                if (target_NBMon_EnergyShield_IsActive >= 1)
                {
                    NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Energy, energyDamageAttack * -1);
                }

                //HP and Energy Drain Logic -> Only for Skill with TechniqueType of Attack
                HP_EN_DrainFunction(skill, damageAttack, attackerMonster, thisMonsterDamageData);
            }
            else
            {
                //By Default, damageAttack value is 0.
            }
        }

        if (skill.techniqueType == SkillsDataBase.TechniqueType.SpecialAttack)
        {
            //Damage becomes 0 if This_NBMon_Damage_Reduction_Modifier = 1
            if (target_NBMon_Damage_Reduction_Modifier == 1)
                damageSpAttack = 0;

            //Normal SP Damage
            NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Hp, damageSpAttack * -1);
            ChangeMoraleGauge(moraleData, playerTeam, enemyTeam, targetMonster, damageSpAttack, humanBattleData);

            if (target_NBMon_EnergyShield_IsActive >= 1)
            {
                //Energy Damage
                NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Energy, energyDamageSPAttack * -1);
            }
        }

        //Let's get all necessary data from Calculate And Do Damage into This Monster Damage Data.
        RecordDamageData(thisMonsterDamageData, criticalHitMultiplier, elementModifier, damageAttack, energyDamageAttack, damageSpAttack, energyDamageSPAttack);
    }

    private static void RecordDamageData(DamageData thisMonsterDamageData, float criticalHitMultiplier, float elementModifier, int damageAttack, int energyDamageAttack, int damageSpAttack, int energyDamageSPAttack)
    {
        thisMonsterDamageData.Damage = damageAttack;
        thisMonsterDamageData.SPDamage = damageSpAttack;
        thisMonsterDamageData.EnergyDamage = energyDamageAttack;
        thisMonsterDamageData.EnergySPDamage = energyDamageSPAttack;
        thisMonsterDamageData.IsCritical = (criticalHitMultiplier > 1); //If Critical Hit Multiplier is higher than 1, it's Critical Hit.
        thisMonsterDamageData.DamageImmune = (elementModifier == 0); //This Monster will deal 0 Damage if Element Modifier = 0.
        thisMonsterDamageData.ElementalVariable = elementModifier;
    }

    //Damage Calculation Method
    private static int DamageCalculation(int skillPower, int attackerLevel, float attackerPower, float targetDef, float STAB, float typeEffects, float passiveDamageBoost, float criticalDamageMultiplier, float statusEffectModifier, float randomValue)
    {
        float func_A = ((attackerLevel * skillPower * attackerPower * statusEffectModifier * passiveDamageBoost / targetDef) / 100) + 10;
        int overallFunc = (int)(func_A * STAB * typeEffects * criticalDamageMultiplier * randomValue);
        return overallFunc;
    }

    // Calculate and return the element modifier value
    private static float SameTypeAttackBonusCalculation(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave attackerMonster)
    {
        var monsterData = NBMonDatabase.FindMonster(attackerMonster.monsterId);
        
        if(monsterData.elements.Contains(skill.skillElement))
            return 1.5f;
        else
            return 1f;
    }

    private static int CriticalHitStatsCalculation(NBMonBattleDataSave originalMonster, SkillsDataBase.SkillInfoPlayFab skill)
    {
        int criticalHit_Skill = skill.criticalRate;
        int criticalHit_Passive = (int)Math.Floor(originalMonster.criticalBuff);
        int criticalHit_NBMon = originalMonster.criticalHit;
        int criticalHit_NBMon_From_StatusEffect = StatusEffectIconDatabase.CriticalStatusEffectLogic(originalMonster);

        return criticalHit_NBMon + criticalHit_NBMon_From_StatusEffect + criticalHit_Skill + criticalHit_Passive;

    }

    // Calculate and return the element modifier value
    private static float CalculateElementModifier(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave targetMonster)
    {
        var monsterInfoFromDatabase = NBMonDatabase.FindMonster(targetMonster.monsterId);

        float modifier_element_1 = ElementDatabase.FindElementValueModifier_Prototype(monsterInfoFromDatabase.elements[0], skill.skillElement);
        float modifier_element_2 = ElementDatabase.FindElementValueModifier_Prototype(monsterInfoFromDatabase.elements[1], skill.skillElement);

        return modifier_element_1 * modifier_element_2;
    }

    private static void HP_EN_DrainFunction(SkillsDataBase.SkillInfoPlayFab skill, int damageDone, NBMonBattleDataSave AttackerMonster, DamageData thisMonsterDamageData)
    {
        int hp_Drained = (int)Math.Floor((float)damageDone * skill.HPDrainInPercent / 100);
        int energyDrained = (int)Math.Floor((float)damageDone * skill.EnergyDrainInPercent / 100);

        if (skill.HPDrainInPercent != 0 && skill.EnergyDrainInPercent == 0)
        {
            if (hp_Drained == 0)
                hp_Drained = 1;
        }

        if (skill.HPDrainInPercent == 0 && skill.EnergyDrainInPercent != 0)
        {
            if (energyDrained == 0)
                energyDrained = 1;
        }

        if (skill.HPDrainInPercent != 0 && skill.EnergyDrainInPercent != 0)
        {
            if (hp_Drained == 0)
                hp_Drained = 1;

            if (energyDrained == 0)
                energyDrained = 1;

            //HP Drain
            NBMonTeamData.StatsValueChange(AttackerMonster ,NBMonProperties.StatsType.Hp, hp_Drained);

            //Energy Drain
            NBMonTeamData.StatsValueChange(AttackerMonster ,NBMonProperties.StatsType.Energy, energyDrained);
        }

        //Insert HP and Energy Drain into This Monster Damage Data
        thisMonsterDamageData.HPDrained = hp_Drained;
        thisMonsterDamageData.EnergyDrained = energyDrained;
    }

    public static void ChangeMoraleGauge(BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, NBMonBattleDataSave monster, int damageTaken, HumanBattleData humanBattleData)
    {
        //Player Logic
        if(playerTeam.Contains(monster))
        {
            BattleMoraleGauge.IncreasePlayerMorale(moraleData, damageTaken);
        }

        if(humanBattleData.playerHumanData != null)
            if(monster == humanBattleData.playerHumanData)
            {
                BattleMoraleGauge.IncreasePlayerMorale(moraleData, damageTaken);
            }

        //Enemy Logic
        if(enemyTeam.Contains(monster))
        {
            BattleMoraleGauge.IncreaseEnemyMorale(moraleData, damageTaken);
        }

        if(humanBattleData.enemyHumanData != null)
            if(monster == humanBattleData.enemyHumanData)
            {
                BattleMoraleGauge.IncreaseEnemyMorale(moraleData, damageTaken);
            }

    }

    //Monster Target Defeated
    public static void CheckTargetDied(NBMonBattleDataSave targetMonster, NBMonBattleDataSave attackerMonster, SkillsDataBase.SkillInfoPlayFab attackerSkillData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, List<string> team1UniqueID_BF, DataSendToUnity dataFromAzureToClient)
    {
        //Let's Check if the TargetMonster's HP reached 0.
        if(targetMonster.hp <= 0)
        {
            //TargetMonster becomes Fainted
            targetMonster.fainted = true;

            //Apply EXP to Every Single Monster (For Player Team Only), Check if the Attacker Monster is from Player 1 (or human) and the Killed Monster is not from Player 1
            if((team1UniqueID_BF.Contains(attackerMonster.uniqueId) || attackerMonster.monsterId == "Human") && !team1UniqueID_BF.Contains(targetMonster.uniqueId))
            {
                foreach(var Player1Monster in playerTeam)
                {
                    if(team1UniqueID_BF.Contains(Player1Monster.uniqueId))
                    {
                        AddEXP(targetMonster, Player1Monster, dataFromAzureToClient, 1);
                    }
                }
            }
        }
    }

    //Add EXP Function
    public static void AddEXP(NBMonBattleDataSave targetMonster, NBMonBattleDataSave attackerMonster, DataSendToUnity dataFromAzureToClient, int expDivider)
    {
        MonsterObtainedEXP thisMonsterEXPData = new MonsterObtainedEXP();

        //Level of Human Player's NBMon
        var thisMonsterLevel = attackerMonster.level;

        //Level of Defeated NBMon
        var defeatedMonsterLevel = targetMonster.level;

        //Get Target Monster's Base EXP from Monster Database
        float defeatedMonsterBaseEXP = NBMonDatabase.FindMonster(targetMonster.monsterId).baseEXP;

        Random r = new Random();

        var demoEXP = r.Next(100,250);

        //Add EXP to Monster (EXP Memory Storage)
        attackerMonster.expMemoryStorage += demoEXP;

        //Insert Necessary Data into ThisMonsterEXPData
        thisMonsterEXPData.MonsterUniqueID = attackerMonster.uniqueId;
        thisMonsterEXPData.MonsterGetEXP = demoEXP;

        //Add ThisMonsterEXPData into dataFromAzureToClient.
        if(dataFromAzureToClient != null)
            dataFromAzureToClient.EXPDatas.Add(thisMonsterEXPData);
        return; //For Demo Purpose

        //EXP Multiplier Depending On Team's Type (Wild, NPC, or Boss)
        // var var_A = 1f;

        // if(VS_Boss)
        // {
        //     var_A = 5f;
        // }

        // if(VS_NPC)
        // {
        //     var_A = 1.5f;
        // }

        // //EXPMultiplier Logic Based on Level
        // var xp_MultiplierByLevelDifferences = EXPMultiplierEquation(thisMonsterLevel, defeatedMonsterLevel);

        // //Step by Step Calculation
        // float func_1 = (4f * (float)Math.Pow((float)defeatedMonsterLevel, 3f) / 8f);
        // float func_2 = (2f * (float)defeatedMonsterLevel + 10f);
        // float func_3 = (float)defeatedMonsterLevel + (float)thisMonsterLevel + 10f;
        // float overall_Function = (float)(Math.Pow(func_2/func_3, 2.5f) / 2.3f);

        // //EXP Equation
        // int xp_Equation = (int)Math.Floor(
        //     xp_MultiplierByLevelDifferences *
        //     func_1 *
        //     var_A *
        //     defeatedMonsterBaseEXP * overall_Function);

        // //Do Not Get Any EXP if defeating same team or Reached Max Level.
        // if (thisMonsterLevel >= MaxLevel)
        //     xp_Equation = 0;

        // var expGained = xp_Equation/expDivider;

        // //Add EXP to Monster (EXP Memory Storage)
        // attackerMonster.expMemoryStorage += expGained;

        // //Insert Necessary Data into ThisMonsterEXPData
        // thisMonsterEXPData.MonsterUniqueID = attackerMonster.uniqueId;
        // thisMonsterEXPData.MonsterGetEXP = expGained;

        // //Add ThisMonsterEXPData into dataFromAzureToClient.
        // if(dataFromAzureToClient != null)
        //     dataFromAzureToClient.EXPDatas.Add(thisMonsterEXPData);
    }

    //Related to EXPMultiplier variable in AddToEXPList function
    private static float EXPMultiplierEquation(int monsterLevel, int monsterDiedLevel)
    {
        if (monsterLevel <= monsterDiedLevel + 10 && monsterLevel >= monsterDiedLevel - 10)
            return 1f;

        return 1f;
    }

    //Function to Resets Temporary Stats
    public static void ResetTemporaryStatsAfterAttacking(NBMonBattleDataSave monster)
    {
        //TemporaryStats
        monster.attackBuff = 0;
        monster.specialAttackBuff = 0;
        monster.defenseBuff = 0;
        monster.specialDefenseBuff = 0;
        monster.criticalBuff = 0;
        monster.ignoreDefenses = 0;
        monster.damageReduction = 0;
        monster.energyShieldValue = 0;

        //Other Parameters
        monster.energyShield = 0;
        monster.mustCritical = 0;
        monster.surviveLethalBlow = 0;
        monster.totalIgnoreDefense = 0;
        monster.immuneCritical = 0;
        monster.elementDamageReduction = 0;
    }
}