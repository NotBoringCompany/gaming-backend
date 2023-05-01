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

public static class BattlefieldEXP
{
    //Cloud Function
    [FunctionName("BattlefieldGetEXP")]
    public static async Task<dynamic> BattlefieldGetEXP([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
        new GetUserDataRequest { 
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam"}
        } );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        List<string> activeMonsterUid = new List<string>();
        string allMonsterUid = string.Empty;
        int wildBattleIndex = new int();

        //Check args["ThisMonsterUniqueID"] if it's null or not
        if(args["allMonsterUid"] != null)
            allMonsterUid = (string) args["allMonsterUid"];

        if(args["WildBattleIndex"] != null)
            wildBattleIndex = (int)args["WildBattleIndex"];
        else
            return $"Error: WildBattleIndex variable not registered!";

        activeMonsterUid = JsonConvert.DeserializeObject<List<string>>(allMonsterUid);
        GenerateEnemyTeam.GenerateWildNBMon(enemyTeam, wildBattleIndex);

        //Declare new Variable for looping
        List<NBMonBattleDataSave> playerTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave currentMonsterData = new NBMonBattleDataSave();
        //Get Value
        playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        
        foreach(var monsterUid in activeMonsterUid)
        {
            //Convert from json to NBmonBattleDataSave
            currentMonsterData = UseItem.FindMonster(monsterUid, playerTeam, null);

            if(currentMonsterData != null)
            {
                NBMonBattleDataSave enemyData = enemyTeam[0];

                //Add EXP, but the dataFromAzureToClient is null because we don't need it to appear in UI
                AttackFunction.AddEXP(enemyData, currentMonsterData, null, activeMonsterUid.Count);

                //Put the EXP into Monster and level up it if the requirement meet.
                BattleFinished.GetEXPLogicIndividual(currentMonsterData);
            }
            else
            {
                return $"Error: Unique ID of {allMonsterUid} not found!";
            }
        }

        ItemDropsData data = new ItemDropsData();
        data.reasoning = $"{activeMonsterUid.Count()} of an active player Monster received EXP and updated to PlayFab!";

        //Item Drops
        data.itemDrops = ItemDrops(enemyTeam[0]);

        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var updateReq = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
            {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)}
            }});

        return JsonConvert.SerializeObject(data);
    }

    public static List<string> ItemDrops(NBMonBattleDataSave defeatedMonsterData)
    {
        var monsterData = NBMonDatabase.FindMonster(defeatedMonsterData.monsterId);
        var lootList = monsterData.LootLists;
        List<string> addedLoots = new List<string>();

        foreach(var lootData in lootList)
        {
            Random r = new Random();

            //add LootData if RNG matches
            if(lootData.RNGChance > r.Next(0, 99))
            {
                addedLoots.Add(lootData.ItemDrop.ItemName);
            }
        }

        return addedLoots;
    }

    public class ItemDropsData
    {
        public string reasoning;
        public List<string> itemDrops;
    }
}