using UnityEngine;
using System.Collections;
using TMPro;

public class Level8 : SpawnManager
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
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 7);
        SaveManager.instance.CompleteLevel(8);
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
        SaveManager.instance.CompleteLevel(8);
        Debug.Log("Level 8 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
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
            spawnInterval = 1.75f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 30.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 19.0f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 3)
        {
            waitTime = 1f;
            spawnInterval = 2.0f;
            spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 20.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 15.25f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 4)
        {
            waitTime = 1f;
            spawnInterval = 1.75f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 14.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 15.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 13.75f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 11.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 5)
        {
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 19;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 12.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 12.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 11.5f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 9.25f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnFirefly));

        } else if (wave == 6)
        {
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 10.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 10.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 9.25f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 7.75f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 8.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 3f, 12.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 8.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 9.25f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 7.0f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 21;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 7.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 10.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 8.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 7.75f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 6.25f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 6.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 8.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 7.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 7.0f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 5.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 1f;
            spawnInterval = 3.0f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 5.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 6.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 7.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 6.25f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 4.75f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 4.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 5.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 6.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 6.25f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 4.75f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 3.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 4.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 6.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 5.5f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 4.0f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 3.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.75f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 5.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 5.5f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 4.0f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 2.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.75f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 5.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 4.75f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 3.25f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 2.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 5.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 4.75f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 3.25f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 1.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 4.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 4.0f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 2.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 1.75f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 4.0f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 2.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
            waitTime = 0.5f;
            spawnInterval = 3.0f;
            spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 1.25f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.25f);
            InvokeRepeating(nameof(SpawnSnail), waitTime, 4.25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime, 4.0f);
            InvokeRepeating(nameof(SpawnFirefly), waitTime, 2.125f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
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
