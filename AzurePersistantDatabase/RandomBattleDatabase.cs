using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NBMonBattleDatabase
{
    //Monster Level Range
    public int LevelRange;
    
    //Information of The Monster
    public List<NBMonData> MonsterDatas;
}

public class NBMonData
{
    public string MonsterID;
    public string MonsterNickname;
    public List<string> EquipSkill;
    public List<string> InheritedSkill;
    public List<string> Passive;
}

public class RandomBattleDatabase
{   
    //Database Variable
    public static List<NBMonBattleDatabase> RandomBattleData;

    //A static function to Convert JsonString into a Class
    public static void GetData()
    {
        RandomBattleData = JsonConvert.DeserializeObject<List<NBMonBattleDatabase>>(RandomBattleDatabaseJsonString);
    }

    //Database JsonString
    public static string RandomBattleDatabaseJsonString = "[{\"LevelRange\":6,\"MonsterDatas\":[{\"MonsterID\":\"Heree\",\"MonsterNickname\":\"Heree\",\"EquipSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"InheritedSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Heree\",\"MonsterNickname\":\"Heree\",\"EquipSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"InheritedSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]}]},{\"LevelRange\":9,\"MonsterDatas\":[{\"MonsterID\":\"Heree\",\"MonsterNickname\":\"Heree\",\"EquipSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"InheritedSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Heree\",\"MonsterNickname\":\"Heree\",\"EquipSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"InheritedSkill\":[\"Small Chop\",\"Leaf Slash\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]}]},{\"LevelRange\":4,\"MonsterDatas\":[{\"MonsterID\":\"Pfufu\",\"MonsterNickname\":\"Pfufu\",\"EquipSkill\":[\"Slap\",\"Defense Dance\"],\"InheritedSkill\":[\"Slap\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Pfufu\",\"MonsterNickname\":\"Pfufu\",\"EquipSkill\":[\"Slap\",\"Defense Dance\"],\"InheritedSkill\":[\"Slap\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]}]},{\"LevelRange\":8,\"MonsterDatas\":[{\"MonsterID\":\"Pfufu\",\"MonsterNickname\":\"Pfufu\",\"EquipSkill\":[\"Slap\",\"Defense Dance\"],\"InheritedSkill\":[\"Slap\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Pfufu\",\"MonsterNickname\":\"Pfufu\",\"EquipSkill\":[\"Slap\",\"Defense Dance\"],\"InheritedSkill\":[\"Slap\",\"Defense Dance\"],\"Passive\":[\"Water Bracer\"]}]},{\"LevelRange\":5,\"MonsterDatas\":[{\"MonsterID\":\"Roggo\",\"MonsterNickname\":\"Roggo\",\"EquipSkill\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"InheritedSkill\":[],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Roggo\",\"MonsterNickname\":\"Roggo\",\"EquipSkill\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"InheritedSkill\":[],\"Passive\":[\"Water Bracer\"]}]},{\"LevelRange\":7,\"MonsterDatas\":[{\"MonsterID\":\"Roggo\",\"MonsterNickname\":\"Roggo\",\"EquipSkill\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"InheritedSkill\":[],\"Passive\":[\"Water Bracer\"]},{\"MonsterID\":\"Roggo\",\"MonsterNickname\":\"Roggo\",\"EquipSkill\":[\"Slap\",\"Tsunami\",\"Defense Dance\"],\"InheritedSkill\":[],\"Passive\":[\"Water Bracer\"]}]}]";
}
