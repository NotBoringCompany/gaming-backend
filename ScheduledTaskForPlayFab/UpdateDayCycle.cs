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
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;

public static class UpdateDayCycle
{
    public static PlayFabServerInstanceAPI SetupScheduledServerAPI()
    {
        var apiSettings = new PlayFabApiSettings
        {
            TitleId = "D91AF",
            DeveloperSecretKey = "P8MPZ9CZ35C4TOFDZM9XFASPNZNXBUEQ6X64T7HKZJ7CMKI6DM"
        };

        var authContext = new PlayFabAuthenticationContext
        {
            EntityId = string.Empty
        };

        return new PlayFabServerInstanceAPI(apiSettings, authContext);
    }

    [FunctionName("ColdStart")]
    public static async Task<dynamic> ColdStart([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

        //Setup serverAPI
        PlayFabServerInstanceAPI serverApi = SetupScheduledServerAPI();

        return "Cold Start Function";
    }

    [FunctionName("UpdateDayCount")]
    public static async Task<dynamic> UpdateDayCount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {    
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

        //Setup serverAPI
        PlayFabServerInstanceAPI serverApi = SetupScheduledServerAPI();

        //Declare Variables
        int dayCount = new int();

        //Get Title Data
        var readRequest = await serverApi.GetTitleDataAsync(new GetTitleDataRequest { Keys = new List<string>() {{"DayCount"}} });

        //Get "DayCount" data from Title Data
        if(readRequest.Result.Data.ContainsKey("DayCount"))
        {
            log.LogInformation($"Day Count: {readRequest.Result.Data["DayCount"]}");
            dayCount = int.Parse(readRequest.Result.Data["DayCount"]);
        }

        //Let's add it by 1.
        dayCount++;

        var updateRequest = await serverApi.SetTitleDataAsync(new SetTitleDataRequest
        {
            Key = "DayCount",
            Value = dayCount.ToString()
        });

        return null;
    }

    [FunctionName("UpdateWeekCount")]
    public static async Task<dynamic> UpdateWeekCount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {    
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

        //Setup serverAPI
        PlayFabServerInstanceAPI serverApi = SetupScheduledServerAPI();

        //Declare Variables
        int weekCount = new int();

        //Get Title Data
        var readRequest = await serverApi.GetTitleDataAsync(new GetTitleDataRequest { Keys = new List<string>() {{"WeekCount"}} });

        //Get "DayCount" data from Title Data
        if(readRequest.Result.Data.ContainsKey("WeekCount"))
        {
            log.LogInformation($"Week Count: {readRequest.Result.Data["WeekCount"]}");
            weekCount = int.Parse(readRequest.Result.Data["WeekCount"]);
        }

        //Let's add it by 1.
        weekCount++;

        var updateRequest = await serverApi.SetTitleDataAsync(new SetTitleDataRequest
        {
            Key = "WeekCount",
            Value = weekCount.ToString()
        });

        return null;
    }
}