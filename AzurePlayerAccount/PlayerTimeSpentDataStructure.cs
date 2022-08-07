using System;
using System.Collections.Generic;

//Player Login Data Structure
public class PlayerLoginData
{
    public int lastLoginDayCount;
    public DateTime loginTime;
}

//Daily Player Time Spent Data Structure
public class DailyPlayerData
{
    public string uMasterId;
    public string uETHAddress;
    public List<DailyData> uDailyData= new List<DailyData>();
}

public class DailyData
{
    public int dayCount;
    public TimeSpan timeSpent;
    public DateTime date;
}

//Weekly Player Time Spent Data Structure
public class WeeklyPlayerData
{
    public string uMasterId;
    public string uETHAddress;
    public List<WeeklyData> uWeeklyData= new List<WeeklyData>();
}

public class WeeklyData
{
    public int weekCount;
    public TimeSpan timeSpent;
    public DateTime date_weekly;
}