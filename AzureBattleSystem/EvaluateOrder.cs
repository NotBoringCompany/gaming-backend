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

public static class EvaluateOrder
{
    

    //Status Effect Counter (Decrease Counter)
    private static void DecreaseCounter(NBMonBattleDataSave Monster)
    {
        //Reverse loop is needed to remove Status Effect
        for(int i = Monster.statusEffectList.Count-1 ; i >= 0; i--)
        {
            //Decrease counter value
            Monster.statusEffectList[i].counter--;

            //Check if counter value equal 0 or below
            if(Monster.statusEffectList[i].counter <= 0)
                Monster.statusEffectList.RemoveAt(i);
        }
    }

    //Change Battle Speed Value if there's Speed Buff or Speed Debuff and Apply Status Effect
    public static void StatusEffectLogicDuringStartTurn(NBMonBattleDataSave ThisMonster)
    {
        //Let's return This Monster's Battle Speed into Normal Speed First.
        ThisMonster.battleSpeed = ThisMonster.speed;

        //Looping through All Status Effects from ThisMonster
        for (int i = 0; i < ThisMonster.statusEffectList.Count; i++)
        {
            //Let's find status effect first (this is individual status effect for each list)
            var TheStatusEffectParameter = UseItem.FindStatusEffectFromDatabase((int)ThisMonster.statusEffectList[i].statusEffect);
            var StatusEffectStack = ThisMonster.statusEffectList[i].stacks;

            //Get This Monster's Temporary Passive Data first.
            var ThisNBMonTempPassive = ThisMonster.temporaryPassives;

            //Do Logic about HP and Energy Recovery / Burn Damage
            if(TheStatusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.HP_Energy_Related)
            {
                NBMonTeamData.StatsValueChange(ThisMonster, NBMonProperties.StatsType.Hp, TheStatusEffectParameter.HPChangesInNumber * StatusEffectStack);
                NBMonTeamData.StatsValueChange(ThisMonster, NBMonProperties.StatsType.Energy, TheStatusEffectParameter.EnergyChangesInNumber * StatusEffectStack);
                NBMonTeamData.StatsPercentageChange(ThisMonster, NBMonProperties.StatsType.Hp, TheStatusEffectParameter.HPChangesInPercent * StatusEffectStack);
                NBMonTeamData.StatsPercentageChange(ThisMonster, NBMonProperties.StatsType.Energy, TheStatusEffectParameter.EnergyChangesInPercent * StatusEffectStack);

                //Make Sure This NBMon's HP stays at 1 as Death by Debuff is not possible (Will Break the game)
                if(ThisMonster.hp <= 0)
                    ThisMonster.hp = 1;
            }

            //Let's change the Battle Speed
            if(TheStatusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Speed_Related)
            {
                //Declare SpeedBuff and Calculate its Value
                float SpeedBuff = TheStatusEffectParameter.Speed/100f * StatusEffectStack;

                //Calculate This Monster Battle Speed
                ThisMonster.battleSpeed += (int)MathF.Floor((float)ThisMonster.speed * SpeedBuff);
            }

            //If Paralyzed, This Monster's Battle Speed is Halved
            if(TheStatusEffectParameter.statusConditionName == NBMonProperties.StatusEffect.Paralyzed)
            {
                //Calculate This Monster Battle Speed (Paralyzed, Reduced by 50%).
                ThisMonster.battleSpeed -= (int)MathF.Floor((float)ThisMonster.speed * 0.5f);
            }

            //Add Temporary Passive from Status Effect
            if(TheStatusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Add_Temp_Passive_Related)
            {   
                if(!ThisNBMonTempPassive.Contains(TheStatusEffectParameter.passiveName))
                    ThisNBMonTempPassive.Add(TheStatusEffectParameter.passiveName);
            }
        }
    }

    //Cloud Methods (Combine Evaluate Order and Start Turn Function, usually for Passive Activation, Active the Status Effect and Passive for Speed and Reduce the Status Effect Counter)
    [FunctionName("EvaluateOrder")]
    public static async Task<dynamic> EvaluteOrder([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "AllMonsterUniqueID_BF", "BattleEnvironment"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> CurrentNBMonOnBF = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> SortedOrder = new List<string>();
        int BattleCondition = new int();
        int CurrentTurn = new int();

        //Check args["BattleAdvantage"] if it's null or not
        if(args["BattleAdvantage"] != null)
            BattleCondition = (int)args["BattleAdvantage"];
        else
            BattleCondition = 0; //Default Battle Condition

        //Check args["Turn"] if it's null or not
        if(args["Turn"] != null)
            CurrentTurn = (int)args["Turn"];
        else
            CurrentTurn = 1;

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        AllMonsterUniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["AllMonsterUniqueID_BF"].Value);
        
        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Insert Player Team and Enemy Team from Local Variable into Static Variable
        NBMonTeamData.PlayerTeam = PlayerTeam;
        NBMonTeamData.EnemyTeam = EnemyTeam;

        //Let's find NBMon in BF using AllMonsterUniqueID_BF, TO DO: Artifact Passives
        foreach(var MonsterID in AllMonsterUniqueID_BF)
        {
            ///BattleCondition = 0, Normal Battle
            ///BattleCondition = 1, Player Advantage
            ///BattleCondition = -1. Enemy Advantage

            //CurrentTurn = 0, First Turn
            //CurrentTurn > 0, Second Turn and So On

            if(BattleCondition == 0 || BattleCondition == 1)
            {
                var PlayerNBMonData = NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(MonsterID, PlayerTeam);
                int MonsterIndex = NBMonDatabase_Azure.FindNBMonTeamPositionUsingUniqueID(MonsterID, PlayerTeam);

                if(PlayerNBMonData != null)
                {
                    CurrentNBMonOnBF.Add(PlayerNBMonData);

                    //Reset Temporary Passive
                    PlayerNBMonData.temporaryPassives.Clear();

                    //Do Passive Logics for Player NBMon in Battle Field
                    if(CurrentTurn == 0) //when Enter Battle Field.
                    {
                        //Apply passives that works when received status effect.
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.WhenEnterBattleField, PassiveDatabase.TargetType.originalMonster, PlayerNBMonData, null, null);
                    }
                    else //Second Turn and So On.
                    {
                        //Decrease Status Effect Counter At the start of the Turn
                        DecreaseCounter(PlayerNBMonData);

                        //Apply passives that works when received status effect. (Turn Start and Turn End combined).
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.TurnStart, PassiveDatabase.TargetType.originalMonster, PlayerNBMonData, null, null);
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.TurnEnd, PassiveDatabase.TargetType.originalMonster, PlayerNBMonData, null, null);
                    
                        //Add NBMon Energy
                        NBMonTeamData.StatsValueChange(PlayerNBMonData, NBMonProperties.StatsType.Energy, 25);
                    }

                    //Status Effect Logic during Start Turn.
                    StatusEffectLogicDuringStartTurn(PlayerNBMonData);

                    //Update PlayerTeam Information
                    PlayerTeam[MonsterIndex] = PlayerNBMonData;

                    continue;
                }
            }

            if(BattleCondition == 0 || BattleCondition == -1)
            {
                var EnemyNBMonData =  NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(MonsterID, EnemyTeam);
                int MonsterIndex = NBMonDatabase_Azure.FindNBMonTeamPositionUsingUniqueID(MonsterID, EnemyTeam);

                if(EnemyNBMonData != null)
                {
                    CurrentNBMonOnBF.Add(EnemyNBMonData);

                    //Reset Temporary Passive
                    EnemyNBMonData.temporaryPassives.Clear();

                    //Do Passive Logics for Enemy in Battle Field
                    if(CurrentTurn == 0) //when Enter Battle Field.
                    {
                        //Apply passives that works when received status effect.
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.WhenEnterBattleField, PassiveDatabase.TargetType.originalMonster, EnemyNBMonData, null, null);
                    }
                    else //Second Turn and So On.
                    {
                        //Decrease Status Effect Counter At the start of the Turn
                        DecreaseCounter(EnemyNBMonData);

                        //Apply passives that works when received status effect. (Turn Start and Turn End combined).
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.TurnStart, PassiveDatabase.TargetType.originalMonster, EnemyNBMonData, null, null);
                        PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.TurnEnd, PassiveDatabase.TargetType.originalMonster, EnemyNBMonData, null, null);

                        //Add NBMon Energy
                        NBMonTeamData.StatsValueChange(EnemyNBMonData, NBMonProperties.StatsType.Energy, 25);
                    }

                    //Status Effect Logic during Start Turn.
                    StatusEffectLogicDuringStartTurn(EnemyNBMonData);

                    //Update Enemy Team Information
                    EnemyTeam[MonsterIndex] = EnemyNBMonData;

                    continue;
                }
            }
        }

        //After getting the NBMons to be sorted, let's sort it by Battle Speed.
        CurrentNBMonOnBF = CurrentNBMonOnBF.OrderByDescending(f => f.battleSpeed).ToList();

        //After sorted by Battle Speed, add their Unique IDs into the SortOrder String List.
        foreach(var SortedMonster in CurrentNBMonOnBF)
        {
            SortedOrder.Add(SortedMonster.uniqueId);
        }

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );
        
        var SortedOrderJsonString = JsonConvert.SerializeObject(SortedOrder);

        return SortedOrderJsonString;
    }
}