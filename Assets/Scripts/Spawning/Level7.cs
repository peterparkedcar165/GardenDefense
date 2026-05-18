using UnityEngine;
using System.Collections;
using TMPro;

public class Level7 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 750, startHealth = 200;
    private int maxWave = 18;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, snail, moth, firefly;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance.Configure(fertilizerPool);
        GameManager.instance.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 6);
        SaveManager.instance.CompleteLevel(7);
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
                yield return StartCoroutine(RestPeriod(25f));
        }

        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        SaveManager.instance.CompleteLevel(7);
        Debug.Log("Level 7 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            waitTime = 2f;
            spawnInterval = 2.25f;
            spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 2)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd2 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSnail), wd2 * 2f / 3f, 30.5f);
            InvokeRepeating(nameof(SpawnMoth), wd2 * 2f / 3f, 19.25f);
            yield return new WaitForSeconds(wd2);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 3)
        {
            waitTime = 2f;
            spawnInterval = 2.25f;
            spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 20.5f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 15.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 4)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd4 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), wd4 / 3f, 14.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd4 / 3f, 11.75f);
            InvokeRepeating(nameof(SpawnSnail), wd4 * 2f / 3f, 15.5f);
            InvokeRepeating(nameof(SpawnMoth), wd4 * 2f / 3f, 14.0f);
            yield return new WaitForSeconds(wd4);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 5)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd5 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd5 / 3f, 12.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd5 / 3f, 9.5f);
            InvokeRepeating(nameof(SpawnSnail), wd5 * 2f / 3f, 12.5f);
            InvokeRepeating(nameof(SpawnMoth), wd5 * 2f / 3f, 11.75f);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 6)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd6 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd6 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd6 / 3f, 10.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd6 / 3f, 8.0f);
            InvokeRepeating(nameof(SpawnSnail), wd6 * 2f / 3f, 10.5f);
            InvokeRepeating(nameof(SpawnMoth), wd6 * 2f / 3f, 9.5f);
            yield return new WaitForSeconds(wd6);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 7)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd7 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd7 / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd7 / 3f, 7.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd7 * 2f / 3f, 12.5f);
            InvokeRepeating(nameof(SpawnSnail), wd7 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnMoth), wd7 * 2f / 3f, 9.5f);
            yield return new WaitForSeconds(wd7);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 8)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 23;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd8 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd8 / 3f, 7.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd8 / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd8 * 2f / 3f, 10.5f);
            InvokeRepeating(nameof(SpawnSnail), wd8 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnMoth), wd8 * 2f / 3f, 8.0f);
            yield return new WaitForSeconds(wd8);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 9)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd9 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd9 / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd9 / 3f, 5.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd9 * 2f / 3f, 8.5f);
            InvokeRepeating(nameof(SpawnSnail), wd9 * 2f / 3f, 7.5f);
            InvokeRepeating(nameof(SpawnMoth), wd9 * 2f / 3f, 7.25f);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 10)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 19;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd10 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd10 / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd10 / 3f, 5.0f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd10 * 2f / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnSnail), wd10 * 2f / 3f, 7.5f);
            InvokeRepeating(nameof(SpawnMoth), wd10 * 2f / 3f, 6.5f);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 11)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd11 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd11 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd11 / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd11 / 3f, 5.0f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd11 * 2f / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnSnail), wd11 * 2f / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnMoth), wd11 * 2f / 3f, 6.5f);
            yield return new WaitForSeconds(wd11);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 12)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd12 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd12 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd12 / 3f, 4.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd12 / 3f, 4.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd12 * 2f / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnSnail), wd12 * 2f / 3f, 6.5f);
            InvokeRepeating(nameof(SpawnMoth), wd12 * 2f / 3f, 5.75f);
            yield return new WaitForSeconds(wd12);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 13)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd13 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd13 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd13 / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnFirefly), wd13 / 3f, 4.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd13 * 2f / 3f, 4.0f);
            InvokeRepeating(nameof(SpawnSnail), wd13 * 2f / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnMoth), wd13 * 2f / 3f, 5.75f);
            yield return new WaitForSeconds(wd13);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 14)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd14 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd14 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd14 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd14 / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd14 * 2f / 3f, 4.0f);
            InvokeRepeating(nameof(SpawnSnail), wd14 * 2f / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnMoth), wd14 * 2f / 3f, 5.0f);
            yield return new WaitForSeconds(wd14);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 15)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd15 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd15 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd15 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd15 / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd15 * 2f / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnSnail), wd15 * 2f / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnMoth), wd15 * 2f / 3f, 5.0f);
            yield return new WaitForSeconds(wd15);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 16)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd16 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd16 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd16 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd16 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd16 * 2f / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnSnail), wd16 * 2f / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnMoth), wd16 * 2f / 3f, 4.25f);
            yield return new WaitForSeconds(wd16);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 17)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd17 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd17 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd17 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd17 / 3f, 2.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd17 * 2f / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnMoth), wd17 * 2f / 3f, 4.25f);
            yield return new WaitForSeconds(wd17);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 18)
        {
            waitTime = 2f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;
            float wd18 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), wd18 / 3f, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), wd18 / 3f, 3.0f);
            InvokeRepeating(nameof(SpawnFirefly), wd18 / 3f, 2.375f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd18 * 2f / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnSnail), wd18 * 2f / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnMoth), wd18 * 2f / 3f, 4.25f);
            yield return new WaitForSeconds(wd18);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }
    void SpawnWasp()       { Spawn(wasp); }
    void SpawnQueenAnt()   { Spawn(queenAnt); }
    void SpawnSnail()      { Spawn(snail); }
    void SpawnMoth()       { Spawn(moth); }
    void SpawnFirefly()    { Spawn(firefly); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
