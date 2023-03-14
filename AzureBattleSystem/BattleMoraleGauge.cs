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
using Azure;

public static class BattleMoraleGauge
{
    public class MoraleData
    {
        public int playerMoraleGauge;
        public int enemyMoraleGauge;
        public int playerMoraleUsageCount;
    }

    public static void ChangePlayerMoraleGauge(MoraleData data, int damageData)
    {
        data.playerMoraleGauge += damageData;

        if(data.playerMoraleGauge >= 100)
            data.playerMoraleGauge = 100;

        if(data.playerMoraleGauge < 0)
             data.playerMoraleGauge = 0;
    }

    public static void ChangeEnemyMoraleGauge(MoraleData data, int damageData)
    {
        data.enemyMoraleGauge += damageData;

        if(data.enemyMoraleGauge >= 100)
            data.enemyMoraleGauge = 100;
            
        if(data.enemyMoraleGauge < 0)
            data.enemyMoraleGauge = 0;
    }
}