using System;
using System.Collections.Generic;

[Serializable]
public class EnemyDefinition
{
    public string name;
    public int sprite;
    public float hp;
    public float speed;
    public float damage;
}

[Serializable]
public class SpawnDefinition
{
    public string enemy;
    public string count;
    public string hp; 
    public string speed;
    public string damage;
    public string delay;
    public List<int> sequence;
    public string location;
}

[Serializable]
public class LevelDefinition
{
    public string name;
    public int waves;
    public List<SpawnDefinition> spawns;
}