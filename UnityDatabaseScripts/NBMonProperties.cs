using System.Collections;
using System.Collections.Generic;

public class NBMonProperties
{
    [System.Serializable]
    public class StatusEffectCountGroup
    {
        public StatusEffect statusEffect;
        public int counter;
        public int stacks;

        //Constructor
        public StatusEffectCountGroup(StatusEffect statusEffect, int counter, int stacks)
        {
            this.statusEffect = statusEffect;
            this.counter = counter;
            this.stacks = stacks;
        }
    }

    //Status of the NBMon
    public enum StatusEffect
    {
        None,
        Sleeping,
        Burned,
        Cold,
        Exhausted,
        Toxic,
        Regen,
        HpUp,
        AttackUp,
        SpecialAttackUp,
        DefenseUp,
        SpecialDefenseUp,
        AttackDown,
        SpecialAttackDown,
        DefenseDown,
        SpecialDefenseDown,
        Stun,
        SpeedUp,
        SpeedDown,
        Frozen,
        Awake,
        Polarized,
        Concentration,
        AntiPoison,
        AntiStun,
        AntiParalyze,
        AntiBurn,
        Guard,
        Paralyzed,
        Panic,
        Double_Down_Attack,
        Double_Down_Special,
        Bleeding,
        Dexterity,
        MoraleGuard
    }


    public enum Gender
    {
        Male, 
        Female,
        None,
    }

    [System.Serializable]
    public class StatusEffectInfo
    {
        public StatusEffect statusEffect;
        public int triggerChance;
        public int countAmmount = 1;
        public int stackAmount = 1;
    }
    public enum InputChange
    {
        HP_Percent,
        Energy_Percent
    }

    [System.Serializable]
    public class StatusEffectOpposites
    {
        public StatusEffect positiveStatus;
        public StatusEffect negativeStatus;
    }

    public enum StatsType
    {
        Hp,
        Energy,
        Speed,
        Attack,
        SpecialAttack,
        Defense,
        SpecialDefense
    }
}

