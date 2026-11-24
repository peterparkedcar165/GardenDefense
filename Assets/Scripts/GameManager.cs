using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public int SunCount;
    // cumulative totals for the current playthrough, read by the in-game info panel (G)
    public int totalSunGenerated;
    public int totalSunSpent;
    public int playerHealth;
    [SerializeField] private GameObject germinateBloomPrefab;
    [System.NonSerialized] public int playerMaxHealth = 200;
    public float BonusSunGain;
    public AudioSource audioSource;
    public AudioClip buttonClick, plantPlace, plantSelect;
    private bool paused = false;
    public float gameSpeed = 1f;
    private float[] speeds = { 0.5f, 1f, 2f, 4f };
    private int speedIndex = 1; // starts at 1x
    public int currentWave = 0;

    protected void Awake()
    {
        instance = this;
        GerminateEffect.bloomPrefab = germinateBloomPrefab;
    }

    public void AddSun(int amount)
    {
        // solar loan: sun still owed is paid out of what comes in first
        int gained = FertilizerManager.PayDebtFrom(Mathf.RoundToInt(amount * (1 + BonusSunGain)));
        SunCount += gained;
        totalSunGenerated += gained;
        UpdateSun();
    }

    public bool SpendSun(int amount)
    {
        if (SunCount >= amount)
        {
            SunCount -= amount;
            totalSunSpent += amount;
            Debug.Log("Successfully spent: " + amount + " of suns");
            UpdateSun();
            return true;
        }
        Debug.Log("Not enough sun!");
        return false;
    }

    public void UpdateSun() => GameHUD.instance?.SetSun(SunCount);

    public void UpdateHealth()
    {
        if (playerHealth < 0) playerHealth = 0;
        GameHUD.instance?.SetHealth(playerHealth);
    }

    public void Damage(int damage)
    {
        playerHealth -= damage;
        Debug.Log("Player health: " + playerHealth);
        UpdateHealth();
        if (playerHealth <= 0)
        {
            GameOver();
        }
    }

    // set at 0 health. the game then stays stopped: unpausing, the speed buttons or closing the
    // settings panel no longer start time again (restart or leave the level instead)
    private bool gameOver;

    private void GameOver()
    {
        Debug.Log("Game Over");
        gameOver = true;
        Time.timeScale = 0; // pauses game
        // show game over screen later
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    // space works the start / pause / resume button like a click (Tommy, 2026-11-21): while it's
    // down the button shows pressed, and letting go presses it. a press that started while a menu
    // was open is ignored, and one released after a menu opened only lets the button back up
    private bool spaceHeld;

    // the canvas outlives this manager, so a level left with space still down mustn't leave the
    // button stuck pressed
    private void OnDisable()
    {
        if (!spaceHeld) return;
        spaceHeld = false;
        GameHUD.instance?.HoldPauseButton(false);
    }

    private bool SpaceAllowed =>
        (SettingsManager.instance == null || !SettingsManager.instance.IsOpen)
        && (FertilizerSelectionUI.instance == null || !FertilizerSelectionUI.instance.IsOpen)
        && (MulchDebugMenu.instance == null || !MulchDebugMenu.instance.IsOpen)
        && (LoadoutSelectionUI.instance == null || !LoadoutSelectionUI.instance.IsOpen);

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && SpaceAllowed)
        {
            spaceHeld = true;
            GameHUD.instance?.HoldPauseButton(true);
        }
        if (spaceHeld && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            spaceHeld = false;
            GameHUD.instance?.HoldPauseButton(false);
            if (SpaceAllowed) TogglePause();
        }

        // cheatcode m grants 99999 sun while in a level
        if (IsGameActive && Keyboard.current.mKey.wasPressedThisFrame)
        {
            SunCount += 99999;
            UpdateSun();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // dev shortcut n opens the mulch, fertilizer and weather test menu while in a level. checked
        // here, not in MulchDebugMenu, because that script sits on its own panel, which is switched off
        // (so its Update doesn't run) whenever the panel is closed
        if (IsGameActive && Keyboard.current.nKey.wasPressedThisFrame
            && MulchDebugMenu.instance != null && !MulchDebugMenu.instance.IsOpen)
            MulchDebugMenu.instance.Show();
#endif
    }

    // true once the player has ever unpaused - the level sits paused at wave 0 waiting for this,
    // so Space or the Pause/Resume button (labelled START until then) doubles as the start action
    public bool HasStarted { get; private set; }

    public void TogglePause()
    {
        // Level1PlantInfo, Level1FirstMulch, Level1SoldierAntAppears and
        // Level2Primers each pause the game the moment they open (see
        // ProceduralLevel/SoldierAnt/ElementalDebuff) and block Start/Space outright for their
        // entire run, with no exception page - the player gets control back only once the last
        // page closes and currentId clears
        if (TutorialManager.instance != null && (TutorialManager.instance.IsTutorialActive(TutorialIds.Level1PlantInfo)
            || TutorialManager.instance.IsTutorialActive(TutorialIds.Level1FirstMulch)
            || TutorialManager.instance.IsTutorialActive(TutorialIds.Level1SoldierAntAppears)
            || TutorialManager.instance.IsTutorialActive(TutorialIds.Level2Primers)))
            return;

        // Level1StartLevel blocks Start/Space for every page except its last, which is itself
        // waiting on this exact action (see TutorialContent.Level1StartLevel) - so only that
        // page lets the toggle through, and announces the event on the way
        if (TutorialManager.instance != null && TutorialManager.instance.IsTutorialActive(TutorialIds.Level1StartLevel)
            && !TutorialManager.instance.IsWaitingForEvent(TutorialEvents.ClickStartLevelButton))
            return;

        TutorialManager.instance?.NotifyEvent(TutorialEvents.ClickStartLevelButton);
        SetPause(!paused);
    }

    public void SetPause(bool value)
    {
        paused = value;
        if (!paused) HasStarted = true;
        Time.timeScale = paused || gameOver ? 0f : gameSpeed;
        GameHUD.instance?.SetPauseButton(paused, HasStarted);
    }

    public void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speeds.Length;
        gameSpeed = speeds[speedIndex];
        if (!paused && !gameOver) Time.timeScale = gameSpeed;
        GameHUD.instance?.SetSpeedButton(gameSpeed);
    }

    public void DecreaseSpeed()
    {
        speedIndex = (speedIndex - 1 + speeds.Length) % speeds.Length;
        gameSpeed = speeds[speedIndex];
        if (!paused && !gameOver) Time.timeScale = gameSpeed;
        GameHUD.instance?.SetSpeedButton(gameSpeed);
    }

    public bool IsGameActive { get; private set; }

    public void InitiateLevel(int sunCount, int health)
    {
        IsGameActive = true;
        HasStarted = false;
        gameOver = false;
        SunCount = sunCount;
        totalSunGenerated = 0;
        totalSunSpent = 0;
        playerMaxHealth = health;
        playerHealth = playerMaxHealth;
        UpdateSun();
        UpdateHealth();
        GameHUD.instance?.SetSpeedButton(gameSpeed);
        PersistentCanvas.instance?.Show();
        PlantBar.instance?.Clear();
        SetPause(true);
        LoadoutSelectionUI.instance?.Show();
    }

    public void OnLoadoutConfirmed()
    {
        PlantBar.instance?.Build();
        FertilizerSelectionUI.instance?.Show();
    }

    public void Restart()
    {
        Debug.Log($"[Restart] Sun: {SunCount} | Health: {playerHealth}");
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        LoadoutSelectionUI.instance?.Hide();
        FertilizerSelectionUI.instance?.CloseForSceneExit();
        MulchDebugMenu.instance?.CloseForSceneExit();
        SceneManager.LoadScene("MainMenu");
    }

}
