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

    public static void ChangeMoraleGauge(MoraleData data, int damageData, bool isPlayerGauge)
    {
        if (isPlayerGauge)
        {
            data.playerMoraleGauge += damageData;
            data.playerMoraleGauge = Math.Max(0, Math.Min(100, data.playerMoraleGauge));
        }
        else
        {
            data.enemyMoraleGauge += damageData;
            data.enemyMoraleGauge = Math.Max(0, Math.Min(100, data.enemyMoraleGauge));
        }
    }
}
