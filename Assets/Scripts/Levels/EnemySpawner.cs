using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RPN = RPNEvaluator.RPNEvaluator;


public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    private List<EnemyDefinition> enemyDefinitions;
    private List<LevelDefinition> levelDefinitions;
    private Dictionary<string, EnemyDefinition> enemyLookup;
    private Dictionary<string, LevelDefinition> levelLookup;

    private LevelDefinition currentLevel;
    private int currentWave;
    private int activeSpawnStreams;

    void Start()
    {
        GameManager.Instance.state = GameManager.GameState.PREGAME;
        LoadDefinitions();
        BuildLevelButtons();
    }

    public void StartLevel(string levelname)
    {
        if (levelLookup == null || !levelLookup.ContainsKey(levelname))
        {
            Debug.LogError("Unknown level: " + levelname);
            return;
        }

        currentLevel = levelLookup[levelname];
        currentWave = 1;

        level_selector.gameObject.SetActive(false);
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();

        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (currentLevel == null) return;

        currentWave++;

        if (currentLevel.waves > 0 && currentWave > currentLevel.waves)
        {
            HandleWin();
            return;
        }

        StartCoroutine(SpawnWave());
    }

    private void LoadDefinitions()
    {
        TextAsset enemiesFile = Resources.Load<TextAsset>("enemies");
        TextAsset levelsFile = Resources.Load<TextAsset>("levels");

        if (enemiesFile == null || levelsFile == null)
        {
            Debug.LogError("Could not load enemies.json or levels.json.");
            return;
        }

        enemyDefinitions = JsonConvert.DeserializeObject<List<EnemyDefinition>>(enemiesFile.text);
        levelDefinitions = JsonConvert.DeserializeObject<List<LevelDefinition>>(levelsFile.text);

        enemyLookup = enemyDefinitions.ToDictionary(e => e.name, e => e);
        levelLookup = levelDefinitions.ToDictionary(l => l.name, l => l);
    }

    private void BuildLevelButtons()
    {
        if (levelDefinitions == null) return;

        for (int i = 0; i < levelDefinitions.Count; i++)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, 130 - (i * 60), 0);

            MenuSelectorController controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.SetLevel(levelDefinitions[i].name);
        }
    }

    private IEnumerator SpawnWave()
    {
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;

        for (int i = 3; i > 0; i--)
        {
            yield return new WaitForSeconds(1f);
            GameManager.Instance.countdown--;
        }

        GameManager.Instance.state = GameManager.GameState.INWAVE;
        activeSpawnStreams = 0;

        foreach (SpawnDefinition spawn in currentLevel.spawns)
        {
            StartCoroutine(SpawnStream(spawn, currentWave));
        }

        yield return new WaitUntil(() => activeSpawnStreams == 0 && GameManager.Instance.enemy_count == 0);

        if (currentLevel.waves > 0 && currentWave >= currentLevel.waves)
        {
            HandleWin();
        }
        else
        {
            GameManager.Instance.state = GameManager.GameState.WAVEEND;
        }
    }

    private IEnumerator SpawnStream(SpawnDefinition spawn, int wave)
    {
        activeSpawnStreams++;

        if (!enemyLookup.ContainsKey(spawn.enemy))
        {
            Debug.LogError("Unknown enemy type: " + spawn.enemy);
            activeSpawnStreams--;
            yield break;
        }

        EnemyDefinition baseEnemy = enemyLookup[spawn.enemy];

        int totalCount = Mathf.Max(0, EvalInt(spawn.count, 0, wave));
        int hp = Mathf.RoundToInt(EvalFloatOrDefault(spawn.hp, baseEnemy.hp, wave, baseEnemy.hp));
        int speed = Mathf.RoundToInt(EvalFloatOrDefault(spawn.speed, baseEnemy.speed, wave, baseEnemy.speed));
        int damage = Mathf.RoundToInt(EvalFloatOrDefault(spawn.damage, baseEnemy.damage, wave, baseEnemy.damage));
        float delay = EvalFloatOrDefault(spawn.delay, 0f, wave, 2f);

        List<int> sequence = (spawn.sequence != null && spawn.sequence.Count > 0)
            ? spawn.sequence
            : new List<int> { 1 };

        int remaining = totalCount;
        int sequenceIndex = 0;

        while (remaining > 0)
        {
            int groupSize = Mathf.Max(1, sequence[sequenceIndex % sequence.Count]);
            groupSize = Mathf.Min(groupSize, remaining);

            SpawnPoint spawnPoint = PickSpawnPoint(spawn.location);
            if (spawnPoint == null)
            {
                Debug.LogError("No valid spawn point for location: " + spawn.location);
                break;
            }

            for (int i = 0; i < groupSize; i++)
            {
                SpawnEnemy(baseEnemy, spawnPoint, hp, speed, damage);
            }

            remaining -= groupSize;
            sequenceIndex++;

            if (remaining > 0)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        activeSpawnStreams--;
    }

    private void SpawnEnemy(EnemyDefinition baseEnemy, SpawnPoint spawnPoint, int hp, int speed, int damage)
    {
        Vector2 offset = Random.insideUnitCircle * 1.8f;
        Vector3 pos = spawnPoint.transform.position + new Vector3(offset.x, offset.y, 0f);

        GameObject newEnemy = Instantiate(enemy, pos, Quaternion.identity);
        newEnemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(baseEnemy.sprite);

        EnemyController controller = newEnemy.GetComponent<EnemyController>();
        controller.hp = new Hittable(hp, Hittable.Team.MONSTERS, newEnemy);
        controller.speed = speed;
        controller.damage = damage;

        GameManager.Instance.AddEnemy(newEnemy);
    }

    private SpawnPoint PickSpawnPoint(string location)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0) return null;

        if (string.IsNullOrEmpty(location) || location == "random")
        {
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        }

        string[] parts = location.Split(' ');

        if (parts.Length >= 2)
        {
            string typeName = parts[1].ToUpper();

            if (System.Enum.TryParse(typeName, out SpawnPoint.SpawnName kind))
            {
                SpawnPoint[] filtered = SpawnPoints.Where(p => p.kind == kind).ToArray();
                if (filtered.Length > 0)
                {
                    return filtered[Random.Range(0, filtered.Length)];
                }
            }
        }

        return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
    }
    
    private int EvalInt(string expression, int baseValue, int wave)
    {
        if (string.IsNullOrEmpty(expression)) return 0;

        return RPN.Evaluate(expression, new Dictionary<string, int>
        {
            { "base", baseValue },
            { "wave", wave }
        });
    }

    private float EvalFloatOrDefault(string expression, float baseValue, int wave, float fallback)
    {
        if (string.IsNullOrEmpty(expression)) return fallback;

        return RPN.Evaluatef(expression, new Dictionary<string, float>
        {
            { "base", baseValue },
            { "wave", wave }
        });
    }

    private void HandleWin()
    {
        GameManager.Instance.playerWon = true;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        Debug.Log("You Win!");
    }
}
