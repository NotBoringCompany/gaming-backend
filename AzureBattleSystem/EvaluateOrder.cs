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
using static InitialTeamSetup;

public class RNGSeedClass
{
    public List<int> RNGSeeds;
    public int currentRNGSeed = 0;
}

public static class EvaluateOrder
{
    public static void LoadSeeds(string rngSeedsValueFromPlayFab, RNGSeedClass seedClass)
    {
        seedClass = JsonConvert.DeserializeObject<RNGSeedClass>(rngSeedsValueFromPlayFab);
    }

    private static List<int> GenerateNewSeeds()
    {
        Random r = new Random();
        List<int> newSeeds = new List<int>();

        for (int i = 0; i < 50; i++)
        {
            newSeeds.Add(r.Next(0, 10000));
        }

        return newSeeds;
    }

    private static void ChangeCurrentSeed(RNGSeedClass seedClass)
    {
        seedClass.currentRNGSeed++;

        if(seedClass.currentRNGSeed > seedClass.RNGSeeds.Count - 1)
            seedClass.currentRNGSeed = 0;
    }

    public static int ConvertSeedToRNG(RNGSeedClass seedClass, int min = 0, int max = 100)
    {
        Random r = new Random(seedClass.RNGSeeds[seedClass.currentRNGSeed]);
        int rngValue = r.Next(min, max);

        ChangeCurrentSeed(seedClass);

        return rngValue;
    }

    public static double CriticalRNG(RNGSeedClass seedClass)
    {
        Random r = new Random(seedClass.RNGSeeds[seedClass.currentRNGSeed]);
        double rngValue = r.NextDouble();

        ChangeCurrentSeed(seedClass);

        return rngValue;
    }

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
    public static void ApplyEffectsOnStartTurn(NBMonBattleDataSave monster)
    {
        //Looping through All Status Effects from the monster
        StatusEffectLogicDuringStartTurn(monster);
    }

    private static void StatusEffectLogicDuringStartTurn(NBMonBattleDataSave monster)
    {
        for (int i = 0; i < monster.statusEffectList.Count; i++)
        {
            //Let's find status effect first (this is individual status effect for each list)
            var statusEffectParameter = UseItem.FindStatusEffectFromDatabase((int)monster.statusEffectList[i].statusEffect);
            var statusEffectStack = monster.statusEffectList[i].stacks;

            //Get This Monster's Temporary Passive Data first.
            var thisNBMonTempPassive = monster.temporaryPassives;

            //Do Logic about HP and Energy Recovery / Burn Damage
            if (statusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.HP_Energy_Related)
            {
                NBMonTeamData.StatsValueChange(monster, NBMonProperties.StatsType.Hp, statusEffectParameter.HPChangesInNumber * statusEffectStack);
                NBMonTeamData.StatsValueChange(monster, NBMonProperties.StatsType.Energy, statusEffectParameter.EnergyChangesInNumber * statusEffectStack);
                NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Hp, statusEffectParameter.HPChangesInPercent * statusEffectStack);
                NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Energy, statusEffectParameter.EnergyChangesInPercent * statusEffectStack);

                //Make Sure This NBMon's HP stays at 1 as Death by Debuff is not possible (Will Break the game)
                if (monster.hp <= 0)
                    monster.hp = 1;
            }

            //Let's change the Battle Speed
            if (statusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Speed_Related)
            {
                //Declare SpeedBuff and Calculate its Value
                float SpeedBuff = statusEffectParameter.Speed / 100f * statusEffectStack;

                //Calculate This Monster Battle Speed
                monster.battleSpeed += (int)MathF.Floor((float)monster.speed * SpeedBuff);
            }

            //If Paralyzed, This Monster's Battle Speed is Halved
            if (statusEffectParameter.statusConditionName == NBMonProperties.StatusEffect.Paralyzed)
            {
                //Calculate This Monster Battle Speed (Paralyzed, Reduced by 50%).
                monster.battleSpeed -= (int)MathF.Floor((float)monster.speed * 0.5f);
            }

            //Add Temporary Passive from Status Effect
            if (statusEffectParameter.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Add_Temp_Passive_Related)
            {
                if (!thisNBMonTempPassive.Contains(statusEffectParameter.passiveName))
                    thisNBMonTempPassive.Add(statusEffectParameter.passiveName);
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
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Keys = new List<string> { "CurrentPlayerTeam", "EnemyTeam", "AllMonsterUniqueID_BF", "BattleEnvironment", "HumanBattleData" }
            }
        );

        //Generate Seed
        var newSeedClass = new RNGSeedClass();

        newSeedClass.RNGSeeds = GenerateNewSeeds();

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> CurrentNBMonOnBF = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> SortedOrder = new List<string>();
        int BattleCondition = new int();
        int CurrentTurn = new int();
        HumanBattleData humanBattleData = new HumanBattleData();

        //Check args["BattleAdvantage"] if it's null or not
        if (args["BattleAdvantage"] != null)
            BattleCondition = (int)args["BattleAdvantage"];
        else
            BattleCondition = 0; //Default Battle Condition

        //Check args["Turn"] if it's null or not
        if (args["Turn"] != null)
            CurrentTurn = (int)args["Turn"];
        else
            CurrentTurn = 1;

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        AllMonsterUniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["AllMonsterUniqueID_BF"].Value);
        humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);

        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Insert Player Team and Enemy Team from Local Variable into Static Variable
        NBMonTeamData.PlayerTeam = PlayerTeam;
        NBMonTeamData.EnemyTeam = EnemyTeam;

        //Let's find NBMon in BF using AllMonsterUniqueID_BF
        ServerOrderLogic(newSeedClass, PlayerTeam, EnemyTeam, CurrentNBMonOnBF, AllMonsterUniqueID_BF, BattleCondition, CurrentTurn, humanBattleData);

        //After getting the NBMons to be sorted, let's sort it by Battle Speed.
        CurrentNBMonOnBF = CurrentNBMonOnBF.OrderByDescending(f => f.battleSpeed).ToList();

        //After sorted by Battle Speed, add their Unique IDs into the SortOrder String List.
        foreach (var SortedMonster in CurrentNBMonOnBF)
        {
            SortedOrder.Add(SortedMonster.uniqueId);
        }

        newSeedClass.currentRNGSeed = 0;

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)},
                 {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)},
                 {"RNGSeeds", JsonConvert.SerializeObject(newSeedClass)}
                }
            }
        );

        var SortedOrderJsonString = JsonConvert.SerializeObject(SortedOrder);

        return SortedOrderJsonString;
    }

    private static void ServerOrderLogic(RNGSeedClass newSeedClass, List<NBMonBattleDataSave> PlayerTeam, List<NBMonBattleDataSave> EnemyTeam, List<NBMonBattleDataSave> CurrentNBMonOnBF, List<string> AllMonsterUniqueID_BF, int BattleCondition, int CurrentTurn, HumanBattleData humanBattleData)
    {
        foreach (var monsterUniqueID in AllMonsterUniqueID_BF)
        {
            var nbMonData = FindNBMonData(monsterUniqueID, BattleCondition, PlayerTeam, EnemyTeam, humanBattleData);

            if (nbMonData == null || nbMonData.fainted || nbMonData.hp <= 0)
            {
                continue;
            }

            CurrentNBMonOnBF.Add(nbMonData);

            nbMonData.battleSpeed = nbMonData.speed;
            nbMonData.temporaryPassives.Clear();

            //Decrease Status Effect Counter.
            DecreaseCounter(nbMonData);
            NBMonTeamData.StatsValueChange(nbMonData, NBMonProperties.StatsType.Energy, EnergyRecoveryPerTurn(nbMonData));

            ApplyPassiveEffects(newSeedClass, CurrentTurn, nbMonData);
            
            ApplyEffectsOnStartTurn(nbMonData);
        }
    }

    private static NBMonBattleDataSave FindNBMonData(string monsterUniqueID, int battleCondition, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        if (battleCondition == 1 || battleCondition == 0)
        {
            var nbMonData = NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(monsterUniqueID, playerTeam);

            if (nbMonData == null && monsterUniqueID == humanBattleData.playerHumanData?.uniqueId)
            {
                nbMonData = humanBattleData.playerHumanData;
            }

            if (nbMonData != null)
            {
                return nbMonData;
            }
        }

        if (battleCondition == -1 || battleCondition == 0)
        {
            var nbMonData = NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(monsterUniqueID, enemyTeam);

            if (nbMonData == null && monsterUniqueID == humanBattleData.enemyHumanData?.uniqueId)
            {
                nbMonData = humanBattleData.enemyHumanData;
            }

            if (nbMonData != null)
            {
                return nbMonData;
            }
        }

        return null;
    }


    private static void ApplyPassiveEffects(RNGSeedClass newSeedClass, int currentTurn, NBMonBattleDataSave nbMonData)
    {
        //Apply passives at the start of the battle
        if (currentTurn == 0)
        {
            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.AtTheStartOfTheBattle, PassiveDatabase.TargetType.originalMonster, nbMonData, null, null, newSeedClass);
        }

        //Apply passives for each turn, includes at the start of the battle as well.
        if (currentTurn >= 0)
        {
            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.TurnStart, PassiveDatabase.TargetType.originalMonster, nbMonData, null, null, newSeedClass);
        }
    }

    
    public static bool CheckBattleOrder(List<string> sortedOrder, string selectedMonsterUniqueID)
    {
        //Let's check the selectedMonsterUniqueID. If exisst, removes the monster from the sorted Order.
        if(sortedOrder.Contains(selectedMonsterUniqueID))
        {
            sortedOrder.Remove(selectedMonsterUniqueID);
            return true;
        }
        else
            return false;
    }

    private static int EnergyRecoveryPerTurn(NBMonBattleDataSave monster)
    {
        var monsterEnergy = monster.maxEnergy;
        return (int)Math.Ceiling((0.05f * monsterEnergy) + 1);
    }
}