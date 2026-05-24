using UnityEngine;
using System.Collections;
using TMPro;

public class Level13 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 1250, startHealth = 200;
    private int maxWave = 24;
    public GameObject soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, mosquito, darklingBeetle;
    public GameObject fireAnt, termite, scorpion, moth, firefly;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;
    public float restInterval = 18f;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        if (WeatherManager.instance) WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance?.Configure(fertilizerPool);
        GameManager.instance?.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 11);
                SaveManager.instance.CompleteLevel(12);
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
                SaveManager.instance.CompleteLevel(12);
            Debug.Log("Level 12 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd1 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    9.41f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    5.47f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd1 / 2f,   25.29f);
            InvokeRepeating(nameof(SpawnMoth),           wd1 / 2f,   21.18f);
            InvokeRepeating(nameof(SpawnFirefly),        wd1 / 2f,   23.53f);
            yield return new WaitForSeconds(wd1);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 2)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd2 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    9.41f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd2 / 3f,   11.76f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    4.88f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd2 / 3f,   17.06f);
            InvokeRepeating(nameof(SpawnMoth),           wd2 / 2f,   18.82f);
            InvokeRepeating(nameof(SpawnFirefly),        wd2 / 2f,   21.18f);
            yield return new WaitForSeconds(wd2);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 3)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd3 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    10.59f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd3 / 3f,   12.94f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd3 / 3f,   11.76f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    4.29f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd3 / 3f,   12.35f);
            InvokeRepeating(nameof(SpawnScorpion),       waitTime,    wd3 / 1.87f);
            InvokeRepeating(nameof(SpawnMoth),           wd3 / 2f,   16.47f);
            InvokeRepeating(nameof(SpawnFirefly),        wd3 / 2f,   18.82f);
            yield return new WaitForSeconds(wd3);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 4)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd4 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    11.76f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd4 / 3f,   14.12f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd4 / 3f,   11.76f);
            InvokeRepeating(nameof(SpawnMosquito),       wd4 / 2f,   15.88f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    4.0f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd4 / 4f,   10.59f);
            InvokeRepeating(nameof(SpawnScorpion),       waitTime,    17.65f);
            InvokeRepeating(nameof(SpawnMoth),           wd4 / 2f,   14.12f);
            InvokeRepeating(nameof(SpawnFirefly),        wd4 / 2f,   16.47f);
            yield return new WaitForSeconds(wd4);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 5)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    12.94f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd5 / 3f,   15.29f);
            InvokeRepeating(nameof(SpawnFruitFly),       wd5 / 3f,   11.76f);
            InvokeRepeating(nameof(SpawnWasp),           wd5 / 2f,   13.53f);
            InvokeRepeating(nameof(SpawnMosquito),       wd5 / 2f,   14.71f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    3.71f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd5 / 4f,   9.41f);
            InvokeRepeating(nameof(SpawnScorpion),       waitTime,    14.12f);
            InvokeRepeating(nameof(SpawnMoth),           wd5 / 2f,   12.35f);
            InvokeRepeating(nameof(SpawnFirefly),        wd5 / 2f,   14.12f);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 6)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd6 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,    14.12f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd6 / 3f,   16.47f);
            InvokeRepeating(nameof(SpawnWasp),           wd6 / 2f,   11.18f);
            InvokeRepeating(nameof(SpawnMosquito),       wd6 / 2f,   12.35f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,    3.41f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd6 / 4f,   8.53f);
            InvokeRepeating(nameof(SpawnScorpion),       waitTime,    12.35f);
            InvokeRepeating(nameof(SpawnMoth),           wd6 / 2f,   10.59f);
            InvokeRepeating(nameof(SpawnFirefly),        wd6 / 2f,   12.35f);
            yield return new WaitForSeconds(wd6);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 7)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      15.29f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd7 / 3f,     17.65f);
            InvokeRepeating(nameof(SpawnWasp),           wd7 / 2f,     10.0f);
            InvokeRepeating(nameof(SpawnMosquito),       wd7 / 2f,     11.18f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd7 * 2f/3f,  14.71f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd7 * 2f/3f,  7.35f);
            InvokeRepeating(nameof(SpawnScorpion),       wd7 / 3f,     10.59f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,      3.12f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd7 / 4f,     7.65f);
            InvokeRepeating(nameof(SpawnMoth),           wd7 / 3f,     9.41f);
            InvokeRepeating(nameof(SpawnFirefly),        wd7 / 3f,     10.59f);
            yield return new WaitForSeconds(wd7);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 8)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      16.47f);
            InvokeRepeating(nameof(SpawnWasp),           wd8 / 3f,     9.41f);
            InvokeRepeating(nameof(SpawnMosquito),       wd8 / 3f,     10.59f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd8 * 2f/3f,  12.35f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd8 / 2f,     6.18f);
            InvokeRepeating(nameof(SpawnScorpion),       wd8 / 3f,     9.41f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,      2.82f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd8 / 4f,     7.06f);
            InvokeRepeating(nameof(SpawnMoth),           wd8 / 3f,     8.24f);
            InvokeRepeating(nameof(SpawnFirefly),        wd8 / 3f,     9.41f);
            yield return new WaitForSeconds(wd8);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 9)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      18.82f);
            InvokeRepeating(nameof(SpawnWasp),           wd9 / 3f,     8.82f);
            InvokeRepeating(nameof(SpawnMosquito),       wd9 / 3f,     10.0f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd9 * 2f/3f,  11.18f);
            InvokeRepeating(nameof(SpawnDarklingBeetle), wd9 / 2f,     5.59f);
            InvokeRepeating(nameof(SpawnScorpion),       wd9 / 3f,     8.24f);
            InvokeRepeating(nameof(SpawnFireAnt),        waitTime,      2.53f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd9 / 4f,     6.47f);
            InvokeRepeating(nameof(SpawnMoth),           wd9 / 3f,     7.65f);
            InvokeRepeating(nameof(SpawnFirefly),        wd9 / 3f,     8.24f);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 10)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),        waitTime,       21.18f);
            InvokeRepeating(nameof(SpawnWasp),            wd10 / 3f,     8.24f);
            InvokeRepeating(nameof(SpawnMosquito),        wd10 / 3f,     9.41f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd10 * 2f/3f,  10.0f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd10 / 2f,     5.0f);
            InvokeRepeating(nameof(SpawnScorpion),        wd10 / 3f,     7.65f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       2.53f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd10 / 4f,     5.88f);
            InvokeRepeating(nameof(SpawnMoth),            wd10 / 3f,     7.06f);
            InvokeRepeating(nameof(SpawnFirefly),         wd10 / 3f,     7.65f);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 11)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd11 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),        waitTime,       23.53f);
            InvokeRepeating(nameof(SpawnWasp),            wd11 / 3f,     7.65f);
            InvokeRepeating(nameof(SpawnMosquito),        wd11 / 3f,     8.82f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd11 * 2f/3f,  9.41f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd11 / 2f,     4.71f);
            InvokeRepeating(nameof(SpawnScorpion),        wd11 / 3f,     7.06f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       2.24f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd11 / 4f,     5.29f);
            InvokeRepeating(nameof(SpawnMoth),            wd11 / 3f,     6.47f);
            InvokeRepeating(nameof(SpawnFirefly),         wd11 / 3f,     7.06f);
            yield return new WaitForSeconds(wd11);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 12)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd12 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),        waitTime,       28.24f);
            InvokeRepeating(nameof(SpawnWasp),            wd12 / 3f,     7.06f);
            InvokeRepeating(nameof(SpawnMosquito),        wd12 / 3f,     8.24f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd12 * 2f/3f,  8.82f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd12 / 2f,     4.41f);
            InvokeRepeating(nameof(SpawnScorpion),        wd12 / 3f,     6.47f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       2.24f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd12 / 4f,     5.0f);
            InvokeRepeating(nameof(SpawnMoth),            wd12 / 3f,     5.88f);
            InvokeRepeating(nameof(SpawnFirefly),         wd12 / 3f,     6.47f);
            yield return new WaitForSeconds(wd12);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 13)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd13 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd13 / 3f,     6.47f);
            InvokeRepeating(nameof(SpawnMosquito),        wd13 / 3f,     7.65f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd13 * 2f/3f,  8.24f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd13 / 2f,     4.12f);
            InvokeRepeating(nameof(SpawnScorpion),        wd13 / 3f,     5.88f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.94f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd13 / 4f,     4.71f);
            InvokeRepeating(nameof(SpawnMoth),            wd13 / 3f,     5.59f);
            InvokeRepeating(nameof(SpawnFirefly),         wd13 / 3f,     5.88f);
            yield return new WaitForSeconds(wd13);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 14)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd14 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd14 / 3f,     5.88f);
            InvokeRepeating(nameof(SpawnMosquito),        wd14 / 3f,     7.06f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd14 * 2f/3f,  7.65f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd14 / 2f,     3.82f);
            InvokeRepeating(nameof(SpawnScorpion),        wd14 / 3f,     5.59f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.94f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd14 / 4f,     4.41f);
            InvokeRepeating(nameof(SpawnMoth),            wd14 / 3f,     5.29f);
            InvokeRepeating(nameof(SpawnFirefly),         wd14 / 3f,     5.59f);
            yield return new WaitForSeconds(wd14);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 15)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd15 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd15 / 3f,     5.29f);
            InvokeRepeating(nameof(SpawnMosquito),        wd15 / 3f,     6.47f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd15 * 2f/3f,  7.06f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd15 / 2f,     3.53f);
            InvokeRepeating(nameof(SpawnScorpion),        wd15 / 3f,     5.29f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.82f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd15 / 4f,     4.12f);
            InvokeRepeating(nameof(SpawnMoth),            wd15 / 3f,     5.0f);
            InvokeRepeating(nameof(SpawnFirefly),         wd15 / 3f,     5.29f);
            yield return new WaitForSeconds(wd15);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 16)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd16 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd16 / 3f,     5.0f);
            InvokeRepeating(nameof(SpawnMosquito),        wd16 / 3f,     6.18f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd16 * 2f/3f,  6.47f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd16 / 2f,     3.24f);
            InvokeRepeating(nameof(SpawnScorpion),        wd16 / 3f,     4.71f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.76f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd16 / 4f,     3.82f);
            InvokeRepeating(nameof(SpawnMoth),            wd16 / 3f,     4.71f);
            InvokeRepeating(nameof(SpawnFirefly),         wd16 / 3f,     5.0f);
            yield return new WaitForSeconds(wd16);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 17)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd17 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd17 / 3f,     4.71f);
            InvokeRepeating(nameof(SpawnMosquito),        wd17 / 3f,     5.88f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd17 * 2f/3f,  6.18f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd17 / 2f,     3.09f);
            InvokeRepeating(nameof(SpawnScorpion),        wd17 / 3f,     4.41f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.71f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd17 / 4f,     3.53f);
            InvokeRepeating(nameof(SpawnMoth),            wd17 / 3f,     4.41f);
            InvokeRepeating(nameof(SpawnFirefly),         wd17 / 3f,     4.71f);
            yield return new WaitForSeconds(wd17);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 18)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd18 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd18 / 3f,     4.41f);
            InvokeRepeating(nameof(SpawnMosquito),        wd18 / 3f,     5.59f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd18 * 2f/3f,  5.88f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd18 / 2f,     2.94f);
            InvokeRepeating(nameof(SpawnScorpion),        wd18 / 3f,     4.12f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.65f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd18 / 4f,     3.24f);
            InvokeRepeating(nameof(SpawnMoth),            wd18 / 3f,     4.12f);
            InvokeRepeating(nameof(SpawnFirefly),         wd18 / 3f,     4.41f);
            yield return new WaitForSeconds(wd18);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 19)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd19 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd19 / 3f,     4.12f);
            InvokeRepeating(nameof(SpawnMosquito),        wd19 / 3f,     5.29f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd19 * 2f/3f,  5.59f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd19 / 2f,     2.79f);
            InvokeRepeating(nameof(SpawnScorpion),        wd19 / 3f,     3.82f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.59f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd19 / 4f,     2.94f);
            InvokeRepeating(nameof(SpawnMoth),            wd19 / 3f,     3.82f);
            InvokeRepeating(nameof(SpawnFirefly),         wd19 / 3f,     4.12f);
            yield return new WaitForSeconds(wd19);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 20)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd20 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd20 / 3f,     3.82f);
            InvokeRepeating(nameof(SpawnMosquito),        wd20 / 3f,     5.0f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd20 * 2f/3f,  5.29f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd20 / 2f,     2.65f);
            InvokeRepeating(nameof(SpawnScorpion),        wd20 / 3f,     3.53f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.53f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd20 / 4f,     2.65f);
            InvokeRepeating(nameof(SpawnMoth),            wd20 / 3f,     3.53f);
            InvokeRepeating(nameof(SpawnFirefly),         wd20 / 3f,     3.82f);
            yield return new WaitForSeconds(wd20);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 21)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd21 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd21 / 3f,     3.53f);
            InvokeRepeating(nameof(SpawnMosquito),        wd21 / 3f,     4.71f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd21 * 2f/3f,  5.0f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd21 / 2f,     2.5f);
            InvokeRepeating(nameof(SpawnScorpion),        wd21 / 3f,     3.53f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.47f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd21 / 4f,     2.35f);
            InvokeRepeating(nameof(SpawnMoth),            wd21 / 3f,     3.24f);
            InvokeRepeating(nameof(SpawnFirefly),         wd21 / 3f,     3.53f);
            yield return new WaitForSeconds(wd21);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 22)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd22 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd22 / 3f,     3.24f);
            InvokeRepeating(nameof(SpawnMosquito),        wd22 / 3f,     4.41f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd22 * 2f/3f,  4.71f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd22 / 2f,     2.35f);
            InvokeRepeating(nameof(SpawnScorpion),        wd22 / 3f,     3.24f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.41f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd22 / 4f,     2.06f);
            InvokeRepeating(nameof(SpawnMoth),            wd22 / 3f,     2.94f);
            InvokeRepeating(nameof(SpawnFirefly),         wd22 / 3f,     3.24f);
            yield return new WaitForSeconds(wd22);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 23)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd23 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd23 / 3f,     2.94f);
            InvokeRepeating(nameof(SpawnMosquito),        wd23 / 3f,     4.12f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd23 * 2f/3f,  4.41f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd23 / 2f,     2.21f);
            InvokeRepeating(nameof(SpawnScorpion),        wd23 / 3f,     2.94f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.35f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd23 / 4f,     1.76f);
            InvokeRepeating(nameof(SpawnMoth),            wd23 / 3f,     2.65f);
            InvokeRepeating(nameof(SpawnFirefly),         wd23 / 3f,     2.94f);
            yield return new WaitForSeconds(wd23);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 24)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd24 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWasp),            wd24 / 3f,     2.65f);
            InvokeRepeating(nameof(SpawnMosquito),        wd24 / 3f,     3.82f);
            InvokeRepeating(nameof(SpawnQueenAnt),        wd24 * 2f/3f,  4.12f);
            InvokeRepeating(nameof(SpawnDarklingBeetle),  wd24 / 2f,     2.06f);
            InvokeRepeating(nameof(SpawnScorpion),        wd24 / 3f,     2.94f);
            InvokeRepeating(nameof(SpawnFireAnt),         waitTime,       1.35f);
            InvokeRepeating(nameof(SpawnTermiteCluster),  wd24 / 4f,     1.76f);
            InvokeRepeating(nameof(SpawnMoth),            wd24 / 3f,     2.35f);
            InvokeRepeating(nameof(SpawnFirefly),         wd24 / 3f,     2.65f);
            yield return new WaitForSeconds(wd24);
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnDarklingBeetle));
            CancelInvoke(nameof(SpawnScorpion));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, mosquito, darklingBeetle, fireAnt, termite, scorpion, moth, firefly };

    void SpawnScorpion()        { Spawn(scorpion); }
    void SpawnDarklingBeetle()  { Spawn(darklingBeetle); }
    void SpawnSoldierAnt()      { Spawn(soldierAnt); }
    void SpawnScoutAnt()        { Spawn(scoutAnt); }
    void SpawnFruitFly()        { Spawn(fruitFly); }
    void SpawnWasp()            { Spawn(wasp); }
    void SpawnQueenAnt()        { Spawn(queenAnt); }
    void SpawnMosquito()        { Spawn(mosquito); }
    void SpawnFireAnt()         { Spawn(fireAnt); }
    void SpawnMoth()            { Spawn(moth); }
    void SpawnFirefly()         { Spawn(firefly); }
    void SpawnTermiteCluster()  { StartCoroutine(TermiteCluster(4)); }

    IEnumerator TermiteCluster(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Spawn(termite);
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
