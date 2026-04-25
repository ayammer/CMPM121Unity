using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LevelLoader : MonoBehaviour
{
    public List<EnemyDefinition> enemies;
    public List<LevelDefinition> levels;

    void Start()
    {
        TextAsset enemiesText = Resources.Load<TextAsset>("enemies");
        TextAsset levelsText = Resources.Load<TextAsset>("levels");

        if (enemiesText != null && levelsText != null)
        {
            enemies = JsonConvert.DeserializeObject<List<EnemyDefinition>>(enemiesText.text);
            levels = JsonConvert.DeserializeObject<List<LevelDefinition>>(levelsText.text);
            
            Debug.Log($"SUCCESS! Loaded {enemies.Count} enemies and {levels.Count} difficulty levels.");
            if (levels.Count > 0) 
            {
                Debug.Log($"The first difficulty level is named: {levels[0].name}");
            }
        }
        else
        {
            Debug.LogError("Could not find the JSON files! Are they exactly named 'enemies.json' and 'levels.json' inside Assets/Resources?");
        }
    }
}