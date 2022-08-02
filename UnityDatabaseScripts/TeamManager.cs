using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class TeamManager
{
    public List<NBMonDataSave> teamList;
    public string Owner;
    public bool isHuman = false;
    public bool isNPC = false;
    public int RECGained = 0;
    public bool isBoss = false;
    public int bossTeamDuplicateTurnRNG = default;
    public string Environment;
    public string MusicName = "Battle 1";
    public int PlayerEnergyCost = 4;

    public NBMonDataSave FindNBMon(string idValue)
    {
        //Find NBMon by matching uniqueId with idValue
        foreach (var monster in teamList)
        {
            if (monster.uniqueId == idValue)
            {
                return monster;
            }
        }
        return null;
    }

    public void SwitchMonsterLocation(int location1, int location2)
    {
        //Create a copy of the list
        List<NBMonDataSave> copyTeamList = new List<NBMonDataSave>();
        copyTeamList.AddRange(teamList);

        //If switching the first monster in team, make sure the second monster have not fainted
        RefreshFainted();
        if (copyTeamList.Count > 2)
        {
            if (location1 == 0 && !copyTeamList[location2].fainted)
            {
                teamList[location1] = copyTeamList[location2];
                teamList[location2] = copyTeamList[location1];
            }
        }
    }

    public void RestoreHP()
    {
        //Reset the state of the team
        foreach (var monster in teamList)
        {
            monster.hp = monster.maxHp;
            monster.energy = monster.maxEnergy;
            monster.fainted = false;
            monster.statusEffectList.Clear();
        }
    }

    public void RemovesAllStatusEffects()
    {
        //Reset all status effects at the start of the battle (Player Team Only)
        foreach (var monster in teamList)
            monster.statusEffectList.Clear();
    }

    public void TempRandomizeOrder()
    {
        Random rand = new Random();
        var randomNumber = rand.Next(0, 10);

        if (randomNumber < 0.25f)
        {
            SwitchMonsterLocation(0, 2);
        } else if (randomNumber >= 0.25f && randomNumber < 0.5)
        {
            SwitchMonsterLocation(0, 3);
        } 
        else if (randomNumber >= 5f && randomNumber <= 1)
        {
            SwitchMonsterLocation(1, 3);
        }
    }

    public bool CheckIfFirstTwoFainted()
    {
        bool returnBool = true;

        int DefeatedCount = 0;
        int NotDefeatedCount = 0;

        foreach (var monster in teamList)
        {
            if (monster.fainted == true)
                DefeatedCount += 1;
            else
                NotDefeatedCount += 1;
        }

        if (DefeatedCount < teamList.Count - DefeatedCount)
            returnBool = false;

        if (NotDefeatedCount >= 2 && teamList.Count == 4)
            returnBool = false;

        // Debug.Log("Defeated Count: " + DefeatedCount + "/" + " Not Defeated Count: " + NotDefeatedCount + "/ Return Bool: " + returnBool);

        return returnBool;
    }

    public bool CheckIfAllFained()
    {
        bool returnBool = true;
        foreach (var monster in teamList)
        {
            //if there is at least 1 monster who have not fainted, return false
            if (!monster.fainted)
            {
                returnBool = false;
            }
        }

        return returnBool;
    }

    public void RefreshFainted()
    {
        foreach (var monster in teamList)
        {
            if (!monster.fainted)
            {
                if (monster.hp <= 0)
                {
                    monster.hp = 0;
                    monster.fainted = true;
                }
            }
        }
    }


}
