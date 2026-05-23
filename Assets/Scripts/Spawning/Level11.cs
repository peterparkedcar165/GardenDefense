using UnityEngine;
using System.Collections;
using TMPro;

public class Level11 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 1000, startHealth = 200;
    private int maxWave = 21;
    public GameObject soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, mosquito;
    public GameObject fireAnt, termite;
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
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 10);
        SaveManager.instance.CompleteLevel(11);
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
        SaveManager.instance.CompleteLevel(11);
        Debug.Log("Level 11 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            waitTime = 2f;
            spawnInterval = 2.5f;
            spawnCount = 12;
            nextWaveTimer = 2f * (waitTime + ((spawnCount - 1) * spawnInterval)) + restInterval;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, 3.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, 3.5f);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 2)
        {
            waitTime = 2f;
            spawnInterval = 2.4f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd2 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),  waitTime,    3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd2 / 3f,   spawnInterval);
            yield return new WaitForSeconds(wd2);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));

        } else if (wave == 3)
        {
            waitTime = 2f;
            spawnInterval = 2.3f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd3 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       3.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     wd3 / 3f,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd3 / 3f,       spawnInterval * 1.8f);
            InvokeRepeating(nameof(SpawnMosquito),       wd3 * 2f / 3f,  28f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd3 / 2f,       6.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd3 / 2f,       60f);
            yield return new WaitForSeconds(wd3);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 4)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd4 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd4 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd4 * 0.5f,    14f);
            InvokeRepeating(nameof(SpawnMosquito),       wd4 * 0.5f,    19f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd4 / 3f,      6.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd4 / 3f,      22f);
            yield return new WaitForSeconds(wd4);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 5)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd5 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd5 * 0.5f,    11f);
            InvokeRepeating(nameof(SpawnMosquito),       wd5 * 0.5f,    16f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd5 * 2f / 3f, 11f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd5 / 3f,      5.75f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd5 / 3f,      19f);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 6)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd6 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd6 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd6 * 0.5f,    10f);
            InvokeRepeating(nameof(SpawnMosquito),       wd6 * 0.5f,    13.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd6 * 2f / 3f, 9.5f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd6 / 3f,      5.75f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd6 / 3f,      18f);
            yield return new WaitForSeconds(wd6);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 7)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd7 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd7 * 0.5f,    8.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd7 * 0.5f,    11.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd7 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd7 / 3f,      5.25f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd7 / 3f,      16f);
            yield return new WaitForSeconds(wd7);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 8)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd8 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd8 / 3f,      6.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd8 / 3f,      10f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd8 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd8 / 3f,      5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd8 / 3f,      14f);
            yield return new WaitForSeconds(wd8);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 9)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,      spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd9 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd9 / 3f,      6f);
            InvokeRepeating(nameof(SpawnMosquito),       wd9 / 3f,      9.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd9 * 2f / 3f, 7.75f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd9 / 3f,      4.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd9 / 3f,      13f);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 10)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd10 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd10 / 3f,      5.25f);
            InvokeRepeating(nameof(SpawnMosquito),       wd10 / 3f,      9f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd10 * 2f / 3f, 7f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd10 / 3f,      4.25f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd10 / 3f,      12f);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 11)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd11 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd11 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd11 / 3f,      4.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd11 / 3f,      8.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd11 * 2f / 3f, 5.25f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd11 / 3f,      4f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd11 / 3f,      11f);
            yield return new WaitForSeconds(wd11);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 12)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd12 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd12 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd12 / 3f,      3.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd12 / 3f,      7.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd12 * 2f / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd12 / 3f,      3.75f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd12 / 3f,      10f);
            yield return new WaitForSeconds(wd12);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 13)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd13 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd13 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd13 / 3f,      3.25f);
            InvokeRepeating(nameof(SpawnMosquito),       wd13 / 3f,      7f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd13 * 2f / 3f, 4f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd13 / 3f,      3.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd13 / 3f,      9.5f);
            yield return new WaitForSeconds(wd13);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 14)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd14 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd14 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd14 / 3f,      3.0f);
            InvokeRepeating(nameof(SpawnMosquito),       wd14 / 3f,      6.5f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd14 * 2f / 3f, 3.75f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd14 / 3f,      3.25f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd14 / 3f,      9f);
            yield return new WaitForSeconds(wd14);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 15)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd15 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd15 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd15 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd15 / 3f,      6f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd15 * 2f / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd15 / 3f,      3.25f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd15 / 3f,      8.5f);
            yield return new WaitForSeconds(wd15);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 16)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd16 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd16 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd16 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd16 / 3f,      5.75f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd16 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd16 / 3f,      3f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd16 / 3f,      8f);
            yield return new WaitForSeconds(wd16);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 17)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd17 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd17 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd17 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd17 / 3f,      5.25f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd17 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd17 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd17 / 3f,      7.5f);
            yield return new WaitForSeconds(wd17);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 18)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd18 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd18 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd18 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd18 / 3f,      4.75f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd18 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd18 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd18 / 3f,      7f);
            yield return new WaitForSeconds(wd18);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 19)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd19 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd19 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd19 / 3f,      2.75f);
            InvokeRepeating(nameof(SpawnMosquito),       wd19 / 3f,      4.25f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd19 * 2f / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd19 / 3f,      2.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd19 / 3f,      6.5f);
            yield return new WaitForSeconds(wd19);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 20)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd20 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd20 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd20 / 3f,      2.5f);
            InvokeRepeating(nameof(SpawnMosquito),       wd20 / 3f,      4f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd20 * 2f / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd20 / 3f,      2.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd20 / 3f,      6f);
            yield return new WaitForSeconds(wd20);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));

        } else if (wave == 21)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd21 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt),       waitTime,       4.5f);
            InvokeRepeating(nameof(SpawnSoldierAnt),     waitTime,       spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly),       wd21 / 3f,      spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp),           wd21 / 3f,      2.25f);
            InvokeRepeating(nameof(SpawnMosquito),       wd21 / 3f,      3.75f);
            InvokeRepeating(nameof(SpawnQueenAnt),       wd21 * 2f / 3f, 2.5f);
            InvokeRepeating(nameof(SpawnFireAnt),        wd21 / 3f,      2.5f);
            InvokeRepeating(nameof(SpawnTermiteCluster), wd21 / 3f,      5.5f);
            yield return new WaitForSeconds(wd21);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnFireAnt));
            CancelInvoke(nameof(SpawnTermiteCluster));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    void SpawnSoldierAnt()      { Spawn(soldierAnt); }
    void SpawnScoutAnt()        { Spawn(scoutAnt); }
    void SpawnFruitFly()        { Spawn(fruitFly); }
    void SpawnWasp()            { Spawn(wasp); }
    void SpawnQueenAnt()        { Spawn(queenAnt); }
    void SpawnMosquito()        { Spawn(mosquito); }
    void SpawnFireAnt()         { Spawn(fireAnt); }
    void SpawnTermiteCluster()  { StartCoroutine(TermiteCluster(3)); }

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
