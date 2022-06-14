using System.Collections;
using System.Collections.Generic;
public class StatusEffectIconDatabase
{

    public class Sprite{
        public string url;
    }
    public enum StatusEffectCategory
    {
        HP_Energy_Related,
        Combat_Related,
        Turn_Action_Related,
        Speed_Related,
        Add_Temp_Passive_Related,
        Critical_Rate_Related
    }

    [System.Serializable]
    public class StatusConditionDataPlayFab
    {
        public NBMonProperties.StatusEffect statusConditionName;
        public StatusEffectCategory statusEffectCategory;
        public string description;
        public bool spawnCancelIcon;
        public bool durationStack = false;
        public bool stackable = false;
        public int maxStacks = 1;
        public bool elementImmunity;
        public ElementDatabase.Elements immuneAgainstElement;

        public int HPChangesInNumber, HPChangesInPercent, EnergyChangesInNumber, EnergyChangesInPercent;
        public float Attack, SP_Attack, Defense, SP_Defense;
        public float CritRate;
        public string passiveName;
        public float RNG_Chance_To_Fail_Attacking;
        public float Speed;
    }

    public List<StatusConditionDataPlayFab> statusConditionDatabasePlayFab;

    public class StatusConditionDatabasePlayFabList
    {
        public List<StatusConditionDataPlayFab> statusConditionDatabasePlayFab;
    }

    public StatusConditionDataPlayFab FindStatusEffectIcon(NBMonProperties.StatusEffect statusEffect)
    {
        foreach (var statusData in statusConditionDatabasePlayFab)
        {
            if (statusData.statusConditionName == statusEffect)
            {
                return statusData;
            }
        }

        return null;
    }
}
