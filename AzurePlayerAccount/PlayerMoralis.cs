using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Samples;
using PlayFab.ServerModels;
using System.Collections.Generic;

public static class PlayerMoralis{

    public class MoralisData{
        public string sessionToken;
        public int nbmonId;
        public string playerId;
        public string secretKey;
    }

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

    public static void CopyCurrentTeamData(List<NBMonBattleDataSave> currTeam, List<NBMonBattleDataSave> stellaBC, List<NBMonConvert.NBMonMoralisData> monsterData){
        foreach(var monster in monsterData){
            //Find Current index monster data inside CurrentPlayerTeam and StellaBlockChainPC
            NBMonBattleDataSave monsterCurrTeam = NBMonConvert.FindMonsterFromTeamByUniqueID(currTeam, monster);
            NBMonBattleDataSave monsterStellaBC = NBMonConvert.FindMonsterFromTeamByUniqueID(stellaBC, monster);
            
            //If current index monster found inside StellaBlockChainPC, continue into next index
            if(monsterStellaBC != null)
                continue;

            //Add Into stellaBC if monsterCurrTeam found
            if(monsterCurrTeam != null)
                stellaBC.Add(monsterCurrTeam);
        }
    }

    [FunctionName("UpdateMonsterToMoralis")]
    public static async Task<dynamic> UpdateMonsterToMoralis([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Read Only Title Data
        var reqTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Request Read Only Title Data
        var reqReadOnlyTitleData = await serverApi.GetUserReadOnlyDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Request Internal Data
        var reqInternalData = await serverApi.GetUserInternalDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Check if requested Data exists
        if(reqReadOnlyTitleData.Result.Data.ContainsKey("BlockChainPC") == false)
            return "Data Not Found!! Terminate the process.";

        if(reqInternalData.Result.Data.ContainsKey("sessionToken") == false)
            return "Data Not Found!! Terminate the process.";

        //Declare new variable
        MoralisData newData = new MoralisData();
        List<NBMonConvert.NBMonMoralisData> monsterData = new List<NBMonConvert.NBMonMoralisData>();
        List<NBMonBattleDataSave> currTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> stellaBC = new List<NBMonBattleDataSave>();

        //Set variable from requested data
        monsterData = JsonConvert.DeserializeObject<List<NBMonConvert.NBMonMoralisData>>(reqReadOnlyTitleData.Result.Data["BlockChainPC"].Value);
        currTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["CurrentPlayerTeam"].Value);
        stellaBC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaBlockChainPC"].Value);
        newData.sessionToken = reqInternalData.Result.Data["sessionToken"].Value;
        newData.secretKey = serverApi.apiSettings.DeveloperSecretKey;
        newData.playerId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId;

        //Copy CurrentTeamPlayer into StellaBlockChainPC
        CopyCurrentTeamData(currTeam, stellaBC, monsterData);

        //Update StellaBlockChainPC In PlayFab
        var updateUserData =  await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
                    {"StellaBlockChainPC", JsonConvert.SerializeObject(stellaBC)}
                }
            }
        );

        //Looping monsterData To Get nbmonId;
        foreach(var monster in monsterData){
            //If monster still an egg, skip into next index
            if(monster.isEgg)
                continue;

            //Get Monter nbmonId
            newData.nbmonId = monster.nbmonId;
            
            //Do Something...
            //To Do, call moralis API
            log.LogInformation(JsonConvert.SerializeObject(newData));
        }

        return "Success";
    }
}