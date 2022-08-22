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
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

public static class UseItem
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

    //UseItem Input
    public class UseItemDataInput
    {
        public string MonsterUniqueID;
        public string ItemName;
    }

    //Find Item Method
    public static ItemsPlayFab FindItem(string ItemName)
    {
        //==========================================================
        //MongoDB Code
        //==========================================================
        //Default Setting to call MongoDB.
        MongoDBTest.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(MongoDBTest.settings);

        //Setting to call Database
        var database = client.GetDatabase("myFirstDatabase");

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("Name", ItemName);
        //Setting for Collection
        var collection = database.GetCollection<BsonDocument>("itemData").Find(filter).FirstOrDefault().AsEnumerable();;

        ItemsPlayFab newItem = new ItemsPlayFab();

        //Convert the Result into desire Class
        newItem = BsonSerializer.Deserialize<ItemsPlayFab>(collection.ToBsonDocument());
        return newItem;

        //==========================================================
        //Original Code
        //==========================================================
        //Get ItemDatabase
        // InventoryItemsPlayFabLists ItemDatabase = JsonConvert.DeserializeObject<InventoryItemsPlayFabLists>(ItemDatabaseJson.ItemDataJson);
        // //Looping ItemDatabase
        // foreach (var Item in ItemDatabase.ItemDataBasePlayFab)
        // {
        //     if(Item.Name == ItemName)
        //     {
        //         return Item;
        //     }
        // }
        // return null;
    }

    public static string FindPlayFabItemID(string ItemName)
        {
        //Get ItemDatabase
        InventoryItemsPlayFabLists ItemDatabase = JsonConvert.DeserializeObject<InventoryItemsPlayFabLists>(ItemDatabaseJson.ItemDataJson);

        for(int i = 0; i < ItemDatabase.ItemDataBasePlayFab.Count; i++)
        {
            if(ItemDatabase.ItemDataBasePlayFab[i].Name == ItemName)
            {
                return i.ToString("000000");
            }
        }

        return string.Empty;
    }

    //Find Corresponding NBMon
    public static NBMonBattleDataSave FindMonster(string UniqueID, List<NBMonBattleDataSave> TeamList)
    {
        foreach(var Monster in TeamList)
        {
            if(Monster.uniqueId == UniqueID)
            {
                return Monster;
            }
        }

        return null;
    }

    //Find This Monster Status Effect
    public static StatusEffectList FindNBMonStatusEffect(NBMonBattleDataSave ThisMonster, NBMonProperties.StatusEffectInfo StatusEffectInfo)
    {
        foreach(var StatusInMonster in ThisMonster.statusEffectList)
        {
            if(StatusInMonster.statusEffect == (int)StatusEffectInfo.statusEffect && StatusInMonster.counter > 0)
            {
                return StatusInMonster;
            }
        }

        //If none found, return False.
        return null;
    }

    //Add Status Effect
    public static void ApplyStatusEffect(NBMonBattleDataSave ThisMonster, List<NBMonProperties.StatusEffectInfo> statusEffectInfoList, ILogger log = null, bool DoNotApplyPassive = false, DocumentClient client = null)
    {
        if(log != null)
            log.LogInformation($"First Step! Code A");
        
        //If there is no existing opposite status start by adding new data
        foreach (var statusEffectInfo in statusEffectInfoList)
        {
            if(log != null)
                log.LogInformation($"First Loop Step! Code B: {statusEffectInfo.statusEffect}");

            //Make RNG Chances vary between Loop
            Random Rand = new Random();
            var RNG = Rand.Next(0, 100);

            if(log != null)
                log.LogInformation($"Second Loop Step! Code C: RNG Value {RNG}");

            // Store variables related with the status effect
            bool statusEffectExist = FindNBMonStatusEffect(ThisMonster, statusEffectInfo) != null;
            var ThisMonsterStatusEffect = FindNBMonStatusEffect(ThisMonster, statusEffectInfo);

            if(log != null)
                log.LogInformation($"Third Loop Step! Code D: Status Effect Exist? {statusEffectExist} / Status Effect {ThisMonsterStatusEffect}");

            bool ElementImmunity = FindStatusEffectFromDatabase((int)statusEffectInfo.statusEffect).elementImmunity;

            NBMonDatabase.MonsterInfoPlayFab MonsterData = NBMonDatabase.FindMonster(ThisMonster.monsterId);

            if(log != null)
                log.LogInformation($"4th Loop Step! Code E: Element Immunity {ElementImmunity} / Monster Data {MonsterData}");

            bool MonsterImmune = MonsterData.elements.Contains(FindStatusEffectFromDatabase((int)statusEffectInfo.statusEffect).immuneAgainstElement);

            //Status Effect Immunity based on Monster's Element.
            if(ElementImmunity)
            {
                //Find Monster Data First and Check if the Monster contains the Element which make the Status Effect unaffected by this Monster.
                if(MonsterImmune)
                {
                   continue; 
                }
            }

            //Status Effect RNG
            if (RNG > statusEffectInfo.triggerChance)
            {
                //Indicating the Apply Status Effect failes because the RNG Value is higher then the chance
                continue;
            }

            if (!statusEffectExist) //If the Status Effect is not inside This Monster.
            {
                //Add new status effect if the NBMon don't have this status effect
                AddNewStatusEffect(statusEffectInfo.statusEffect, statusEffectInfo.countAmmount, statusEffectInfo.stackAmount, ThisMonster);
            }
            else if (statusEffectExist) //If the Status Effect already inside This Monster
            {
                if(log != null)
                    log.LogInformation($"5th Loop Step! Code F: Modify Status Effect Value!");

                //Check if the Status Effect is Stackable
                ModifyStatusEffectValue(statusEffectInfo.statusEffect, statusEffectInfo.countAmmount, statusEffectInfo.stackAmount, statusEffectInfo, ThisMonsterStatusEffect, ThisMonster, client);
            }
           
            //Check if the function does not want to Apply Passive (used to avoid Infinite Loop Error).
            if(!DoNotApplyPassive)
            {
                if(log != null)
                    log.LogInformation($"6th Loop Step! Code G: Calling Apply Passive");

                //Apply passives that works when received status effect.
                PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.StatusConditionReceiving, PassiveDatabase.TargetType.originalMonster, ThisMonster, null, null);
            }
        }

        if(log != null)
            log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} Add Status Effect has been Called!");
    }

    //Find Status Effect from Database
    public static StatusEffectIconDatabase.StatusConditionDataPlayFab FindStatusEffectFromDatabase(int StatusEffectInt, DocumentClient client = null)
    {
        //Get Status Effect Database Database
        var StatusEffectJsonString = StatusEffectDatabaseJson.StatusEffectDataJson;
        var StatusEffectDatabase = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(StatusEffectJsonString);

        //If client exists, run Cosmos DB query
        if(client != null){
            //Declare Variable for Cosmos DB
            var option = new FeedOptions() { EnableCrossPartitionQuery = true };
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "StatusEffect");

            StatusEffectIconDatabase.StatusConditionDataPlayFab UsedData = new StatusEffectIconDatabase.StatusConditionDataPlayFab();
            UsedData = client.CreateDocumentQuery<StatusEffectIconDatabase.StatusConditionDataPlayFab>(collectionUri, $"SELECT * FROM db WHERE db.statusConditionName = {StatusEffectInt}", option).AsEnumerable().FirstOrDefault();

            //If data found return it
            if(UsedData != null)
                return UsedData;
            
            return null;
        }

        foreach (var StatusEffect in StatusEffectDatabase.statusConditionDatabasePlayFab)
        {
            if((int)StatusEffect.statusConditionName == StatusEffectInt)
            {
                return StatusEffect;
            }
        }

        //else return null.
        return null;
    }

    //Add New Status Effect to ThisMonster
    public static void AddNewStatusEffect(NBMonProperties.StatusEffect statusEffect, int count, int stacks, NBMonBattleDataSave ThisMonster)
    {
        //Declare New Variable
        StatusEffectList AddNewStatus = new StatusEffectList();

        //Insert New Data according to the Statement from this Function.
        AddNewStatus.statusEffect = (int) statusEffect;
        AddNewStatus.counter = count;
        AddNewStatus.stacks = stacks;

        //Add Monster's Status Effect into It's List.
        ThisMonster.statusEffectList.Add(AddNewStatus);
    }

    //Add New Status Effect to ThisMonster
    public static void ModifyStatusEffectValue(NBMonProperties.StatusEffect statusEffect, int count, int stacks, NBMonProperties.StatusEffectInfo statusEffectInfo, StatusEffectList thisMonsterStatusEffect, NBMonBattleDataSave ThisMonster, DocumentClient client = null)
    {
        //Declare Variables
        var StatusEffectFromDatabase = FindStatusEffectFromDatabase((int)statusEffectInfo.statusEffect);
        int MaximumStacks = StatusEffectFromDatabase.maxStacks;
        bool Stackable = StatusEffectFromDatabase.stackable;

        //Add Duration to the Status Effect (up to it's original value)
        thisMonsterStatusEffect.counter = count;

        //Check if the Status Effect can Stack
        if(Stackable)
        {
            thisMonsterStatusEffect.stacks += stacks;

            if(thisMonsterStatusEffect.stacks > MaximumStacks)
            {
                thisMonsterStatusEffect.stacks = MaximumStacks;
            }
        }
    }

    //Remove Status Effect
    public static void RemoveStatusEffect(NBMonBattleDataSave ThisMonster, List<NBMonProperties.StatusEffectInfo> statusEffectInfoList, ILogger log = null)
    {
        foreach (var statusEffectInfo in statusEffectInfoList)
        {
            for (int i = ThisMonster.statusEffectList.Count - 1; i >= 0; i--)
            {
                if ((int)statusEffectInfo.statusEffect == ThisMonster.statusEffectList[i].statusEffect)
                {
                    ThisMonster.statusEffectList.RemoveAt(i);
                }
            }
        }

        if(log != null)
            log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} Remove Status Effect has been Called!");
    }

    

    //Cloud Function Method With Cosmos DB
    [FunctionName("UseItem")]
    public static async Task<dynamic> UseItemAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = null)]HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "BattleEnvironment"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave ThisMonster = new NBMonBattleDataSave();
        UseItemDataInput ConvertedInputData = new UseItemDataInput();
        ItemsPlayFab UsedItem = new ItemsPlayFab();
        dynamic UseItemInputValue = null;
        bool NonCombat = new bool();

        //Check args["UseItemInput"] if it's null or not
        if(args["UseItemInput"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            UseItemInputValue = args["UseItemInput"];

            //Change from Dynamic to String
            string UseItemInputValueString = UseItemInputValue;

            //Convert that argument into Input variable.
            ConvertedInputData = JsonConvert.DeserializeObject<UseItemDataInput>(UseItemInputValueString);
        }

        //Check it UseItem is called on Non Combat.
        if(args["NonCombat"] != null)
            NonCombat = true;
        else
            NonCombat = false;

        //Convert from json to NBmonBattleDataSave and Other Type Data (String for Battle Environment).
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        
        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Send Data to Static Team Data
        NBMonTeamData.PlayerTeam = PlayerTeam;
        NBMonTeamData.EnemyTeam = EnemyTeam;

        //Find Item using Cosmos DB query API
        UsedItem = FindItem(ConvertedInputData.ItemName);

        log.LogInformation($"Is Item {ConvertedInputData.ItemName} Found? {UsedItem != null}");

        //When UsedItem is not Null, Let's recover their stats.
        if(UsedItem != null)
        {
            //Find This Monster
            ThisMonster = FindMonster(ConvertedInputData.MonsterUniqueID, PlayerTeam);

            //Setup HP's Tooltip
            int TotalHPRecovery = UsedItem.HPRecovery + (int)Math.Floor((float)UsedItem.HPRecovery_Percentage/100f * (float)ThisMonster.maxHp);
            int TotalEnergyRecovery = UsedItem.EnergyRecover + (int)Math.Floor((float)UsedItem.EnergyRecover_Percentage/100f * (float)ThisMonster.energy);

            log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} found!");

            //If This Monster is Not NULL
            if(ThisMonster != null)
            {
                //Recover HP
                ThisMonster.hp += TotalHPRecovery;
                if(ThisMonster.hp > ThisMonster.maxHp)
                    ThisMonster.hp = ThisMonster.maxHp;

                log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} HP Recovered!");

                //Energy Recovery
                ThisMonster.energy += TotalEnergyRecovery;
                if(ThisMonster.energy > ThisMonster.maxEnergy)
                    ThisMonster.energy = ThisMonster.maxEnergy;

                log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} Energy Recovered!");

                if(!NonCombat) //Apply Status Effect is only called during Combat.
                {
                    //Add Status Effect
                    ApplyStatusEffect(ThisMonster, UsedItem.AddStatusEffects, log, false);
                }

                //Remove Status Effect
                RemoveStatusEffect(ThisMonster, UsedItem.RemovesStatusEffects, log);

                //return JsonConvert.SerializeObject(PlayerTeam);
                var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
                        PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, 
                        Data = new Dictionary<string, string>{ {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)}
                    }
                });

                return "Item Usage Success";
            }
            else
            {
                return "Monster Not Found!";
            }
        }
        else
        {
            return "Item Data Not Found!";
        }
    }

    //Cloud Function Method With Cosmos DB
    /*
    [FunctionName("UseItemCosmos")]
    public static async Task<dynamic> UseItemAzureCosmos([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = null)]HttpRequest req,
    [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "BattleEnvironment"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave ThisMonster = new NBMonBattleDataSave();
        UseItemDataInput ConvertedInputData = new UseItemDataInput();
        ItemsPlayFab UsedItem = new ItemsPlayFab();
        dynamic UseItemInputValue = null;
        bool NonCombat = new bool();

        //Declare Variable For Cosmos Usage
        var option = new FeedOptions(){ EnableCrossPartitionQuery = true };
        Uri collectionUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "ItemsData");

        //Check args["UseItemInput"] if it's null or not
        if(args["UseItemInput"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            UseItemInputValue = args["UseItemInput"];

            //Change from Dynamic to String
            string UseItemInputValueString = UseItemInputValue;

            //Convert that argument into Input variable.
            ConvertedInputData = JsonConvert.DeserializeObject<UseItemDataInput>(UseItemInputValueString);
        }

        //Check it UseItem is called on Non Combat.
        if(args["NonCombat"] != null)
            NonCombat = true;
        else
            NonCombat = false;

        //Convert from json to NBmonBattleDataSave and Other Type Data (String for Battle Environment).
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        
        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Send Data to Static Team Data
        NBMonTeamData.PlayerTeam = PlayerTeam;
        NBMonTeamData.EnemyTeam = EnemyTeam;

        //Find Item using Cosmos DB query API
        UsedItem = client.CreateDocumentQuery<ItemsPlayFab>(collectionUri, $"SELECT * FROM db WHERE db.Name = '{ConvertedInputData.ItemName}'",option).AsEnumerable().FirstOrDefault();

        log.LogInformation($"Is Item {ConvertedInputData.ItemName} Found? {UsedItem != null}");

        //When UsedItem is not Null, Let's recover their stats.
        if(UsedItem != null)
        {
            //Find This Monster
            ThisMonster = FindMonster(ConvertedInputData.MonsterUniqueID, PlayerTeam);

            //Setup HP's Tooltip
            int TotalHPRecovery = UsedItem.HPRecovery + (int)Math.Floor((float)UsedItem.HPRecovery_Percentage/100f * (float)ThisMonster.maxHp);
            int TotalEnergyRecovery = UsedItem.EnergyRecover + (int)Math.Floor((float)UsedItem.EnergyRecover_Percentage/100f * (float)ThisMonster.energy);

            log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} found!");

            //If This Monster is Not NULL
            if(ThisMonster != null)
            {
                //Recover HP
                ThisMonster.hp += TotalHPRecovery;
                if(ThisMonster.hp > ThisMonster.maxHp)
                    ThisMonster.hp = ThisMonster.maxHp;

                log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} HP Recovered!");

                //Energy Recovery
                ThisMonster.energy += TotalEnergyRecovery;
                if(ThisMonster.energy > ThisMonster.maxEnergy)
                    ThisMonster.energy = ThisMonster.maxEnergy;

                log.LogInformation($"Monster {ThisMonster.nickName} with {ThisMonster.uniqueId} Energy Recovered!");

                if(!NonCombat) //Apply Status Effect is only called during Combat.
                {
                    //Add Status Effect
                    ApplyStatusEffect(ThisMonster, UsedItem.AddStatusEffects, log, false, client);
                }

                //Remove Status Effect
                RemoveStatusEffect(ThisMonster, UsedItem.RemovesStatusEffects, log);

                //return JsonConvert.SerializeObject(PlayerTeam);
                var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
                        PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, 
                        Data = new Dictionary<string, string>{ {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)}
                    }
                });

                return "Item Usage Success";
            }
            else
            {
                return "Monster Not Found!";
            }
        }
        else
        {
            return "Item Data Not Found!";
        }
    }*/
}