using UnityEngine;

// central entry point for one-time tutorial popups. call TryShow(id, pages) from wherever a
// tutorial should trigger (level start, a specific UI's first open, etc) - it no-ops if that id
// has already been seen (SaveData.seenTutorials), otherwise shows the pages through
// TutorialPopupUI and marks it seen once the player clicks through the last page.
// per-scene singleton, same convention as SkillTargetingManager/PlantUpgradeUI - lives on the
// same Canvas as its popup, no DontDestroyOnLoad needed since each gameplay scene has its own
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [SerializeField] private TutorialPopupUI popup;

    // id of whichever tutorial the popup is currently running, so gameplay code can gate
    // actions on a specific tutorial being active (see IsTutorialActive/IsWaitingForEvent)
    private string currentId;

    // latches true (for the rest of this session) the first time Level1PlantInfo's Attack
    // upgrade page is reached, or immediately if that tutorial was already completed in an
    // earlier session - see Level1UpgradesLocked
    private bool level1UpgradeGateReached;

    private void Awake()
    {
        instance = this;
        Debug.Log("[Tutorial] TutorialManager.Awake - instance set, popup=" + (popup != null ? popup.name : "NULL"));
    }

    // returns true only if this call actually started showing the popup (lets callers that
    // need a side effect tied to the tutorial genuinely appearing, e.g. pausing the game, avoid
    // doing that when it's already been seen). onComplete, if given, runs right after the
    // tutorial is marked seen - e.g. to resume play once its last page closes
    public bool TryShow(string id, TutorialPage[] pages, System.Action onComplete = null)
    {
        if (string.IsNullOrEmpty(id) || pages == null || pages.Length == 0)
        {
            Debug.Log($"[Tutorial] TryShow('{id}') aborted - id or pages missing (pages count={pages?.Length ?? -1})");
            return false;
        }
        if (SaveManager.instance == null)
        {
            Debug.Log($"[Tutorial] TryShow('{id}') aborted - SaveManager.instance is NULL");
            return false;
        }
        if (popup == null)
        {
            Debug.Log($"[Tutorial] TryShow('{id}') aborted - popup reference is NULL (not wired in the Inspector?)");
            return false;
        }
        if (SaveManager.instance.saveData.seenTutorials.Contains(id))
        {
            Debug.Log($"[Tutorial] TryShow('{id}') skipped - already in seenTutorials (press the debug hotkey to clear it)");
            return false;
        }

        Debug.Log($"[Tutorial] TryShow('{id}') showing {pages.Length} page(s)");
        currentId = id;
        popup.Show(pages, () => { MarkSeen(id); onComplete?.Invoke(); });
        return true;
    }

    // forwards a named in-game action to the active popup, which only reacts if it's currently
    // on a page waiting for this exact id - harmless to call any time, tutorial active or not
    public void NotifyEvent(string eventId)
    {
        Debug.Log($"[Tutorial] NotifyEvent('{eventId}')");
        if (popup == null) return;
        popup.NotifyEvent(eventId);
    }

    // true while this exact tutorial id is the one currently showing - lets gameplay code
    // block unrelated actions (e.g. pausing) for the duration of a specific tutorial
    public bool IsTutorialActive(string id) => popup != null && popup.IsShowing && currentId == id;

    // true while any tutorial popup is up, regardless of which one - used to block actions
    // (e.g. hotkey plant selection) that would let the player skip past what it's teaching
    public bool IsAnyTutorialActive => popup != null && popup.IsShowing;

    // true while Escape should be swallowed instead of reaching Settings/Level Selector/Skill
    // Tree/etc - every one of those systems' own Escape check ANDs this in, so a tutorial can
    // never be backed out of or covered up before its last page is actually reached
    public static bool BlocksEscape => instance != null && instance.IsAnyTutorialActive;

    // true while the active popup is sitting on a page waiting for this exact event id - lets
    // gameplay code temporarily restrict an action to only the one the tutorial is teaching
    // (e.g. only allow placing on Dirt while that page is up)
    public bool IsWaitingForEvent(string eventId) => popup != null && popup.IsShowing && popup.CurrentWaitEventId == eventId;

    // true if placing the Acorn Knight on this tile type would skip ahead of what
    // Level1StartLevel's current page is specifically teaching (see Tile.cs/CursorIcon.cs)
    public bool BlocksAcornKnightPlacement(TileType tileType)
    {
        if (tileType == TileType.Grass && IsWaitingForEvent(TutorialEvents.PlaceAcornKnightOnDirt)) return true;
        if (tileType == TileType.Dirt && IsWaitingForEvent(TutorialEvents.PlaceAcornKnightOnGrass)) return true;
        return false;
    }

    // true while plant path upgrades (Attack/Passive/Skill, any plant) should be completely
    // blocked because Level1PlantInfo hasn't yet taught the system - see PlantUpgradeUI's
    // OnPathXUpgradeClicked. only ever applies during level 1, and latches permanently false
    // the moment the tutorial's Attack-upgrade page is reached, so it never re-locks afterward
    public bool Level1UpgradesLocked()
    {
        if (level1UpgradeGateReached) return false;
        if (SaveManager.instance != null && SaveManager.instance.saveData.seenTutorials.Contains(TutorialIds.Level1PlantInfo))
        {
            level1UpgradeGateReached = true;
            return false;
        }
        if (ProceduralLevel.CurrentConfig == null || ProceduralLevel.CurrentConfig.levelNumber != 1)
            return false;
        if (IsWaitingForEvent(TutorialEvents.UpgradeAcornAttackPath))
        {
            level1UpgradeGateReached = true;
            return false;
        }
        return true;
    }

    private void MarkSeen(string id)
    {
        Debug.Log($"[Tutorial] MarkSeen('{id}') - tutorial complete, saving");
        currentId = null;
        SaveManager.instance.saveData.seenTutorials.Add(id);
        SaveManager.instance.Save();
    }
}
