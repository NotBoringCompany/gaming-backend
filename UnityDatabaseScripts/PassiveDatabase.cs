using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

public class PassiveDatabase
{
    /// <summary>
    /// Stores the passive database list
    /// </summary>
    /// 

    [System.Serializable]
    public class PassiveInfoPlayFab
    {
        public ObjectId _id { get; set; }
        public string name;
        public string description;
        public List<PassiveDetail> passiveDetail;
    }
    public List<PassiveInfoPlayFab> passiveInfosPlayFab;

    public class PassiveInfosPlayFabList{
        public List<PassiveInfoPlayFab> passiveInfosPlayFab;
    }

    //Find the passive
    public PassiveInfoPlayFab FindPassive(string passiveName)
    {
        foreach (var passive in passiveInfosPlayFab)
        {
            if (passiveName == passive.name)
            {
                return passive;
            }
        }
        //Debug.Log("This Passive Do Not Exist" + passiveName);
        return null;
    }

    [System.Serializable]
    public class ElementalDamageReduction
    {
        public ElementDatabase.Elements SkillElementTaken;
        public int DamageReductionValue;
    }



    /// <summary>
    /// Properties related to the passive database
    /// </summary>
    [System.Serializable]
    public class PassiveDetail
    {
        public ExecutionPosition executionPosition;
        public List<Requirements> requirements;
        public List<EffectInfo> effect;
    }




    public enum ExecutionPosition
    {
        AtTheStartOfTheBattle,
        TurnStart,
        TurnEnd,
        WhenAttacking,
        WhenAttacked,
        AfterAttacked,
        ReceiveStatusEffect,
        OnDeath,
        WhenAttackedDuringCombat,
        InflictStatusEffect
    }


    public enum TargetType
    {
        originalMonster,
        targetedMonster,
        both
    }

    //Requirements to do the Modification
    [System.Serializable]
    public class Requirements
    {
        //monsterTargetingCheck, if this bool = true. the effect only works to Target NBMon.
        public bool monsterTargetingCheck;
        //monsterTargeting_EffectToSelf, if this bool is true while monsterTargetingCheck is also true, passive effect goes into the user instead target monster.
        public bool monsterTargeting_EffectToSelf;

        public RequirementTypes requirementTypes;

        //List of all the requirements logic, choose one
        public List<StatsValueIsRequired> statsValueIsRequired;
        public NBMonProperties.StatusEffect statusEffect;
        public SkillsDataBase.RemoveStatusEffectType statusRemovalRequirement;
        public List<string> skillLists;
        public List<string> EnvinromentLists;
        public ElementDatabase.Elements skillElement;
        
        public bool monsterElementImportant;
        public ElementDatabase.Elements originalMonsterElement;
        public ElementDatabase.Elements targetMonsterElement;
    }

    //Categorizing the requirement types to do the Modification
    public enum RequirementTypes
    {
        None,
        StatsValueReq,
        ActionUsingElement,
        ActionReceivingElement,
        ActionUsingTechnique,
        ActionReceivingTechnique,
        ActionReceivingStatusEffect,
        ActionGivingStatusEffect,
        HaveStatusEffect,
        Fainted,
        Environment
    }


    [System.Serializable]
    public class StatsValueIsRequired
    {
        public NBMonProperties.InputChange stats;
        public float value;
        public Numerator Numerator;
    }

    public enum Numerator
    {
        Same,
        BiggerThan,
        SmallerThan
    }

    //The Effect Information
    [System.Serializable]
    public class EffectInfo
    {
        public EffectType effectType;
        public NBMonProperties.StatsType statsType;
        public int valueChange;
        public List<NBMonProperties.StatusEffectInfo> statusEffectInfoList, removeStatusEffectInfoList, teamStatusEffectInfoList, removeTeamStatusEffectInfoList, enemyTeamStatusEffectInfoList, removeEnemyTeamStatusEffectInfoList;
        public SkillsDataBase.RemoveStatusEffectType statusRemoveType_Self, statusRemoveType_Allies, statusRemoveType_Enemies;
        public int triggerChance;
        public int attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses;
        public int energyShield, surviveLethalBlow, totalIgnoreDefense, mustCritical, immuneCritical;
        public int damageReduction, energyShieldValue, absorbDamageValue;
        public List<ElementalDamageReduction> ElementalDamageReductions;
        public List<string> newTemporaryPassives;

    }

    //How to Modify the Passive
    public enum EffectType
    {
        None,
        StatsPercentage,
        StatusEffect,
        Stats,
        DuringBattle,
        ApplySelfTemporaryPassive
    }

    public class ModifierStatus
    {
        public NBMonProperties.StatusEffect statusCondition;
    }

    //Find Skill
    public static PassiveInfoPlayFab FindPassiveSkill(string passiveName)
    {
        // //============================================================
        // // MONGODB Logic
        // //============================================================
        // MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // //Let's create a filter to query single data
        // var filter = Builders<BsonDocument>.Filter.Eq("name", passiveName);
        // //Setting for Collection
        // var collection = MongoHelper.db.GetCollection<BsonDocument>("passiveData").Find(filter).FirstOrDefault().AsEnumerable();
        // var newData = new PassiveInfoPlayFab();

        // //Convert the Result into desire Class
        // newData = BsonSerializer.Deserialize<PassiveInfoPlayFab>(collection.ToBsonDocument());
        // return newData;
        
        //============================================================
        // ORIGINAL Logic
        //============================================================    
        string PassiveDatabaseJsonString = PassiveDatabaseJson.PassiveDataJson;
        PassiveInfosPlayFabList ConvertedPassiveDatabase = JsonConvert.DeserializeObject<PassiveInfosPlayFabList>(PassiveDatabaseJsonString);

        //Loop to find passive 1 by 1.
        foreach (var passive in ConvertedPassiveDatabase.passiveInfosPlayFab)
        {
            if (passiveName == passive.name)
            {
                return passive;
            }
        }

        //If Passive is not found, return null
        return null;
    }

}
