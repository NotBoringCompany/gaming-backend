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

public static class CheckPlayerActivity
{
    

    [FunctionName("PlayerLoginCheck")]
    public static async Task<dynamic> PlayerLoginCheck([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Get Game's Title Data
        var reqTitleData = await serverApi.GetTitleDataAsync(new GetTitleDataRequest());

        var reqServerTime = await serverApi.GetTimeAsync(new GetTimeRequest());

        //Get User Title Data Information
        var reqReadOnlyData = await serverApi.GetUserReadOnlyDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //declare variables
        PlayerLoginData playerLoginData = new PlayerLoginData();
        int currentServerDayCount = int.Parse(reqTitleData.Result.Data["DayCount"]);

        //Check if the player has DayCountLastLogin data
        if(reqReadOnlyData.Result.Data.ContainsKey("DayCountLastLogin"))
            playerLoginData = JsonConvert.DeserializeObject<PlayerLoginData>(reqReadOnlyData.Result.Data["DayCountLastLogin"].Value);
        else
            playerLoginData.lastLoginDayCount = -1; //Indicates empty data

        var dictionary = new Dictionary<string, string>();

        if(playerLoginData.lastLoginDayCount != currentServerDayCount)
        {
            playerLoginData.lastLoginDayCount = currentServerDayCount;

            //Resets Defeated Monsters Objs ID and DemoWorldPickedItemData.
            dictionary.Add("DemoWorldPickedItemData", "[]");
            dictionary.Add("DefeatedMonsterObjs", "[]");
        }

        playerLoginData.loginTime = reqServerTime.Result.Time;

        //updatePlayerReadOnlyData about DayCountLastLogin and tell PlayFab this player has Logged In using PlayerLogin.
        var updatePlayerReadOnlyData = serverApi.UpdateUserReadOnlyDataAsync(
            new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>() 
                {
                    {"DayCountLastLogin", JsonConvert.SerializeObject(playerLoginData)},
                    {"PlayerLogin", "True"}
                }
            }
        );

        if(dictionary.Count > 0)
        {
            var updatePlayerData = serverApi.UpdateUserDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = dictionary
            });
        }

        return null;
    }

    [FunctionName("PlayerLogOutCheck")]
    public static async Task<dynamic> PlayerLogOutCheck([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Get Game's Title Data
        var reqTitleData = await serverApi.GetTitleDataAsync(new GetTitleDataRequest());

        var reqServerTime = await serverApi.GetTimeAsync(new GetTimeRequest());

        //Get User Read Only Title Data Information
        var reqReadOnlyData = await serverApi.GetUserReadOnlyDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Get User Internal Title Data Information
        var reqInternalData = await serverApi.GetUserInternalDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //declare variables
        PlayerLoginData playerLoginData = new PlayerLoginData();
        DailyPlayerData dailyTimeSpent = new DailyPlayerData();
        WeeklyPlayerData weeklyTimeSpent = new WeeklyPlayerData();
        DailyData newData = new DailyData();
        WeeklyData newWeekly = new WeeklyData();
        DateTime dailyLastLogout = new DateTime();
        int currentServerDayCount = int.Parse(reqTitleData.Result.Data["DayCount"]);
        int currentServerWeekCount = int.Parse(reqTitleData.Result.Data["WeekCount"]);

        //Check if the player has DayCountLastLogin data
        if(reqReadOnlyData.Result.Data.ContainsKey("DayCountLastLogin"))
        {
            playerLoginData = JsonConvert.DeserializeObject<PlayerLoginData>(reqReadOnlyData.Result.Data["DayCountLastLogin"].Value);
        }

        //Check if the player has DailyTimeSpent data
        if(reqReadOnlyData.Result.Data.ContainsKey("DailyTimeSpent"))
            dailyTimeSpent = JsonConvert.DeserializeObject<DailyPlayerData>(reqReadOnlyData.Result.Data["DailyTimeSpent"].Value);
        else
            dailyTimeSpent.uMasterId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId;

        //Check if the player has WeeklyTimeSpent data
        if(reqReadOnlyData.Result.Data.ContainsKey("WeeklyTimeSpent"))
            weeklyTimeSpent = JsonConvert.DeserializeObject<WeeklyPlayerData>(reqReadOnlyData.Result.Data["WeeklyTimeSpent"].Value);
        else
            weeklyTimeSpent.uMasterId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId;

        //Check the data if the player has ETH Adress or not.
        if(string.IsNullOrEmpty(dailyTimeSpent.uETHAddress) || string.IsNullOrEmpty(weeklyTimeSpent.uETHAddress))
        {
            if(reqInternalData.Result.Data.ContainsKey("ethAddress"))
            {
                dailyTimeSpent.uETHAddress = reqInternalData.Result.Data["ethAddress"].Value;
                weeklyTimeSpent.uETHAddress = reqInternalData.Result.Data["ethAddress"].Value;
            }
        }

        //Let's compare the player's last login data
        if(dailyTimeSpent.uDailyData.Count > 0)
        {
            //Let's get the DailyData from the latest data
            DailyData currentDailyData = dailyTimeSpent.uDailyData[dailyTimeSpent.uDailyData.Count-1];

            //Let's check it's day count
            if(currentDailyData.dayCount == currentServerDayCount)
            {
                dailyLastLogout = currentDailyData.date;
                currentDailyData.timeSpent += (reqServerTime.Result.Time).Subtract(currentDailyData.date);
                currentDailyData.date = reqServerTime.Result.Time;
            }
            else //Generate new data because this player latest data is outdated.
            {
                newData.dayCount = currentServerDayCount;
                newData.date = reqServerTime.Result.Time;
                newData.timeSpent = (reqServerTime.Result.Time).Subtract(playerLoginData.loginTime);
                dailyTimeSpent.uDailyData.Add(newData);
            }
        }
        else //Generate new data because this player has 0 data.
        {
            newData.dayCount = currentServerDayCount;
            newData.date = reqServerTime.Result.Time;
            newData.timeSpent = (reqServerTime.Result.Time).Subtract(playerLoginData.loginTime);
            dailyTimeSpent.uDailyData.Add(newData);
        }

        //Let's compare the player's weekly login data
        if(weeklyTimeSpent.uWeeklyData.Count > 0)
        {
            //Let's get the DailyData from the latest data
            WeeklyData currentWeeklyData = weeklyTimeSpent.uWeeklyData[weeklyTimeSpent.uWeeklyData.Count-1];

            //Let's check it's day count
            if(currentWeeklyData.weekCount == currentServerWeekCount)
            {
                currentWeeklyData.timeSpent += (reqServerTime.Result.Time).Subtract(dailyLastLogout);
                currentWeeklyData.date_weekly = reqServerTime.Result.Time;
            }
            else //Generate new data because this player latest data is outdated.
            {
                newWeekly.weekCount = currentServerWeekCount;
                newWeekly.date_weekly = reqServerTime.Result.Time;
                newWeekly.timeSpent = (reqServerTime.Result.Time).Subtract(playerLoginData.loginTime);
                weeklyTimeSpent.uWeeklyData.Add(newWeekly);
            }
        }
        else //Generate new data because this player has 0 data.
        {
            newWeekly.weekCount = currentServerWeekCount;
            newWeekly.date_weekly = reqServerTime.Result.Time;
            newWeekly.timeSpent = (reqServerTime.Result.Time).Subtract(playerLoginData.loginTime);
            weeklyTimeSpent.uWeeklyData.Add(newWeekly);
        }

        //Update dailyTimeSpent to PlayFab and make PlayerLogin data False.
        var updatePlayerReadOnlyData = serverApi.UpdateUserReadOnlyDataAsync(
            new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>() 
                {
                    {"DailyTimeSpent", JsonConvert.SerializeObject(dailyTimeSpent)},
                    {"WeeklyTimeSpent", JsonConvert.SerializeObject(weeklyTimeSpent)},
                    {"PlayerLogin", "False"}
                }
            }
        );

        return null;
    }
}