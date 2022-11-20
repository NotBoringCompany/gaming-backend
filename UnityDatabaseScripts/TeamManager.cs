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
