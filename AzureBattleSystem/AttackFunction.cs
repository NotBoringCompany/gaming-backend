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
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Keys = new List<string> { "CurrentPlayerTeam", "EnemyTeam", "Team1UniqueID_BF", "BattleEnvironment", "RNGSeeds", "SortedOrder", "MoraleGaugeData", "HumanBattleData" }
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        HumanBattleData humanBattleData = new HumanBattleData();
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave attackerMonster = new NBMonBattleDataSave();
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
        if (args["UnityDataInput"] != null)
        {
            string ArgumentString = args["UnityDataInput"];

            UnityData = JsonConvert.DeserializeObject<DataFromUnity>(ArgumentString);
        }

        //Get Variable if it's an NPC Battle
        if (args["IsNPCBattle"] != null)
        {
            VS_NPC = (bool)args["IsNPCBattle"];
        }

        //Get Variable if it's an NPC Battle
        if (args["IsBossBattle"] != null)
        {
            VS_Boss = (bool)args["IsBossBattle"];
        }

        //Check if monster can attack
        var monsterCanMove = EvaluateOrder.CheckBattleOrder(SortedOrder, UnityData.AttackerMonsterUniqueID);

        if (!monsterCanMove)
        {
            return $"No Monster in the turn order. Error Code: RH-0001";
        }

        //Get Attacker Data from Unity Input
        attackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, PlayerTeam, humanBattleData);

        //If Attacker not found in Player Team, Find Attacker on Enemy Team
        if (attackerMonster == null)
            attackerMonster = UseItem.FindMonster(UnityData.AttackerMonsterUniqueID, EnemyTeam, humanBattleData);

        //Insert Attacker Monster Unique ID.
        DataFromAzureToClient.AttackerMonsterUniqueID = attackerMonster.uniqueId;

        //Declare Skill Slot
        var skillSlot = UnityData.SkillSlot;

        //Check Skill Slot Value, prevent using Value that is not 0 ~ 3
        if (skillSlot < 0)
            skillSlot = 0;

        if (skillSlot > attackerMonster.skillList.Count - 1)
            skillSlot = attackerMonster.skillList.Count - 1;

        //Get all Defender Data from Unity Input
        foreach (string TargetID in UnityData.TargetUniqueIDs)
        {
            var Monster = UseItem.FindMonster(TargetID, PlayerTeam, humanBattleData);

            if (Monster == null)
            {
                //If Target Monster not found, find this from Enemy Team 
                Monster = UseItem.FindMonster(TargetID, EnemyTeam, humanBattleData);
            }

            //Add Monster into DefenderMonster List
            DefenderMonsters.Add(Monster);
        }

        log.LogInformation($"Code A: 1st Step, Get Attacker Skill Data");

        //Let's get Attacker Data like Skill
        SkillsDataBase.SkillInfoPlayFab skill = SkillsDataBase.FindSkill(attackerMonster.skillList[skillSlot]);

        //Deduct Attacker Monster Energy
        NBMonTeamData.StatsValueChange(attackerMonster, NBMonProperties.StatsType.Energy, SkillsDataBase.SkillEnergyCost(skill.skillName, attackerMonster) * -1);

        log.LogInformation($"Code B: 2nd Step, Apply Passive for Attacker");

        //Let's Apply Passive to Attacker Monster before Attacking (ActionBefore)
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.WhenAttacking, PassiveDatabase.TargetType.originalMonster, attackerMonster, null, skill, seedClass);

        log.LogInformation($"Code C: 3rd Step, Looping for each Monster Target");

        //After Applying passive, let's call Apply Skill for each Target Monster
        foreach (var targetMonster in DefenderMonsters)
        {
            if ((targetMonster.fainted || targetMonster.hp <= 0) && DefenderMonsters.Count != 1)
                continue;

            log.LogInformation($"{targetMonster.nickName}, Apply Skill");

            //Apply Skill
            ApplySkill(skill, attackerMonster, targetMonster, DataFromAzureToClient, seedClass, moraleData, PlayerTeam, EnemyTeam, humanBattleData);

            log.LogInformation($"{targetMonster.nickName}, Apply Status Effect Immediately");

            //Apply status effect logic like Anti Poison/Anti Stun.
            StatusEffectIconDatabase.ApplyStatusEffectImmediately(targetMonster);

            log.LogInformation($"{targetMonster.nickName}, Check Monster Died");

            //Let's Check Target Monster wether it's Fainted or not
            CheckTargetDied(targetMonster, attackerMonster, skill, PlayerTeam, EnemyTeam, Team1UniqueID_BF, DataFromAzureToClient);
        }

        log.LogInformation($"Code D: 4th Step, Apply and Remove Status Effect from Attacker Monster");

        //Apply Status Effect to Attacker Monster
        UseItem.ApplyStatusEffect(attackerMonster, skill.statusEffectListSelf, null, false, seedClass);

        //Remove Status Effect to Attacker Monster
        UseItem.RemoveStatusEffect(attackerMonster, skill.removeStatusEffectListSelf);

        //Remove Status Effect to Atttacker Monster with Criteria.
        HardCodedRemoveStatusEffect(attackerMonster, skill.removeStatusEffectTypeSelf);

        log.LogInformation($"Code E: 5th Step, Reset Combat Related Stats");

        //After the Combat, let's resets the Monster's Temporary Stats that only works in Combat Phase
        ResetTemporaryStatsAfterAttacking(attackerMonster);

        foreach (var TargetMonster in DefenderMonsters)
            ResetTemporaryStatsAfterAttacking(TargetMonster);

        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
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

    public static void HardCodedRemoveStatusEffect(NBMonBattleDataSave selectedMonster, SkillsDataBase.RemoveStatusEffectType statusEffectRemovalType)
    {
        if(statusEffectRemovalType == SkillsDataBase.RemoveStatusEffectType.None)
            return;

        UseItem.RemoveStatusEffectByType(selectedMonster, statusEffectRemovalType);

        //Remove All Status Effect if the skill does that
        if (statusEffectRemovalType == SkillsDataBase.RemoveStatusEffectType.All)
            UseItem.RemoveAllStatusEFfect(selectedMonster);
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
        public int DamageTaken; //Normal Damage
        public int EnergyDamageTaken; //Energy Damage
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
    public static void ApplySkill(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave attackerMonster, NBMonBattleDataSave targetMonster, DataSendToUnity dataFromAzureToClient, RNGSeedClass seedClass, BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        //Declare New DamageData
        DamageData thisMonsterDamageData = new DamageData();

        //Apply passive related with receiving element input before get hit!
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.WhenAttacked, PassiveDatabase.TargetType.originalMonster, targetMonster, targetMonster, skill, seedClass);

        //Insert Defender Monster Unique ID.
        thisMonsterDamageData.DefenderMonsterUniqueID = targetMonster.uniqueId;
        
        //When skill type is damage, calculate the damage based on element modifier
        if(skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            CalculateAndDoDamage(skill, attackerMonster, targetMonster, thisMonsterDamageData, seedClass, moraleData, playerTeam, enemyTeam, humanBattleData);
        }

        //Apply passive effect related to after action to the original monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.AfterAttacked, PassiveDatabase.TargetType.originalMonster, attackerMonster, targetMonster, skill, seedClass);

        //Apply passive effect that inflict status effect from this monster to attacker monster (aka original monster), make sure the attacker is not itself and the skill's type is damage
        if (attackerMonster != targetMonster && skill.actionType == SkillsDataBase.ActionType.Damage)
        {
            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.InflictStatusEffect, PassiveDatabase.TargetType.originalMonster, targetMonster, attackerMonster, skill, seedClass);
        }

        //If the skill is Healing Skill, e.g HP and Energy Recovery
        if(skill.actionType == SkillsDataBase.ActionType.StatsRecovery)
        {
            StatsRecoveryLogic(skill, targetMonster, thisMonsterDamageData);
        }

        //Apply Status Effect to Target
        UseItem.ApplyStatusEffect(targetMonster, skill.statusEffectList, null, false, seedClass);

        //Remove Status Effect to Target
        UseItem.RemoveStatusEffect(targetMonster, skill.removeStatusEffectList);

        //Remove Status Effect to Target Monster with Criteria.
        HardCodedRemoveStatusEffect(targetMonster, skill.removeStatusEffectType);

        //Add ThisMonsterDamageData to DataFromAzureToClient
        dataFromAzureToClient.DamageDatas.Add(thisMonsterDamageData);
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

        //Apply passives and artifact passives that only works during combat to This Monster
        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.WhenAttackedDuringCombat, PassiveDatabase.TargetType.originalMonster, targetMonster, null, skill, seedClass);
        //TO DO, Apply Artifact Passive to Defender Monster.

        //Calculate Critical Hit RNG
        int attackerCriticalHitRate = CriticalHitStatsCalculation(attackerMonster, skill, playerTeam, enemyTeam, humanBattleData, moraleData);
        int criticalRNG = EvaluateOrder.ConvertSeedToRNG(seedClass, 0, 1000); //Ranging from 0 to 1000 instead 0 to 100.
        float criticalHitMultiplier = 1f;
        int mustCritical = attackerMonster.mustCritical;

        //Check Target's Immune to Critical or Not
        int immuneToCritical = targetMonster.immuneCritical;

        if (criticalRNG <= attackerCriticalHitRate || mustCritical >= 1) //Critical Hit Damage Multiplier Using Andre's Equation
            criticalHitMultiplier = ((7f * (float)attackerMonster.level) + 15f)/((4f * (float)attackerMonster.level) + 15f);
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
        float target_NBMon_Absorb_Damage = targetMonster.absorbDamageValue / 100f;
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

        //Calculate Attack Logic
        if(skill.techniqueType == SkillsDataBase.TechniqueType.Attack)
        {
            //Attack Logic
            float attackerAttackStats = attackerMonster.attack * (1f + attack_Passive_Modifier) * attack_StatusEffect_Modifier;
            int attackDamage = DamageCalculation(skill.attack, attackerMonster.level, attackerAttackStats, targetMonsterDef, sameTypeAttackBoost, elementModifier, criticalHitMultiplier, randomDamage);
            int damageAttack = (int)Math.Truncate(attackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (1f - target_NBMon_Damage_Reduction_From_Energy_Shield));
            int energyDamageAttack = (int)Math.Truncate(attackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (target_NBMon_Damage_Reduction_From_Energy_Shield));

            if(target_NBMon_Absorb_Damage > 0)
            {
                damageAttack = (int)Math.Truncate(damageAttack * target_NBMon_Absorb_Damage) * -1;
            }

            //Survive Lethal Blow Logic
            if (target_NBMon_Must_Survive_Lethal_Blow >= 1)
            {
                int targetMonsterHP = targetMonster.hp;

                if (targetMonsterHP > 1 && damageAttack > targetMonsterHP)
                    damageAttack = targetMonsterHP - 1;
            }

            // Reduce target NBMon's HP using Attack
            DamageLogic(skill, attackerMonster, targetMonster, thisMonsterDamageData, moraleData, playerTeam, enemyTeam, humanBattleData, criticalHitMultiplier, target_NBMon_Damage_Reduction_Modifier, target_NBMon_Damage_Reduction_From_Energy_Shield, target_NBMon_EnergyShield_IsActive, elementModifier, ref damageAttack, energyDamageAttack);
        }
        else //Calculate Special Attack Logic
        {
            //Special Attack Logic
            float attackerSPAttackStats = attackerMonster.specialAttack * (1f + sp_Attack_Passive_Modifier) * sp_Attack_StatusEffect_Modifier;
            int spAttackDamage = DamageCalculation(skill.specialAttack, attackerMonster.level, attackerSPAttackStats, targetMonsterSPDef, sameTypeAttackBoost, elementModifier, criticalHitMultiplier, randomDamage);
            int damageSpAttack = (int)Math.Truncate(spAttackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (1f - target_NBMon_Damage_Reduction_From_Energy_Shield));
            int energyDamageSPAttack = (int)Math.Truncate(spAttackDamage * (1f - target_NBMon_Damage_Reduction_Modifier) * (target_NBMon_Damage_Reduction_From_Energy_Shield));

            if(target_NBMon_Absorb_Damage > 0)
            {
                damageSpAttack = (int)Math.Truncate(damageSpAttack * target_NBMon_Absorb_Damage) * -1;
            }

            //Survive Lethal Blow Logic
            if (target_NBMon_Must_Survive_Lethal_Blow >= 1)
            {
                int targetMonsterHP = targetMonster.hp;

                if (targetMonsterHP > 1 && damageSpAttack > targetMonsterHP)
                    damageSpAttack = targetMonsterHP - 1;
            }
            
            // Reduce target NBMon's HP using Special Attack
            DamageLogic(skill, attackerMonster, targetMonster, thisMonsterDamageData, moraleData, playerTeam, enemyTeam, humanBattleData, criticalHitMultiplier, target_NBMon_Damage_Reduction_Modifier, target_NBMon_Damage_Reduction_From_Energy_Shield, target_NBMon_EnergyShield_IsActive, elementModifier, ref damageSpAttack, energyDamageSPAttack);
        }
    }

    private static void DamageLogic(SkillsDataBase.SkillInfoPlayFab skill, NBMonBattleDataSave attackerMonster, NBMonBattleDataSave targetMonster, 
    DamageData thisMonsterDamageData, BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData, 
    float criticalHitMultiplier, float target_NBMon_Damage_Reduction_Modifier, float target_NBMon_Damage_Reduction_From_Energy_Shield, int target_NBMon_EnergyShield_IsActive, 
    float elementModifier, ref int overallDamage, int overallEnergyDamage)
    {
        //Check if the Element Modifier is not 0, if its 0, by default the damage is 0.
        if (elementModifier != 0)
        {
            //Normal Damage
            NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Hp, overallDamage * -1);
            
            //Changes morale Data for attacker's team
            GainMoraleGaugeByAttacking(moraleData, playerTeam, enemyTeam, attackerMonster, overallDamage, humanBattleData);

            //Increase Morale Gain to Target's Team
            GainMoraleGaugeByTakingDamage(moraleData, playerTeam, enemyTeam, targetMonster, overallDamage, humanBattleData);

            //Energy Damage
            if (target_NBMon_EnergyShield_IsActive >= 1)
            {
                NBMonTeamData.StatsValueChange(targetMonster, NBMonProperties.StatsType.Energy, overallEnergyDamage * -1);
            }

            //HP and Energy Drain Logic
            HP_EN_DrainFunction(skill, overallDamage, attackerMonster, thisMonsterDamageData);
        }
        else
        {
            //By Default, damageAttack value is 0.
        }

        //Let's get all necessary data from Calculate And Do Damage into This Monster Damage Data.
        RecordDamageData(thisMonsterDamageData, criticalHitMultiplier, elementModifier, overallDamage, overallEnergyDamage);
    }

    private static void RecordDamageData(DamageData thisMonsterDamageData, float criticalHitMultiplier, float elementModifier, int overallDamageTaken, int overallEnergyDamage)
    {
        thisMonsterDamageData.DamageTaken = overallDamageTaken;
        thisMonsterDamageData.EnergyDamageTaken = overallEnergyDamage;
        thisMonsterDamageData.IsCritical = (criticalHitMultiplier > 1); //If Critical Hit Multiplier is higher than 1, it's Critical Hit.
        thisMonsterDamageData.DamageImmune = (elementModifier == 0); //This Monster will deal 0 Damage if Element Modifier = 0.
        thisMonsterDamageData.ElementalVariable = elementModifier;
    }

    //Damage Calculation Method
    private static int DamageCalculation(int skillPower, int attackerLevel, float attackerPower, float targetDef, float STAB, float typeEffects, float criticalDamageMultiplier, float randomValue)
    {
        if (targetDef < 0)
            targetDef *= -1;

        float func_A = ((attackerLevel * skillPower * attackerPower / targetDef / 3f) / 100) + 10;
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

    private static int CriticalHitStatsCalculation(NBMonBattleDataSave originalMonster, SkillsDataBase.SkillInfoPlayFab skill, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData, BattleMoraleGauge.MoraleData moraleData)
    {
        int criticalHit_Skill = skill.criticalRate;
        int criticalHit_Passive = (int)Math.Floor(originalMonster.criticalBuff);
        int criticalHit_NBMon = originalMonster.criticalHit;
        int criticalHit_NBMon_From_StatusEffect = StatusEffectIconDatabase.CriticalStatusEffectLogic(originalMonster);
        int criticalHit_FromMorale = GetCriticalHitFromMorale(originalMonster, playerTeam, enemyTeam, humanBattleData, moraleData);

        return criticalHit_NBMon + criticalHit_NBMon_From_StatusEffect + criticalHit_Skill + criticalHit_Passive + criticalHit_FromMorale;

    }

    private static int GetCriticalHitFromMorale(NBMonBattleDataSave originalMonster, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData, BattleMoraleGauge.MoraleData moraleData)
    {
        //Check if originalMonster is from Player Team
        if(playerTeam.Contains(originalMonster) || originalMonster == humanBattleData.playerHumanData)
        {
            return (moraleData.playerMoraleGauge + 1);
        }
        else
        {
            return (moraleData.enemyMoraleGauge + 1);
        }
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

    public static void GainMoraleGaugeByAttacking(BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, NBMonBattleDataSave monster, int damageData, HumanBattleData humanBattleData)
    {
        if (damageData < 0)
            damageData *= -1;

        var moraleGain = (int)System.Math.Ceiling((float)damageData / (0.5f * (float)monster.level));

        IncreaseMoraleFunction(moraleData, playerTeam, enemyTeam, monster, humanBattleData, moraleGain);

    }

    public static void GainMoraleGaugeByTakingDamage(BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, NBMonBattleDataSave monster, int damageTaken, HumanBattleData humanBattleData)
    {
        if(damageTaken < 0)
            damageTaken *= -1;

        var moraleGain = (int)System.Math.Ceiling((float)damageTaken / (0.35f * (float)monster.level));   

        IncreaseMoraleFunction(moraleData, playerTeam, enemyTeam, monster, humanBattleData, moraleGain);
    }

    private static void IncreaseMoraleFunction(BattleMoraleGauge.MoraleData moraleData, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, NBMonBattleDataSave monster, HumanBattleData humanBattleData, int moraleGain)
    {
        //Player Logic
        if (playerTeam.Contains(monster))
        {
            BattleMoraleGauge.ChangeMoraleGauge(moraleData, moraleGain, true);
        }

        if (humanBattleData.playerHumanData != null)
            if (monster == humanBattleData.playerHumanData)
            {
                BattleMoraleGauge.ChangeMoraleGauge(moraleData, moraleGain, true);
            }

        //Enemy Logic
        if (enemyTeam.Contains(monster))
        {
            BattleMoraleGauge.ChangeMoraleGauge(moraleData, moraleGain, false);
        }

        if (humanBattleData.enemyHumanData != null)
            if (monster == humanBattleData.enemyHumanData)
            {
                BattleMoraleGauge.ChangeMoraleGauge(moraleData, moraleGain, false);
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

                //Add Growth Value to the Attacker.
                AddGrowthValueToAttacker(attackerMonster, targetMonster);
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

        var demoEXP = r.Next(2000,5000);

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
        monster.absorbDamageValue = 0;

        //Other Parameters
        monster.energyShield = 0;
        monster.mustCritical = 0;
        monster.surviveLethalBlow = 0;
        monster.totalIgnoreDefense = 0;
        monster.immuneCritical = 0;
        monster.elementDamageReduction = 0;
    }

    public static void AddGrowthValueToAttacker(NBMonBattleDataSave attackerMonster, NBMonBattleDataSave targetMonster)
    {
        int usedBaseStat = new int();

        var monsterDataBase = NBMonDatabase.FindMonster(targetMonster.monsterId);
        List<int> baseStats = new List<int>{
            monsterDataBase.monsterBaseStat.maxHpBase, monsterDataBase.monsterBaseStat.maxEnergyBase,
            monsterDataBase.monsterBaseStat.speedBase, monsterDataBase.monsterBaseStat.attackBase,
            monsterDataBase.monsterBaseStat.specialAttackBase, monsterDataBase.monsterBaseStat.defenseBase,
            monsterDataBase.monsterBaseStat.specialDefenseBase};

        //Sort Base Stats Value from the hihgest
        baseStats.OrderByDescending(x => x).ToList();
        usedBaseStat = baseStats[0];

        float species = 0.65f; //Assumes Wild

        if (monsterDataBase.Tier == NBMonDatabase.NBMonTierType.Origin)
            species = 1f;
        else if (monsterDataBase.Tier == NBMonDatabase.NBMonTierType.Hybrid)
            species = 0.8f;

        //Calculate the Growth Value Points
        float growthPoints = species * (float)usedBaseStat / 20f;

        //Add the growthPoints value
        AddGrowthValueLogic(attackerMonster, usedBaseStat, monsterDataBase, growthPoints);
    }

    private static void AddGrowthValueLogic(NBMonBattleDataSave attackerMonster, int usedBaseStat, NBMonDatabase.MonsterInfoPlayFab monsterDataBase, float growthPoints)
    {
        //Let's check if the total value is 5000 first
        var totalGrowthValue = attackerMonster.maxHpEffort + attackerMonster.maxEnergyEffort + attackerMonster.speedEffort + attackerMonster.attackEffort + attackerMonster.specialAttackEffort + attackerMonster.defenseEffort + attackerMonster.specialDefenseEffort;

        if((totalGrowthValue + growthPoints) >= 5000)
        {
            growthPoints = (5000 - totalGrowthValue);
        }

        if(growthPoints == 0)
            return;

        //Add the growthPoints value
        if (usedBaseStat == monsterDataBase.monsterBaseStat.maxHpBase)
            attackerMonster.maxHpEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.maxEnergyBase)
            attackerMonster.maxEnergyEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.speedBase)
            attackerMonster.speedEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.attackBase)
            attackerMonster.attackEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.specialAttackBase)
            attackerMonster.specialAttackEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.defenseBase)
            attackerMonster.defenseEffort += (int)Math.Floor(growthPoints);
        else if (usedBaseStat == monsterDataBase.monsterBaseStat.specialDefenseBase)
            attackerMonster.specialDefenseEffort += (int)Math.Floor(growthPoints);

        //Let's normalize the Growth Value
        if(attackerMonster.maxHpEffort > 2250)
            attackerMonster.maxHpEffort = 2250;

        if(attackerMonster.maxEnergyEffort > 2250)
            attackerMonster.maxEnergyEffort = 2250;

        if(attackerMonster.speedEffort > 2250)
            attackerMonster.speedEffort = 2250;

        if(attackerMonster.attackEffort > 2250)
            attackerMonster.attackEffort = 2250;

        if(attackerMonster.specialAttackEffort > 2250)
            attackerMonster.specialAttackEffort = 2250;

        if(attackerMonster.defenseEffort > 2250)
            attackerMonster.defenseEffort = 2250;

        if(attackerMonster.specialDefenseEffort > 2250)
            attackerMonster.specialDefenseEffort = 2250;
    }
}