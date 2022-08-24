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

public static class CheckPlayerRealmData
{
    

    public class RealmData
    {
        public string MonsterUniqueID;
        public int Level;
        public int CurrentEXP;
    }

    public static RealmData GetThisMonsterRealmData(string UniqueID, List<RealmData> CurrentSelectedRealmData)
    {
        foreach(var MonsterRealmData in CurrentSelectedRealmData)
        {
            if(MonsterRealmData.MonsterUniqueID == UniqueID)
                return MonsterRealmData;
        }

        return null;
    }

    [FunctionName("CheckPlayerRealmData")]
    public static async Task<dynamic> CheckPlayerRealmDataFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Get User Title Data Information
        var reqTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Declare Variable
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> StellaPC = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> BlockChainPC = new List<NBMonBattleDataSave>();
        List<RealmData> CurrentSelectedRealmData = new List<RealmData>();
        int RealmID = new int();

        //Check args["RealmID"] if it's null or not
        if(args["RealmID"] != null)
            RealmID = (int)args["RealmID"];
        else
        {
            return $"RealmID function parameter was not set, Error Code Rozen-001.";
        }

        //About Realm ID, in PlayFab, it will be stored with a key value of
        //PlayerData_Realm_RealmID, example PlayerData_Realm_0 for Mini Demo World.

        //Let's extract CurrentPlayerTeam Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("CurrentPlayerTeam"))
        {
            PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["CurrentPlayerTeam"].Value);
        }

        //Let's extract StellaPC Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("StellaPC"))
        {
            StellaPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaPC"].Value);
        }

        //Let's extract StellaPC Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("StellaBlockChainPC"))
        {
            BlockChainPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaBlockChainPC"].Value);
        }

        //Let's extract PlayerData_Realm_{RealmID} Data from PlayFab, this is important as if the player already have the Data or not.
        if(reqTitleData.Result.Data.ContainsKey($"PlayerData_Realm_{RealmID}"))
        {
            CurrentSelectedRealmData = JsonConvert.DeserializeObject<List<RealmData>>(reqTitleData.Result.Data[$"PlayerData_Realm_{RealmID}"].Value);
        }

        //LoadPlayerRealmData
        LoadPlayerRealmData(PlayerTeam, StellaPC, BlockChainPC, CurrentSelectedRealmData);

        //After That, let's update back to PlayFab.
        var requestUpdateToPlayerTitleData = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"StellaBlockChainPC", JsonConvert.SerializeObject(BlockChainPC)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"StellaPC", JsonConvert.SerializeObject(StellaPC)},
                 {$"PlayerData_Realm_{RealmID}", JsonConvert.SerializeObject(CurrentSelectedRealmData)}
                }
            }
        );

        return null;
    }

    [FunctionName("SavePlayerRealmData")]
    public static async Task<dynamic> SavePlayerRealmDataFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Get User Title Data Information
        var reqTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Declare Variable
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> StellaPC = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> BlockChainPC = new List<NBMonBattleDataSave>();
        List<RealmData> CurrentSelectedRealmData = new List<RealmData>();
        int RealmID = new int();

        //Check args["RealmID"] if it's null or not
        if(args["RealmID"] != null)
            RealmID = (int)args["RealmID"];
        else
        {
            return $"RealmID function parameter was not set, Error Code Rozen-001.";
        }

        //About Realm ID, in PlayFab, it will be stored with a key value of
        //PlayerData_Realm_RealmID, example PlayerData_Realm_0 for Mini Demo World.

        //Let's extract CurrentPlayerTeam Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("CurrentPlayerTeam"))
        {
            PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["CurrentPlayerTeam"].Value);
        }

        //Let's extract StellaPC Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("StellaPC"))
        {
            StellaPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaPC"].Value);
        }

        //Let's extract StellaPC Data from PlayFab
        if(reqTitleData.Result.Data.ContainsKey("StellaBlockChainPC"))
        {
            BlockChainPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaBlockChainPC"].Value);
        }

        //Let's extract PlayerData_Realm_{RealmID} Data from PlayFab, this is important as if the player already have the Data or not.
        if(reqTitleData.Result.Data.ContainsKey($"PlayerData_Realm_{RealmID}"))
        {
            CurrentSelectedRealmData = JsonConvert.DeserializeObject<List<RealmData>>(reqTitleData.Result.Data[$"PlayerData_Realm_{RealmID}"].Value);
        }

        //Save Player Realm Data Into PlayFab
        SavePlayerRealmDataList(PlayerTeam, StellaPC, BlockChainPC, CurrentSelectedRealmData);

        //After That, let's update back to PlayFab.
        var requestUpdateToPlayerTitleData = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"StellaBlockChainPC", JsonConvert.SerializeObject(BlockChainPC)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"StellaPC", JsonConvert.SerializeObject(StellaPC)},
                 {$"PlayerData_Realm_{RealmID}", JsonConvert.SerializeObject(CurrentSelectedRealmData)}
                }
            }
        );

        return null;
    }

    public static void SavePlayerRealmDataList(List<NBMonBattleDataSave> PlayerTeam, List<NBMonBattleDataSave> StellaPC, List<NBMonBattleDataSave> BlockChainPC, List<RealmData> CurrentSelectedRealmData)
    {
        foreach(var Monster in PlayerTeam)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            if(ThisMonsterRealmData != null)
            {
                ThisMonsterRealmData.Level = Monster.level;
                ThisMonsterRealmData.CurrentEXP = Monster.currentExp;
            }
            else
            {
                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = Monster.level;
                NewData.CurrentEXP = Monster.currentExp;
                CurrentSelectedRealmData.Add(NewData);
            }
        }

        foreach(var Monster in StellaPC)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            if(ThisMonsterRealmData != null)
            {
                ThisMonsterRealmData.Level = Monster.level;
                ThisMonsterRealmData.CurrentEXP = Monster.currentExp;
            }
            else
            {
                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = Monster.level;
                NewData.CurrentEXP = Monster.currentExp;
                CurrentSelectedRealmData.Add(NewData);
            }
        }

        foreach(var Monster in BlockChainPC)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            if(ThisMonsterRealmData != null)
            {
                ThisMonsterRealmData.Level = Monster.level;
                ThisMonsterRealmData.CurrentEXP = Monster.currentExp;
            }
            else
            {
                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = Monster.level;
                NewData.CurrentEXP = Monster.currentExp;
                CurrentSelectedRealmData.Add(NewData);
            }
        }
    }

    public static void LoadPlayerRealmData(List<NBMonBattleDataSave> PlayerTeam, List<NBMonBattleDataSave> StellaPC, List<NBMonBattleDataSave> BlockChainPC, List<RealmData> CurrentSelectedRealmData)
    {
        foreach(var Monster in PlayerTeam)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            //Check if This Monster Realm Data is not null, apply the Level and EXP
            if(ThisMonsterRealmData != null)
            {
                Monster.level = ThisMonsterRealmData.Level;
                Monster.currentExp = ThisMonsterRealmData.CurrentEXP;
            }
            else //If This Monster Realm Data is null, apply default Level and EXP also Add this Monster Realm Data related to it
            {
                Monster.level = 5;
                Monster.currentExp = 0;

                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = 5;
                NewData.CurrentEXP = 0;
                CurrentSelectedRealmData.Add(NewData);
            }

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(Monster.monsterId);

            //After that Recalculate NBMon Stats
            NBMonStatsCalculation.StatsCalculation(Monster, MonsterFromDatabase, true);
        }

        foreach(var Monster in StellaPC)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            //Check if This Monster Realm Data is not null, apply the Level and EXP
            if(ThisMonsterRealmData != null)
            {
                Monster.level = ThisMonsterRealmData.Level;
                Monster.currentExp = ThisMonsterRealmData.CurrentEXP;
            }
            else //If This Monster Realm Data is null, apply default Level and EXP also Add this Monster Realm Data related to it
            {
                Monster.level = 5;
                Monster.currentExp = 0;

                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = 5;
                NewData.CurrentEXP = 0;
                CurrentSelectedRealmData.Add(NewData);
            }

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(Monster.monsterId);

            //After that Recalculate NBMon Stats
            NBMonStatsCalculation.StatsCalculation(Monster, MonsterFromDatabase, true);
        }

        foreach(var Monster in BlockChainPC)
        {
            RealmData ThisMonsterRealmData = GetThisMonsterRealmData(Monster.uniqueId, CurrentSelectedRealmData);

            //Check if This Monster Realm Data is not null, apply the Level and EXP
            if(ThisMonsterRealmData != null)
            {
                Monster.level = ThisMonsterRealmData.Level;
                Monster.currentExp = ThisMonsterRealmData.CurrentEXP;
            }
            else //If This Monster Realm Data is null, apply default Level and EXP also Add this Monster Realm Data related to it
            {
                Monster.level = 5;
                Monster.currentExp = 0;

                //Add this new Monster Ralm Data into CurrentSelectedRealmData
                RealmData NewData = new RealmData();
                NewData.MonsterUniqueID = Monster.uniqueId;
                NewData.Level = 5;
                NewData.CurrentEXP = 0;
                CurrentSelectedRealmData.Add(NewData);
            }

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(Monster.monsterId);

            //After that Recalculate NBMon Stats
            NBMonStatsCalculation.StatsCalculation(Monster, MonsterFromDatabase, true);
        }
    }
}