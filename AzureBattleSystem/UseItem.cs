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
        public string UserMonsterID;
        public string ItemName;
    }

    //Find Item Method
    public static ItemsPlayFab FindItem(string ItemName, InventoryItemsPlayFabLists Database)
    {
        //Looping ItemDatabase
        foreach (var Item in Database.ItemDataBasePlayFab)
        {
            if(Item.Name == ItemName)
            {
                return Item;
            }
        }

        return null;
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

/*
    //Add Status Effect
    public static void ApplyStatusEffect(NBMonBattleDataSave ThisMonster, List<NBMonProperties.StatusEffectInfo> statusEffectInfoList)
    {
        //Declare Database
        NBMonDatabase.MonstersPlayFabList MonsterDatabase = new NBMonDatabase.MonstersPlayFabList();
        StatusEffectIconDatabase.StatusConditionDatabasePlayFabList StatusEffectDatabase = new StatusEffectIconDatabase.StatusConditionDatabasePlayFabList();   

        //Get MonsterDatabase and Convert it from Json to Class
        var MonsterJsonData = NBMonDatabaseJson.MonsterDatabaseJson;
        MonsterDatabase = JsonConvert.DeserializeObject<NBMonDatabase.MonstersPlayFabList>(MonsterJsonData);

        //Get Passive Database
        var PassiveDatabaseJsonString = PassiveDatabaseJson.PassiveDataJson;
        StatusEffectDatabase = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(PassiveDatabaseJsonString);

        //If there is no existing opposite status start by adding new data
        foreach (var statusEffectInfo in statusEffectInfoList)
        {
            //Make RNG Chances vary between Loop
            Random Rand = new Random();
            var RNG = Rand.Next(0, 100);

            // Store variables related with the status effect
            bool statusEffectExist = FindNBMonStatusEffect(ThisMonster, statusEffectInfo) != null;

            //Get This Status Effect's Counter
            int statusEffectCounterMemory = FindNBMonStatusEffect(ThisMonster, statusEffectInfo).counter;

            //Status Effect Immunity based on Monster's Element.
            if(StatusEffectIconDatabase.FindStatusEffectIcon(statusEffectInfo.statusEffect).elementImmunity)
            {
                //Find Monster Data First and Check if the Monster contains the Element which make the Status Effect unaffected by this Monster.
                if(NBMonDatabase.FindMonster(ThisMonster.monsterId).elements.Contains(StatusEffectIconDatabase.FindStatusEffectIcon(statusEffectInfo.statusEffect).immuneAgainstElement))
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

            if (statusEffectCounterMemory > 0)
            {
                //Do the following when status effect counter memory is still larger than 0
                if (!statusEffectExist)
                {
                    //Add new status effect if the NBMon don't have this status effect
                    AddNewStatusEffect(statusEffectInfo.statusEffect, statusEffectCounterMemory, statusEffectInfo.stackAmount);
                }
                else if (statusEffectExist)
                {
                    if(MockReference_StatusEffect.FindStatusEffectIcon(statusEffectInfo.statusEffect).stackable)
                    {
                        //Add Stack Value
                        StatusEffectStackModifier(statusEffectInfo.statusEffect, statusEffectCounterMemory, statusEffectInfo.stackAmount, statusEffectInfo);
                    }

                    if(MockReference_StatusEffect.FindStatusEffectIcon(statusEffectInfo.statusEffect).durationStack)
                    {
                        //Add the counter if the status already exists
                        ModifyStatusCounter(statusEffectInfo.statusEffect, statusEffectCounterMemory, statusEffectInfo.stackAmount, statusEffectInfo);
                    }
                    else
                    {
                        //Modify Status Counter for Status Effect with it's Duration non stackable.
                        ModifyStatusCounterNonDurationStack(statusEffectInfo.statusEffect, statusEffectCounterMemory);
                    }
                }

            }

            //Apply passives that works when received status effect.
            PassiveLogic.instances.ApplyPassive(PassiveDatabase.ExecutionPosition.StatusConditionReceiving, PassiveDatabase.TargetType.originalMonster, monsterOnScreen, null, null);
        }
    }
*/

/*
    //Add New Status Effect to ThisMonster
    private static void AddNewStatusEffect(NBMonProperties.StatusEffect statusEffect, int count, int stacks, NBMonBattleDataSave ThisMonster)
    {
        //Add new status effec to the counter list group
        var newStatusEffectGroup = new NBMonProperties.StatusEffectCountGroup(statusEffect, count, stacks);

        ThisMonster.statusEffectList.Add(newStatusEffectGroup);
    }
*/

    //Remove Status Effect
    public static void RemoveStatusEffect(NBMonBattleDataSave ThisMonster, List<NBMonProperties.StatusEffectInfo> statusEffectInfoList)
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
    }

    //Cloud Methods
    [FunctionName("UseItem")]
    public static async Task<dynamic> UseItemAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        NBMonBattleDataSave ThisMonster = new NBMonBattleDataSave();
        InventoryItemsPlayFabLists ItemDatabase = new InventoryItemsPlayFabLists();
        ItemsPlayFab UsedItem = new ItemsPlayFab();
        dynamic UseItemInputValue = null;
        
        //Get ItemDatabase
        ItemDatabase = JsonConvert.DeserializeObject<InventoryItemsPlayFabLists>(ItemDatabaseJson.ItemDataJson);

        //Check args["UseItemInput"] if it's null or not
        if(args["UseItemInput"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            UseItemInputValue = args["UseItemInput"];

            //Change from Dynamic to String
            string UseItemInputValueString = UseItemInputValue;

            //Convert that argument into Input variable.
            UseItemInputValueString = JsonConvert.DeserializeObject<UseItemDataInput>(UseItemInputValue);
        }

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);

        //Find Item
        UsedItem = FindItem(UseItemInputValue.ItemName, ItemDatabase);

        //When UsedItem is not Null, Let's recover their stats.
        if(UsedItem != null)
        {
            //Setup HP's Tooltip
            int TotalHPRecovery = UsedItem.HPRecovery + (int)((float)UsedItem.HPRecovery_Percentage * (float)ThisMonster.maxHp);
            int TotalEnergyRecovery = UsedItem.EnergyRecover + (int)((float)UsedItem.EnergyRecover_Percentage * (float)ThisMonster.energy);

            //Find This Monster
            ThisMonster = FindMonster(UseItemInputValue.uniqueId, PlayerTeam);

            //If This Monster is Not NULL
            if(ThisMonster != null)
            {
                //Recover HP
                ThisMonster.hp += TotalHPRecovery;
                if(ThisMonster.hp > ThisMonster.maxHp)
                    ThisMonster.hp = ThisMonster.maxHp;

                //Energy Recovery
                ThisMonster.energy += TotalEnergyRecovery;
                if(ThisMonster.energy > ThisMonster.maxEnergy)
                    ThisMonster.energy = ThisMonster.maxEnergy;

                //Add Status Effect (TO DO, this is Hard)

                //Remove Status Effect
                RemoveStatusEffect(ThisMonster, UsedItem.RemovesStatusEffects);

                //Once Done, let's Save the new Player Data into PlayFab Database. But first, convert it into Json Datatbase
                string Team1String = JsonConvert.SerializeObject(PlayerTeam);

                var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, 
                    Data = new Dictionary<string, string>{ {"CurrentPlayerTeam", JsonConvert.SerializeObject(Team1String)}
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

        return null;
    }
}