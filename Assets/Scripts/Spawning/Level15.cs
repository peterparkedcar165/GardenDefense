using UnityEngine;
using System.Collections;

public class Level15 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 350, startHealth = 200;
    private int maxWave = 40;
    public GameObject soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, mosquito, darklingBeetle;
    public GameObject fireAnt, termite, scorpion, moth, firefly;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;
    public float restInterval = 12f;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    // -1 = alternate between entries, 0 = SpawnA only, 1 = SpawnB only
    private int activeEntry = -1;
    private int _spawnCounter = 0;

    protected override void Start()
    {
        if (WeatherManager.instance) WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance?.Configure(fertilizerPool);
        GameManager.instance?.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 14);
        SaveManager.instance.CompleteLevel(13);
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        nextWaveTimer = 10f;
        yield return new WaitForSeconds(10f);

        while (wave < maxWave)
        {
            wave++;
            GameManager.instance.currentWave = wave;
            GameHUD.instance?.SetWaveCount(wave, maxWave);
            yield return StartCoroutine(Wave(wave));

            if (wave < maxWave)
                yield return StartCoroutine(RestPeriod(restInterval));
        }

        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        SaveManager.instance.CompleteLevel(14);
        Debug.Log("Level 14 completed");
    }

    IEnumerator Wave(int wave)
    {
        // ── PHASE 1 · Waves 1-13 · Setup (spawnCount=7, wd≈20s) ──────────────
        // Ground bugs only. Lets the player build up before pressure arrives.

        if (wave == 1)
        {
            activeEntry = 0;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, 9f);
            InvokeRepeating(nameof(SpawnFireAnt),  waitTime, 7f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));

        } else if (wave == 2)
        {
            activeEntry = 1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, 10f);
            InvokeRepeating(nameof(SpawnFireAnt),  waitTime,  7f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));

        } else if (wave == 3)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  9f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  6f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  20f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 4)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  8f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  5.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  20f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 5)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  8f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 6)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  7f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,   9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 7)
        {
            // SoldierAnt joins
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  9f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 2f,  10f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,   9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 8)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  8f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,   9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 9)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,  7f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 10)
        {
            // Scorpion joins
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),   waitTime,   9f);
            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,   4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,    8f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 2f,   12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));

        } else if (wave == 11)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 12)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,   9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 13)
        {
            // Heavy ground push before flyers arrive
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 7;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   6f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,   8f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,   9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnTermiteCluster));

        // ── PHASE 2 · Waves 14-33 · Rising difficulty (spawnCount=9, wd≈26s) ─
        // Flying bugs introduced gradually. Density builds wave by wave.

        } else if (wave == 14)
        {
            // Moth + Firefly join
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,  12f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 2f,  13f);
            InvokeRepeating(nameof(SpawnFirefly),    wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 15)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   8.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 3f,  12f);
            InvokeRepeating(nameof(SpawnFirefly),        wd / 3f,  12f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 16)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  4f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnFirefly),        wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 17)
        {
            // FruitFly joins
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnFirefly),    wd / 4f,  12f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 18)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  4f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnFirefly),    wd / 4f,  11f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 19)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   9.5f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 3f,   9.5f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 3f,  10.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 20)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnFirefly),        wd / 4f,   9f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 21)
        {
            // Mosquito joins
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  4f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnFirefly),    wd / 4f,   9f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));

        } else if (wave == 22)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 4f,  10f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));

        } else if (wave == 23)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   8.5f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,   9.5f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 24)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  3f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,   8f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 3f,   8.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 25)
        {
            // Wasp joins
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 4f,   9f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnWasp),       wd / 2f,  13f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));

        } else if (wave == 26)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  3f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 4f,   8.5f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,   9.5f);
            InvokeRepeating(nameof(SpawnWasp),       wd / 3f,  12f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 27)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  3f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnWasp),       wd / 3f,  11f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 4f,  11f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 28)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  3f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   6.5f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,   8.5f);
            InvokeRepeating(nameof(SpawnWasp),       wd / 3f,  10.5f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 4f,  10.5f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 29)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),    waitTime,  2.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f,   6.5f);
            InvokeRepeating(nameof(SpawnScorpion),   wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnMosquito),   wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnWasp),       wd / 3f,  10f);
            InvokeRepeating(nameof(SpawnFruitFly),   wd / 4f,  10f);
            InvokeRepeating(nameof(SpawnMoth),       wd / 3f,  11f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 30)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  2.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   6f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,   9.5f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,   9.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 31)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  2.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   6f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   6.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,   7.5f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 3f,   9f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  11f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 32)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  2f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   5.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   6f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,   8.5f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,   9f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 33)
        {
            // Peak Phase 2 – all Phase 2 bugs at full pressure
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,  2f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 3f,   5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 3f,   6f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,   7f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,   8f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,   8f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,   9f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,  10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnTermiteCluster));

        // ── PHASE 3 · Waves 34-40 · Tough (spawnCount=11, wd≈32s) ────────────
        // All insects. QueenAnt and DarklingBeetle join the assault.

        } else if (wave == 34)
        {
            // QueenAnt + DarklingBeetle join
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,     3f);
            InvokeRepeating(nameof(SpawnSoldierAnt),      wd / 4f,     7f);
            InvokeRepeating(nameof(SpawnScorpion),        wd / 4f,     8f);
            InvokeRepeating(nameof(SpawnMosquito),        wd / 3f,     8f);
            InvokeRepeating(nameof(SpawnWasp),            wd / 3f,     9f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd / 2f,    16f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd * 2f/3f, 32f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd / 3f,    10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 35)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    2.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    6.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    7.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    8f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    8.5f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,    9f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    9f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,   14f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 2f,   32f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 36)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    2.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    6f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    7f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    7.5f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    8f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    8.5f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,   12f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 2f,   32f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,   10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 37)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    2f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    5.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    6.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    7f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    7.5f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,    8f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    8f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,   11f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 3f,   32f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 38)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    2f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    6f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    6.5f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    7f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    7.5f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,   10f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 3f,   16f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,    9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 39)
        {
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    2f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    4.5f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    5.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    6f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    6.5f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,    7f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    7f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,    9f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 3f,   16f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,    8.5f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 40)
        {
            // FINAL WAVE – maximum intensity
            activeEntry = -1;
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    1.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd / 4f,    4f);
            InvokeRepeating(nameof(SpawnScorpion),       wd / 4f,    5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd / 3f,    5.5f);
            InvokeRepeating(nameof(SpawnWasp),           wd / 3f,    6f);
            InvokeRepeating(nameof(SpawnMoth),           wd / 4f,    6.5f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd / 4f,    6.5f);
            InvokeRepeating(nameof(SpawnFirefly),        wd / 4f,    7f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd / 3f,    8f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd / 3f,   16f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd / 3f,    8f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, mosquito, darklingBeetle, fireAnt, termite, scorpion, moth, firefly };

    // ── Spawn helpers ──────────────────────────────────────────────────────────
    // All go through SpawnForWave so activeEntry is respected automatically.

    void SpawnForWave(GameObject prefab)
    {
        if (activeEntry < 0)
        {
            // alternate: even spawns → entry 0, odd spawns → entry 1
            SpawnAt(prefab, _spawnCounter % 2);
            _spawnCounter++;
        }
        else
        {
            SpawnAt(prefab, activeEntry);
        }
    }

    void SpawnScorpion()        { SpawnForWave(scorpion); }
    void SpawnDarklingBeetle()  { SpawnForWave(darklingBeetle); }
    void SpawnSoldierAnt()      { SpawnForWave(soldierAnt); }
    void SpawnScoutAnt()        { SpawnForWave(scoutAnt); }
    void SpawnFruitFly()        { SpawnForWave(fruitFly); }
    void SpawnWasp()            { SpawnForWave(wasp); }
    void SpawnQueenAnt()        { SpawnForWave(queenAnt); }
    void SpawnMosquito()        { SpawnForWave(mosquito); }
    void SpawnFireAnt()         { SpawnForWave(fireAnt); }
    void SpawnMoth()            { SpawnForWave(moth); }
    void SpawnFirefly()         { SpawnForWave(firefly); }
    void SpawnTermiteCluster()  { StartCoroutine(TermiteCluster(4)); }

    IEnumerator TermiteCluster(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnForWave(termite);
            yield return new WaitForSeconds(0.3f);
        }
    }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
