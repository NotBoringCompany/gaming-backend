using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using MongoDB.Bson;
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
        public ExecutionPosition executionPosition;
        public List<PassiveDetail> passiveDetail;
        public bool oncePerBattleOnly = false;
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
        public List<Requirements> requirements;
        public List<EffectInfo> effect;
        public SkillsDataBase.TargetType targetType;
    }




    public enum ExecutionPosition
    {
        WhenEnterBattleField,
        TurnStart,
        TurnEnd,
        ActionBefore,
        ActionReceiving,
        ActionAfter,
        StatusConditionReceiving,
        OnKnockedOut,
        DuringCombat,
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

        public RequirementTypes requirementTypes;

        //List of all the requirements logic, choose one
        public List<StatsValueIsRequired> statsValueIsRequired;
        public NBMonProperties.StatusEffect statusEffect;
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
        public int triggerChance;
        public int attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses;
        public int energyShield, surviveLethalBlow, totalIgnoreDefense, mustCritical, immuneCritical;
        public int damageReduction, energyShieldValue;
        public List<ElementalDamageReduction> ElementalDamageReductions;

    }

    //How to Modify the Passive
    public enum EffectType
    {
        None,
        StatsPercentage,
        StatusEffect,
        Stats,
        DuringBattle

    }

    public class ModifierStatus
    {
        public NBMonProperties.StatusEffect statusCondition;
    }

    //Find Skill
    public static PassiveInfoPlayFab FindPassiveSkill(string passiveName, DocumentClient client)
    {
        string PassiveDatabaseJsonString = PassiveDatabaseJson.PassiveDataJson;
        PassiveInfosPlayFabList ConvertedPassiveDatabase = JsonConvert.DeserializeObject<PassiveInfosPlayFabList>(PassiveDatabaseJsonString);

        if(client != null)
        {
            //Declare Variable for Cosmos Usage
            var option = new FeedOptions() { EnableCrossPartitionQuery = true };
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "PassiveData");

            PassiveInfoPlayFab usedData = client.CreateDocumentQuery<PassiveInfoPlayFab>(collectionUri, $"SELECT * FROM db WHERE db.name = '{passiveName}'", option).AsEnumerable().FirstOrDefault();
            
            //if usedData exists, return the data
            if(usedData != null)
                return usedData;

            return null;
        }

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
