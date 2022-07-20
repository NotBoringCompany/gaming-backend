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

public static class CapturedMonster{

    //Class to Send Azure Data into Client
    public class DataToClient{
        public NBMonBattleDataSave WildMonsterData = new NBMonBattleDataSave();
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

    //Azure Function
    [FunctionName("CapturedWildMonster")]
    public static async Task<dynamic> CapturedWildMonster([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Convert JsonString into a Class
        RandomBattleDatabase.GetData();

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request User Title Data
        var requestUserTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            }
        );

        //Declare Used Variable
        List<NBMonBattleDataSave> StellaPC = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> TeamInformation = new List<NBMonBattleDataSave>();
        DataToClient ClientData = new DataToClient();
        int DataID = new int();
        
        //Get Value From Client
        if(args["DataID"] != null)
            DataID = (int)args["DataID"];

        TeamInformation = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestUserTitleData.Result.Data["CurrentPlayerTeam"].Value);

        //Check if Title Data Exists run the script
        if(requestUserTitleData.Result.Data.ContainsKey("StellaPC")){
            StellaPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestUserTitleData.Result.Data["StellaPC"].Value);
        }

        //Generate Wild NBMon Status and Save Add into Stella PC
        CapturedWildNBMon(TeamInformation,StellaPC, DataID, ClientData);
        var updateStellaPC = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
                    {"CurrentPlayerTeam", JsonConvert.SerializeObject(TeamInformation)},
                    {"StellaPC", JsonConvert.SerializeObject(StellaPC)}
                }
            }
        );

        //Return Captured Monster Data into Client Using JsonString
        string JsonString = JsonConvert.SerializeObject(ClientData.WildMonsterData).ToString();
        log.LogInformation(JsonString);
        return JsonString;
    }

    public static void CapturedWildNBMon(List<NBMonBattleDataSave> TeamInformation, List<NBMonBattleDataSave> StellaPC, int DataID, DataToClient ClientData)
    {
        NBMonBattleDatabase UsedData = RandomBattleDatabase.RandomBattleData[DataID];

        foreach(var MonsterDataFromRandomBattle in UsedData.MonsterDatas)
        {
            NBMonBattleDataSave MonsterData = new NBMonBattleDataSave();

            //Insert Data from Databse into NBMonBattleDataSave
            MonsterData.owner = "WILD";
            MonsterData.monsterId = MonsterDataFromRandomBattle.MonsterID;
            MonsterData.nickName = MonsterData.monsterId;
            MonsterData.skillList = MonsterDataFromRandomBattle.EquipSkill;
            MonsterData.uniqueSkillList = MonsterDataFromRandomBattle.InheritedSkill;
            MonsterData.passiveList = MonsterDataFromRandomBattle.Passive;

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(MonsterData.monsterId);

            //Let's Generate This Monster's Level
            NBMonStatsCalculation.GenerateRandomLevel(MonsterData, UsedData.LevelRange);

            //Let's Generate This Monster UniqueID
            MonsterData.uniqueId = "Demo" + new Random().Next(0, 999999999).ToString();

            //Let's Generate This Monster's Quality
            NBMonStatsCalculation.GenerateThisMonsterQuality(MonsterData);

            //Let's Generate It's Potential Stats
            NBMonStatsCalculation.GenerateRandomPotentialValue(MonsterData, MonsterFromDatabase);

            //Generate This Monster Base Stats
            NBMonStatsCalculation.StatsCalculation(MonsterData, MonsterFromDatabase);

            //Fully Recovery HP and Energy
            NBMonTeamData.StatsPercentageChange(MonsterData, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(MonsterData, NBMonProperties.StatsType.Energy, 100);

            //Make Other Data not null.
            MonsterData.statusEffectList = new List<StatusEffectList>();
            MonsterData.temporaryPassives = new List<string>();
            MonsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();

            //Once Done Processing This Monster Data, Add This Monster Into Team Information if Slot is less than 4 or Stella PC
            if(TeamInformation.Count < 4){
                TeamInformation.Add(MonsterData);
            }
            else{
                StellaPC.Add(MonsterData);
            }
            ClientData.WildMonsterData = MonsterData;

            //Break The Function to Prevent Looping
            break;
        }
    }
}