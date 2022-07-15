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

public static class BattleFinished
{
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

    //Win Logic
    public static void DoWinLogic(List<NBMonBattleDataSave> PlayerTeam, List<NBMonBattleDataSave> EnemyTeam, List<string> DroppedItemCredential, List<int> AllMonsterEXPMemoryStorage)
    {
        //Gain EXP to every single NBMon in the Player Team
        GetEXPLogic(PlayerTeam, AllMonsterEXPMemoryStorage);

        //Add Item Drops From Enemy Team
        foreach(var EnemyMonster in EnemyTeam)
        {
            if(EnemyMonster.fainted)
            {
                GetItemDropFromEnemyMonster(EnemyMonster, DroppedItemCredential);
            }
        }

        //TO DO, Currency
    }

    //Get Item Drop From Each Monster Fainted in Enemy Team
    public static void GetItemDropFromEnemyMonster(NBMonBattleDataSave EnemyMonster, List<string> DroppedItemCredential)
    {
        //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
        NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(EnemyMonster.monsterId);

        if(MonsterFromDatabase != null)
        {
            //Let's do RNG Looping for each Loot List from Monster
            foreach (var ItemDropTable in MonsterFromDatabase.LootLists)
            {
                Random rand = new Random();
                if (ItemDropTable.RNGChance > rand.Next(0, 100))
                {
                    DroppedItemCredential.Add(ItemDropTable.ItemDrop.PlayFabItemID);
                }
            }
        }
    }

    //Get EXP Logic
    public static void GetEXPLogic(List<NBMonBattleDataSave> PlayerTeam, List<int> AllMonsterEXPMemoryStorage)
    {
        foreach(var Monster in PlayerTeam)
        {
            int ThisMonsterEXPStorage = new int();

            if(!Monster.fainted)
            {
                ThisMonsterEXPStorage = Monster.expMemoryStorage;
                AllMonsterEXPMemoryStorage.Add(ThisMonsterEXPStorage);
                InsertEXPToMonster(Monster);
            }
            else
            {
                AllMonsterEXPMemoryStorage.Add(0);
                Monster.expMemoryStorage = 0;
            }
        }
    }

    //Get EXP Logic to Individual Monster
    public static void InsertEXPToMonster(NBMonBattleDataSave Monster)
    {
        //Add Monster Current exp using EXP Memory Storage
        Monster.currentExp += Monster.expMemoryStorage;

        //Reset this monster's EXP Memory Storage
        Monster.expMemoryStorage = 0;
            
        //Check if this monster Level Up.
        if(Monster.currentExp >= Monster.nextLevelExpRequired)
        {
            Monster.currentExp -= Monster.nextLevelExpRequired;
            MonsterLevelUp(Monster);
        }
    }

    //Check Level Up
    public static void MonsterLevelUp(NBMonBattleDataSave Monster)
    {
        //Check if Max Level Reached
        if(Monster.level >= AttackFunction.MaxLevel)
        {
            Monster.currentExp = 0;
            Monster.expMemoryStorage = 0;
            return;
        }

        //Add Level to this Monster
        Monster.level += 1;
        Monster.expMemoryStorage = 0;
        
        //Calculate This Monster Stats
        NBMonStatsCalculation.CalculateNBMonStatsAfterLevelUp(Monster);

        //Check if This Monster's Current EXP is enough to Level Up again
        if(Monster.currentExp >= Monster.nextLevelExpRequired)
        {
            Monster.currentExp -= Monster.nextLevelExpRequired;
            MonsterLevelUp(Monster);
        }
    }
    

    //Lost Logic
    public static void DoLostLogic(List<NBMonBattleDataSave> PlayerTeam)
    {
        
    }

    //Revive Player Logic and Reset Status Effect
    public static void RevivePlayer(List<NBMonBattleDataSave> PlayerTeam)
    {
        foreach(var Monster in PlayerTeam)
        {
            Monster.statusEffectList.Clear();
            Monster.temporaryPassives.Clear();

            if(Monster.fainted)
            {
                Monster.hp = 1;
                Monster.fainted = false;
            }
        }
    }

    //Class for Data Sent to Unity
    public class EXPDataAndItemDropData
    {
        public List<int> EXPDatas;
        public List<string> ItemPlayFabCredentialDatas;
    }

    //Cloud Function
    [FunctionName("BattleFinished")]
    public static async Task<dynamic> BattleFinishedAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
        new GetUserDataRequest { 
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam"}
        } );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        int BattleResult = new int();
        List<string> DroppedItemCredential = new List<string>();
        List<int> AllMonsterEXPMemoryStorage = new List<int>();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);

        //Get Battle Result (as Integer)
        //BattleResult = 1, Indicating Win
        //BattleResult = 0, Indicating Draw / Escape / Lost
        if(args["BattleResult"] != null)
            BattleResult = (int)args["BattleResult"];

        //Win Case
        //Azure needs to run a logic for Item Drop
        if(BattleResult == 1)
        {
            DoWinLogic(PlayerTeam, EnemyTeam, DroppedItemCredential, AllMonsterEXPMemoryStorage);
        }

        //Escape or Draw or Lost Case
        if(BattleResult == 0)
        {
            DoLostLogic(PlayerTeam);
        }

        //Revive Fallen NBMon Players After Battle (Regardless of Battle Finished State)
        RevivePlayer(PlayerTeam);

        //If DroppedItemCredential exist, let's call Azure Function to grant item based on Item Drops.
        if(DroppedItemCredential.Count != 0)
        {        
            var request = await serverApi.GrantItemsToUserAsync(new GrantItemsToUserRequest
        {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            ItemIds = DroppedItemCredential,
            CatalogVersion = "InventoryTest" });        
        }

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                }
            }
        );

        //Sent Data if The Battle Result is Winning
        if(BattleResult == 1)
        {
            EXPDataAndItemDropData DataSentToClient = new EXPDataAndItemDropData();

            DataSentToClient.EXPDatas = AllMonsterEXPMemoryStorage;
            DataSentToClient.ItemPlayFabCredentialDatas = DroppedItemCredential;

            //Convert it to JsonString
            var JsonString = JsonConvert.SerializeObject(DataSentToClient);

            //Sent this to Client
            return JsonString;
        }
        else
        {
            return null;
        }
    }

}