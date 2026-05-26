using UnityEngine;
using System.Collections;

public class Level6 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 750, startHealth = 200;
    private int maxWave = 20;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, snail, moth;
    public GameObject weatherManager;
    public float nextWaveTimer;
    public float restInterval = 5f;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        if (WeatherManager.instance) WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance?.Configure(fertilizerPool);
        GameManager.instance?.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 5);
        SaveManager.instance.CompleteLevel(6);
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
        SaveManager.instance.CompleteLevel(6);
        Debug.Log("Level 6 completed");
    }

    IEnumerator Wave(int wave)
    {
        // phase 1 waves 1 to 5 setup
        // double phase at wave 1 (worker then scout), scout joins wave 2
        // soldier joins wave 3, fruit fly joins wave 4
        // double phase at wave 5 (workers+scouts then soldiers+flies with wasp interjection)

        if (wave == 1)
        {
            // double phase: worker stream then scout stream
            waitTime = 2f; spawnInterval = 4f; spawnCount = 8;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = 2f * wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 2)
        {
            // worker + scout concurrent, offset start
            waitTime = 2f; spawnInterval = 4f; spawnCount = 8;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), wd / 3f, spawnInterval);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 3)
        {
            // soldier joins, moth appears as rare interjection
            waitTime = 2f; spawnInterval = 3.5f; spawnCount = 8;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 2f, 7f);
            InvokeRepeating(nameof(SpawnMoth), wd / 2f, 35f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 4)
        {
            // fruit fly joins, snail appears as rare interjection
            waitTime = 2f; spawnInterval = 3f; spawnCount = 8;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 6f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 8f);
            InvokeRepeating(nameof(SpawnMoth), wd / 2f, 30f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 40f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnSnail));

        } else if (wave == 5)
        {
            // double phase: phase1 workers+scouts, phase2 soldiers+flies with wasp interjection
            waitTime = 2f; spawnInterval = 3f; spawnCount = 8;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = 2f * wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), waitTime + 8f, 14f);
            InvokeRepeating(nameof(SpawnMoth), waitTime + 12f, 35f);
            InvokeRepeating(nameof(SpawnSnail), waitTime + 20f, 40f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnSnail));

        // phase 2 waves 6 to 13 rising pressure
        // wasp joins wave 6, queen ant joins wave 8

        } else if (wave == 6)
        {
            // wasp joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 9;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 8f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 18f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 40f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnSnail));

        } else if (wave == 7)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 10;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 5f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 7f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 15f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 32f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnSnail));

        } else if (wave == 8)
        {
            // queen ant joins, double phase
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 10;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = 2f * wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 5f);
            InvokeRepeating(nameof(SpawnMoth), wd / 2f, 30f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnMoth));

            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 5f);
            InvokeRepeating(nameof(SpawnQueenAnt), waitTime + 8f, 20f);
            InvokeRepeating(nameof(SpawnSnail), waitTime + 10f, 25f);
            InvokeRepeating(nameof(SpawnMoth), waitTime + 6f, 15f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 9)
        {
            // all 8 types
            waitTime = 2f; spawnInterval = 2.25f; spawnCount = 10;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 6f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 22f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 12f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 25f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 10)
        {
            waitTime = 2f; spawnInterval = 2.25f; spawnCount = 11;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 18f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 10f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 22f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 11)
        {
            waitTime = 2f; spawnInterval = 2.25f; spawnCount = 11;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 4f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 4f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 5f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 16f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 9f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 20f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 12)
        {
            // double phase: phase1 workers+scouts+soldiers, phase2 workers+flies+wasp+queen
            waitTime = 2f; spawnInterval = 2f; spawnCount = 11;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = 2f * wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, 4f);
            InvokeRepeating(nameof(SpawnMoth), waitTime + 12f, 30f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnMoth));

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), waitTime, 3f);
            InvokeRepeating(nameof(SpawnSnail), waitTime + 6f, 14f);
            InvokeRepeating(nameof(SpawnQueenAnt), waitTime + 8f, 18f);
            InvokeRepeating(nameof(SpawnMoth), waitTime + 6f, 9f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnQueenAnt));
            CancelInvoke(nameof(SpawnMoth));

        } else if (wave == 13)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 11;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 4f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 4f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 4.5f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 14f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 8f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 18f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        // phase 3 waves 14 to 20 tough but toned down for darkness biome
        // all 8 types, intervals tighten, counts rise steadily

        } else if (wave == 14)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 11;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 3.6f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 3.6f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 4f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 12f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 7f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 16f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 15)
        {
            waitTime = 2f; spawnInterval = 1.9f; spawnCount = 12;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 3.4f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 3.4f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 3.8f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 11f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 7f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 15f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 16)
        {
            waitTime = 2f; spawnInterval = 1.8f; spawnCount = 13;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 3.2f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 3.2f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 3.5f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 10f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 6f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 14f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 17)
        {
            waitTime = 2f; spawnInterval = 1.7f; spawnCount = 14;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 3f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 3f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 3.2f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 9f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 6f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 12f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 18)
        {
            waitTime = 2f; spawnInterval = 1.6f; spawnCount = 15;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 2.8f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 2.8f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 3f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 9f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 5.5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 11f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 19)
        {
            waitTime = 2f; spawnInterval = 1.5f; spawnCount = 16;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 2.6f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 2.6f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 2.8f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 8f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 20)
        {
            // final wave
            waitTime = 2f; spawnInterval = 1.2f; spawnCount = 25;
            float wd = waitTime + ((spawnCount - 1) * spawnInterval);
            nextWaveTimer = wd + restInterval;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd / 3f, 2.4f);
            InvokeRepeating(nameof(SpawnFruitFly), wd / 3f, 2.4f);
            InvokeRepeating(nameof(SpawnWasp), wd / 3f, 2.5f);
            InvokeRepeating(nameof(SpawnSnail), wd * 2f / 3f, 8f);
            InvokeRepeating(nameof(SpawnMoth), wd / 3f, 5f);
            InvokeRepeating(nameof(SpawnQueenAnt), wd * 2f / 3f, 10f);
            yield return new WaitForSeconds(wd);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnSnail));
            CancelInvoke(nameof(SpawnMoth));
            CancelInvoke(nameof(SpawnQueenAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { workerAnt, soldierAnt, scoutAnt, fruitFly, wasp, queenAnt, snail, moth };

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }
    void SpawnWasp()       { Spawn(wasp); }
    void SpawnQueenAnt()   { Spawn(queenAnt); }
    void SpawnSnail()      { Spawn(snail); }
    void SpawnMoth()       { Spawn(moth); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
