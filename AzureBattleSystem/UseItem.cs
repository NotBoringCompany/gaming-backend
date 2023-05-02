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
using static InitialTeamSetup;

public static class UseItem
{
    

    //UseItem Input
    public class UseItemDataInput
    {
        public string InputMonsterUniqueID;
        public string UsedMonsterUniqueID;
        public string ItemName;
    }

    //Find Item Method
    public static ItemsPlayFab FindItem(string ItemName)
    {
        //==========================================================
        //MongoDB Code
        //==========================================================
        //Default Setting to call MongoDB.
        // MongoDBTest.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // var client = new MongoClient(MongoDBTest.settings);

        // //Setting to call Database
        // var database = client.GetDatabase("myFirstDatabase");

        // //Let's create a filter to query single data
        // var filter = Builders<BsonDocument>.Filter.Eq("Name", ItemName);
        // //Setting for Collection
        // var collection = database.GetCollection<BsonDocument>("itemData").Find(filter).FirstOrDefault().AsEnumerable();;

        // ItemsPlayFab newItem = new ItemsPlayFab();

        // //Convert the Result into desire Class
        // newItem = BsonSerializer.Deserialize<ItemsPlayFab>(collection.ToBsonDocument());
        // return newItem;

        //==========================================================
        //Original Code
        //==========================================================
        //Get ItemDatabase
        InventoryItemsPlayFabLists ItemDatabase = JsonConvert.DeserializeObject<InventoryItemsPlayFabLists>(ItemDatabaseJson.ItemDataJson);
        //Looping ItemDatabase
        foreach (var Item in ItemDatabase.ItemDataBasePlayFab)
        {
            if(Item.Name == ItemName)
            {
                return Item;
            }
        }
        return null;
    }

    public static int FindItemIndex(string ItemName)
    {
        InventoryItemsPlayFabLists ItemDatabase = JsonConvert.DeserializeObject<InventoryItemsPlayFabLists>(ItemDatabaseJson.ItemDataJson);
        //Looping ItemDatabase
        foreach (var Item in ItemDatabase.ItemDataBasePlayFab)
        {
            if(Item.Name == ItemName)
            {
                return ItemDatabase.ItemDataBasePlayFab.LastIndexOf(Item);
            }
        }
        
        return -1;
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
    public static NBMonBattleDataSave FindMonster(string UniqueID, List<NBMonBattleDataSave> TeamList, InitialTeamSetup.HumanBattleData humanBattleData)
    {
        foreach(var Monster in TeamList)
        {
            if(Monster.uniqueId == UniqueID)
            {
                return Monster;
            }
        }

        if(humanBattleData != null)
        {
            if(UniqueID == humanBattleData.playerHumanData.uniqueId)
                return humanBattleData.playerHumanData;

            if(humanBattleData.enemyHumanData != null) //Check if the enemyHumanData is null or not.
                if(UniqueID == humanBattleData.enemyHumanData.uniqueId)
                    return humanBattleData.enemyHumanData;
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
    public static void ApplyStatusEffect(NBMonBattleDataSave thisMonster, List<NBMonProperties.StatusEffectInfo> statusEffects, ILogger logger = null, bool skipPassive = false, RNGSeedClass seed = null)
{
    foreach (var statusEffect in statusEffects)
    {
        var rng = EvaluateOrder.ConvertSeedToRNG(seed);

        var existingStatusEffect = FindNBMonStatusEffect(thisMonster, statusEffect);
        var statusEffectExists = existingStatusEffect != null;
        var elementImmunity = FindStatusEffectFromDatabase((int)statusEffect.statusEffect).elementImmunity;

        var monsterData = NBMonDatabase.FindMonster(thisMonster.monsterId);
        var monsterImmune = monsterData.elements.Contains(FindStatusEffectFromDatabase((int)statusEffect.statusEffect).immuneAgainstElement);

        if (elementImmunity && monsterImmune)
        {
            continue;
        }

        if (rng > statusEffect.triggerChance)
        {
            continue;
        }

        if (!statusEffectExists)
        {
            AddNewStatusEffect(statusEffect.statusEffect, statusEffect.countAmmount, statusEffect.stackAmount, thisMonster);
        }
        else
        {
            ModifyStatusEffectValue(statusEffect.statusEffect, statusEffect.countAmmount, statusEffect.stackAmount, statusEffect, existingStatusEffect, thisMonster);
        }

        if (!skipPassive)
        {
            PassiveLogic.ApplyPassive(PassiveDatabase.ExecutionPosition.ReceiveStatusEffect, PassiveDatabase.TargetType.originalMonster, thisMonster, null, null, seed);
        }
    }
}


    //Find Status Effect from Database
    public static StatusEffectIconDatabase.StatusConditionDataPlayFab FindStatusEffectFromDatabase(int StatusEffectInt)
    {
        //============================================================
        // MongoDB Logic
        //============================================================
        // //Default Setting to call MongoDB.
        // MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // //Let's create a filter to query single data
        // var filter = Builders<BsonDocument>.Filter.Eq("statusConditionName", StatusEffectInt);
        // //Setting for Collection
        // var collection = MongoHelper.db.GetCollection<BsonDocument>("statusEffectData").Find(filter).FirstOrDefault().AsEnumerable();
        // var newData = new StatusEffectIconDatabase.StatusConditionDataPlayFab();
        // //Convert the Result into desire Class
        // newData = BsonSerializer.Deserialize<StatusEffectIconDatabase.StatusConditionDataPlayFab>(collection.ToBsonDocument());

        // return newData;

        //============================================================
        // ORIGINAL Logic
        //============================================================
        //Get Status Effect Database Database
        var StatusEffectJsonString = StatusEffectDatabaseJson.StatusEffectDataJson;
        var StatusEffectDatabase = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(StatusEffectJsonString);

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
    public static void ModifyStatusEffectValue(NBMonProperties.StatusEffect statusEffect, int count, int stacks, NBMonProperties.StatusEffectInfo statusEffectInfo, StatusEffectList thisMonsterStatusEffect, NBMonBattleDataSave thisMonster)
    {
        var statusEffectFromDatabase = FindStatusEffectFromDatabase((int)statusEffectInfo.statusEffect);
        int maxStacks = statusEffectFromDatabase.maxStacks;
        bool stackable = statusEffectFromDatabase.stackable;

        if (thisMonsterStatusEffect.counter < count)
            thisMonsterStatusEffect.counter = count;

        if (stackable)
        {
            thisMonsterStatusEffect.stacks += stacks;

            if (thisMonsterStatusEffect.stacks > maxStacks)
                thisMonsterStatusEffect.stacks = maxStacks;
        }
    }


    //Remove Status Effect
    public static void RemoveStatusEffect(NBMonBattleDataSave thisMonster, List<NBMonProperties.StatusEffectInfo> statusEffectInfoList)
    {
        foreach (var statusEffectInfo in statusEffectInfoList)
        {
            thisMonster.statusEffectList.RemoveAll(s => (int)statusEffectInfo.statusEffect == s.statusEffect);
        }
    }


    public static void RemoveStatusEffectByType(NBMonBattleDataSave thisMonster, SkillsDataBase.RemoveStatusEffectType statusType)
    {
        thisMonster.statusEffectList.RemoveAll(statusEffect =>
        {
            var statusEffectData = UseItem.FindStatusEffectFromDatabase(statusEffect.statusEffect);
            return statusEffectData.statusEffectType == statusType;
        });
    }

    public static void RemoveAllStatusEFfect(NBMonBattleDataSave thisMonster)
    {
        thisMonster.statusEffectList.Clear();
    }

    //Cloud Function Method
    [FunctionName("UseItem")]
    public static async Task<dynamic> UseItemAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = null)]HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "BattleEnvironment", "RNGSeeds", "SortedOrder", "HumanBattleData"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> playerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave thisMonster = new NBMonBattleDataSave();
        UseItemDataInput convertedInputData = new UseItemDataInput();
        ItemsPlayFab usedItem = new ItemsPlayFab();
        RNGSeedClass seedClass = new RNGSeedClass();
        dynamic useItemInputValue = null;
        List<string> sortedOrder = new List<string>();
        bool nonCombat = new bool();
        HumanBattleData humanBattleData = new HumanBattleData();

        //Check args["UseItemInput"] if it's null or not
        if(args["UseItemInput"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            useItemInputValue = args["UseItemInput"];

            //Change from Dynamic to String
            string UseItemInputValueString = useItemInputValue;

            //Convert that argument into Input variable.
            convertedInputData = JsonConvert.DeserializeObject<UseItemDataInput>(UseItemInputValueString);
        }

        //Check it UseItem is called on Non Combat.
        if(args["NonCombat"] != null)
            nonCombat = true;
        else
            nonCombat = false;

        //Convert from json to NBmonBattleDataSave and Other Type Data (String for Battle Environment).
        playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        seedClass = JsonConvert.DeserializeObject<RNGSeedClass>(requestTeamInformation.Result.Data["RNGSeeds"].Value);
        sortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);
        humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);
        
        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Send Data to Static Team Data
        NBMonTeamData.PlayerTeam = playerTeam;
        NBMonTeamData.EnemyTeam = enemyTeam;

        //Find Item
        usedItem = FindItem(convertedInputData.ItemName);

        log.LogInformation($"Is Item {convertedInputData.ItemName} Found? {usedItem != null}");

        //Check if monster can use item
        var monsterCanMove = EvaluateOrder.CheckBattleOrder(sortedOrder, convertedInputData.InputMonsterUniqueID);

        if(!monsterCanMove)
        {
            return $"No Monster in the turn order. Error Code: RH-0001";
        }

        if (usedItem == null)
        {
            return "Item Data Not Found!";
        }

        // Find the monster that used the item
        thisMonster = FindMonster(convertedInputData.UsedMonsterUniqueID, playerTeam, humanBattleData);

        // If the monster isn't on player team 1, check team 2
        if (thisMonster == null)
        {
            thisMonster = FindMonster(convertedInputData.UsedMonsterUniqueID, enemyTeam, humanBattleData);
        }

        // If the monster still isn't found, return an error message
        if (thisMonster == null)
        {
            return "Monster Not Found!";
        }

        // Recover HP and energy
        int TotalHPRecovery = usedItem.HPRecovery + (int)Math.Floor((float)usedItem.HPRecovery_Percentage / 100f * (float)thisMonster.maxHp);
        int TotalEnergyRecovery = usedItem.EnergyRecover + (int)Math.Floor((float)usedItem.EnergyRecover_Percentage / 100f * (float)thisMonster.maxEnergy);
        thisMonster.hp += TotalHPRecovery;

        if (thisMonster.hp > thisMonster.maxHp)
        {
            thisMonster.hp = thisMonster.maxHp;
        }
        thisMonster.energy += TotalEnergyRecovery;
        if (thisMonster.energy > thisMonster.maxEnergy)
        {
            thisMonster.energy = thisMonster.maxEnergy;
        }

        // Apply and remove status effects if applicable
        if (!nonCombat)
        {
            ApplyStatusEffect(thisMonster, usedItem.AddStatusEffects, log, false, seedClass);
        }
            RemoveStatusEffect(thisMonster, usedItem.RemovesStatusEffects);
            AttackFunction.HardCodedRemoveStatusEffect(thisMonster, usedItem.removeStatusEffectType);

        // Serialize updated player data and return success message
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest
        {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            Data = new Dictionary<string, string>{
                {"SortedOrder", JsonConvert.SerializeObject(sortedOrder)},
                {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)},
                {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)}
            }
        });

        return "Item Usage Success";

    }
}