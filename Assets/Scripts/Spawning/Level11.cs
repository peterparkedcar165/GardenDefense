using UnityEngine;
using System.Collections;
using TMPro;

public class Level11 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 1000, startHealth = 200;
    private int maxWave = 21;
    public GameObject soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, snail, moth, firefly, mosquito;
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
        WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance.Configure(fertilizerPool);
        GameManager.instance.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 9);
        SaveManager.instance.CompleteLevel(10);
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
        SaveManager.instance.CompleteLevel(10);
        Debug.Log("Level 10 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            // Easy setup: scouts only, two small phases
            waitTime = 2f;
            spawnInterval = 2.5f;
            spawnCount = 12;
            nextWaveTimer = 2f * (waitTime + ((spawnCount - 1) * spawnInterval)) + restInterval;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 2)
        {
            // Easy: scouts with soldiers delayed, snail/moth arrive late and sparse
            waitTime = 2f;
            spawnInterval = 2.4f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd2 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd2 / 3f, spawnInterval);
            InvokeRepeating(nameof(SpawnSnail), wd2 * 0.6f, 32f);
            InvokeRepeating(nameof(SpawnMoth), wd2 * 0.6f, 22f);
            yield return new WaitForSeconds(wd2);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 3)
        {
            // Easy-moderate: scouts+soldiers, fruitfly mid, mosquito+snail+moth late and sparse
            waitTime = 2f;
            spawnInterval = 2.3f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd3 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd3 / 3f, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd3 / 3f, spawnInterval * 1.8f);
            InvokeRepeating(nameof(SpawnMosquito), wd3 * 2f / 3f, 25f);
            InvokeRepeating(nameof(SpawnSnail), wd3 * 2f / 3f, 22f);
            InvokeRepeating(nameof(SpawnMoth), wd3 * 2f / 3f, 18f);
            yield return new WaitForSeconds(wd3);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 4)
        {
            // Ramp begins: full roster, wasp/firefly/mosquito at midpoint
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd4 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd4 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd4 * 0.5f, 14f);
            InvokeRepeating(nameof(SpawnFirefly), wd4 * 0.5f, 11f);
            InvokeRepeating(nameof(SpawnMosquito), wd4 * 0.5f, 17f);
            InvokeRepeating(nameof(SpawnSnail), wd4 * 2f / 3f, 12f);
            InvokeRepeating(nameof(SpawnMoth), wd4 * 2f / 3f, 11f);
            yield return new WaitForSeconds(wd4);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 5)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd5 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd5 * 0.5f, 11f);
            InvokeRepeating(nameof(SpawnFirefly), wd5 * 0.5f, 9f);
            InvokeRepeating(nameof(SpawnMosquito), wd5 * 0.5f, 14f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd5 * 2f / 3f, 11f);
            InvokeRepeating(nameof(SpawnSnail), wd5 * 2f / 3f, 10f);
            InvokeRepeating(nameof(SpawnMoth), wd5 * 2f / 3f, 9f);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 6)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd6 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd6 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd6 * 0.5f, 10f);
            InvokeRepeating(nameof(SpawnFirefly), wd6 * 0.5f, 8f);
            InvokeRepeating(nameof(SpawnMosquito), wd6 * 0.5f, 12f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd6 * 2f / 3f, 9.5f);
            InvokeRepeating(nameof(SpawnSnail), wd6 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnMoth), wd6 * 2f / 3f, 8f);
            yield return new WaitForSeconds(wd6);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 7)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd7 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd7 * 0.5f, 8.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd7 * 0.5f, 7f);
            InvokeRepeating(nameof(SpawnMosquito), wd7 * 0.5f, 10f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd7 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnSnail), wd7 * 2f / 3f, 7f);
            InvokeRepeating(nameof(SpawnMoth), wd7 * 2f / 3f, 7.5f);
            yield return new WaitForSeconds(wd7);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 8)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd8 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd8 / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd8 / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnMosquito), wd8 / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd8 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnSnail), wd8 * 2f / 3f, 7f);
            InvokeRepeating(nameof(SpawnMoth), wd8 * 2f / 3f, 6.5f);
            yield return new WaitForSeconds(wd8);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 9)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 19;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd9 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd9 / 3f, 6f);
            InvokeRepeating(nameof(SpawnFirefly), wd9 / 3f, 5f);
            InvokeRepeating(nameof(SpawnMosquito), wd9 / 3f, 8f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd9 * 2f / 3f, 7.75f);
            InvokeRepeating(nameof(SpawnSnail), wd9 * 2f / 3f, 6.25f);
            InvokeRepeating(nameof(SpawnMoth), wd9 * 2f / 3f, 6.25f);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 10)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd10 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd10 / 3f, 5.25f);
            InvokeRepeating(nameof(SpawnFirefly), wd10 / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnMosquito), wd10 / 3f, 7.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd10 * 2f / 3f, 7f);
            InvokeRepeating(nameof(SpawnSnail), wd10 * 2f / 3f, 6f);
            InvokeRepeating(nameof(SpawnMoth), wd10 * 2f / 3f, 5.5f);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 11)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd11 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd11 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd11 / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd11 / 3f, 4f);
            InvokeRepeating(nameof(SpawnMosquito), wd11 / 3f, 7f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd11 * 2f / 3f, 5.25f);
            InvokeRepeating(nameof(SpawnSnail), wd11 * 2f / 3f, 5f);
            InvokeRepeating(nameof(SpawnMoth), wd11 * 2f / 3f, 5.25f);
            yield return new WaitForSeconds(wd11);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 12)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd12 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd12 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd12 / 3f, 3.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd12 / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnMosquito), wd12 / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd12 * 2f / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnSnail), wd12 * 2f / 3f, 4.25f);
            InvokeRepeating(nameof(SpawnMoth), wd12 * 2f / 3f, 4.75f);
            yield return new WaitForSeconds(wd12);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 13)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd13 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd13 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd13 / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnFirefly), wd13 / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnMosquito), wd13 / 3f, 6f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd13 * 2f / 3f, 4f);
            InvokeRepeating(nameof(SpawnSnail), wd13 * 2f / 3f, 4f);
            InvokeRepeating(nameof(SpawnMoth), wd13 * 2f / 3f, 4.5f);
            yield return new WaitForSeconds(wd13);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 14)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd14 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd14 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd14 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd14 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnMosquito), wd14 / 3f, 5.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd14 * 2f / 3f, 3.75f);
            InvokeRepeating(nameof(SpawnSnail), wd14 * 2f / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnMoth), wd14 * 2f / 3f, 4f);
            yield return new WaitForSeconds(wd14);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 15)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd15 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd15 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd15 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd15 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnMosquito), wd15 / 3f, 5.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd15 * 2f / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnSnail), wd15 * 2f / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnMoth), wd15 * 2f / 3f, 3.75f);
            yield return new WaitForSeconds(wd15);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 16)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd16 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd16 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd16 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd16 / 3f, 2.5f);
            InvokeRepeating(nameof(SpawnMosquito), wd16 / 3f, 5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd16 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnSnail), wd16 * 2f / 3f, 2f);
            InvokeRepeating(nameof(SpawnMoth), wd16 * 2f / 3f, 2.75f);
            yield return new WaitForSeconds(wd16);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 17)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd17 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd17 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd17 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd17 / 3f, 2.25f);
            InvokeRepeating(nameof(SpawnMosquito), wd17 / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd17 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnMoth), wd17 * 2f / 3f, 2.75f);
            yield return new WaitForSeconds(wd17);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 18)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd18 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd18 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd18 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd18 / 3f, 2f);
            InvokeRepeating(nameof(SpawnMosquito), wd18 / 3f, 4f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd18 * 2f / 3f, 3f);
            InvokeRepeating(nameof(SpawnSnail), wd18 * 2f / 3f, 1.75f);
            InvokeRepeating(nameof(SpawnMoth), wd18 * 2f / 3f, 2.75f);
            yield return new WaitForSeconds(wd18);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 19)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd19 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd19 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd19 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnFirefly), wd19 / 3f, 1.75f);
            InvokeRepeating(nameof(SpawnMosquito), wd19 / 3f, 3.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd19 * 2f / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnSnail), wd19 * 2f / 3f, 1.75f);
            InvokeRepeating(nameof(SpawnMoth), wd19 * 2f / 3f, 2.5f);
            yield return new WaitForSeconds(wd19);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 20)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd20 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd20 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd20 / 3f, 2.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd20 / 3f, 1.75f);
            InvokeRepeating(nameof(SpawnMosquito), wd20 / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd20 * 2f / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnSnail), wd20 * 2f / 3f, 1.5f);
            InvokeRepeating(nameof(SpawnMoth), wd20 * 2f / 3f, 2.25f);
            yield return new WaitForSeconds(wd20);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 21)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd21 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd21 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd21 / 3f, 2.25f);
            InvokeRepeating(nameof(SpawnFirefly), wd21 / 3f, 1.75f);
            InvokeRepeating(nameof(SpawnMosquito), wd21 / 3f, 3.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd21 * 2f / 3f, 2.5f);
            InvokeRepeating(nameof(SpawnSnail), wd21 * 2f / 3f, 1.5f);
            InvokeRepeating(nameof(SpawnMoth), wd21 * 2f / 3f, 2f);
            yield return new WaitForSeconds(wd21);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnFirefly));
            CancelInvoke(nameof(SpawnMosquito));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }
    void SpawnWasp()       { Spawn(wasp); }
    void SpawnQueenAnt()   { Spawn(queenAnt); }
    void SpawnSnail()      { Spawn(snail); }
    void SpawnMoth()       { Spawn(moth); }
    void SpawnFirefly()    { Spawn(firefly); }
    void SpawnMosquito()   { Spawn(mosquito); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
