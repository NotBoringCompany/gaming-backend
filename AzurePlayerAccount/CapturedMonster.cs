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
using Microsoft.Azure.Documents.Client;
using System.Linq;

public static class CapturedMonster{

    //Class to Send Azure Data into Client
    public class DataToClient{
        public NBMonBattleDataSave WildMonsterData = new NBMonBattleDataSave();
    }

    

    //Azure Function
    [FunctionName("CapturedWildMonster")]
    public static async Task<dynamic> CapturedWildMonster([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request User Title Data
        var requestUserTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            }
        );

        var reqUserInternalTitleData = await serverApi.GetUserInternalDataAsync( new GetUserDataRequest{
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
        });

        //Declare Used Variable
        List<NBMonBattleDataSave> StellaPC = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> TeamInformation = new List<NBMonBattleDataSave>();
        DataToClient ClientData = new DataToClient();
        int DataID = new int();
        string playerETHAdress = "";

        //Check Player's ETH Adress as Owner
        if(reqUserInternalTitleData.Result.Data.ContainsKey("ethAddress"))
            playerETHAdress = reqUserInternalTitleData.Result.Data["ethAddress"].Value;
        
        //Get Value From Client
        if(args["DataID"] != null)
            DataID = (int)args["DataID"];

        //Extract Player's Team
        TeamInformation = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestUserTitleData.Result.Data["CurrentPlayerTeam"].Value);

        //Check if Title Data Exists run the script
        if(requestUserTitleData.Result.Data.ContainsKey("StellaPC")){
            StellaPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestUserTitleData.Result.Data["StellaPC"].Value);
        }

        //Generate Wild NBMon Status and Add into Stella PC or Player's Current Equipped Team depending on situation (Player NBMon Slot).
        CapturedWildNBMon(TeamInformation, StellaPC, DataID, ClientData, playerETHAdress);
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

    public static void CapturedWildNBMon(List<NBMonBattleDataSave> teamInformation, List<NBMonBattleDataSave> stellaPC, int dataId, DataToClient clientData, string playerETHAdress)
    {
        NBMonBattleDatabase UsedData = RandomBattleDatabase.GetWildBattleData(dataId);

        foreach(var MonsterDataFromRandomBattle in UsedData.MonsterDatas)
        {
            NBMonBattleDataSave monsterData = new NBMonBattleDataSave();

            //Insert Data from Databse into NBMonBattleDataSave
            if(string.IsNullOrEmpty(playerETHAdress))
                monsterData.owner = "WILD";
            else
                monsterData.owner = playerETHAdress;

            monsterData.monsterId = MonsterDataFromRandomBattle.MonsterID;
            monsterData.nickName = monsterData.monsterId;
            monsterData.skillList = MonsterDataFromRandomBattle.EquipSkill;
            monsterData.uniqueSkillList = MonsterDataFromRandomBattle.InheritedSkill;
            monsterData.passiveList = MonsterDataFromRandomBattle.Passive;

            //Let's Generate This Monster's Level
            NBMonStatsCalculation.GenerateFixedLevel(monsterData, UsedData.LevelRange);

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase = NBMonDatabase.FindMonster(monsterData.monsterId);

            //Let's Generate This Monster UniqueID
            monsterData.uniqueId = "Demo" + new Random().Next(0, 999999999).ToString();

            //Let's Generate This Monster's Quality
            NBMonStatsCalculation.GenerateThisMonsterQuality(monsterData);

            //Let's Generate It's Potential Stats
            NBMonStatsCalculation.GenerateRandomPotentialValue(monsterData, monsterFromDatabase);

            //Generate This Monster Base Stats
            NBMonStatsCalculation.StatsCalculation(monsterData, monsterFromDatabase);

            //Fully Recovery HP and Energy
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Energy, 100);

            //Make Other Data not null.
            monsterData.statusEffectList = new List<StatusEffectList>();
            monsterData.temporaryPassives = new List<string>();
            monsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();

            //Once Done Processing This Monster Data, Add This Monster Into Team Information if Slot is less than 4 or Stella PC
            if(teamInformation.Count < 4){
                teamInformation.Add(monsterData);
            }
            else{
                stellaPC.Add(monsterData);
            }
            clientData.WildMonsterData = monsterData;

            //Break The Function to Prevent Looping
            break;
        }
    }
}