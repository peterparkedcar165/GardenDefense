using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// click-anywhere-on-the-box tutorial dialogue: shows one page at a time from the array passed to
// Show(). a normal page advances on click; a page with TutorialPage.waitForEventId set stays up
// without consuming clicks (see RefreshPage) and only advances when TutorialManager.NotifyEvent
// is called with that id. clicking the last page's click (or its matching event) closes the
// popup and fires onComplete (TutorialManager uses that to mark the tutorial seen)
[RequireComponent(typeof(Image))]
public class TutorialPopupUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text continueHintText;
    // full-screen invisible panel, siblings with this popup under TutorialSystem (not a child of
    // it, so it isn't confined to the popup's own small rect). active only on a non-waiting
    // ("click to continue") page, so nothing else on screen is clickable while that text is up -
    // a waiting page leaves it off so the player can freely interact with the game underneath
    [SerializeField] private GameObject screenBlocker;

    private Image background;
    private TutorialPage[] pages;
    private int pageIndex;
    private System.Action onComplete;

    public bool IsShowing => pages != null && gameObject.activeSelf;
    public string CurrentWaitEventId => (pages != null && pageIndex < pages.Length) ? pages[pageIndex].waitForEventId : null;

    private void Awake()
    {
        background = GetComponent<Image>();
    }

    public void Show(TutorialPage[] pages, System.Action onComplete)
    {
        Debug.Log($"[Tutorial] Popup.Show - {pages.Length} page(s)");
        this.pages = pages;
        this.onComplete = onComplete;
        pageIndex = 0;
        gameObject.SetActive(true);
        RefreshPage();
    }

    private void RefreshPage()
    {
        TutorialPage page = pages[pageIndex];
        bool waitsForEvent = !string.IsNullOrEmpty(page.waitForEventId);

        if (bodyText != null) bodyText.text = page.text;

        // a page waiting on a specific game event doesn't consume clicks - let them pass through
        // to whatever's underneath (a plant slot, the confirm button, etc) instead of this popup
        // swallowing the click and advancing itself
        if (background != null) background.raycastTarget = !waitsForEvent;
        // and when it's NOT waiting on an event, block the rest of the screen too, so the only
        // thing clickable is the popup itself
        if (screenBlocker != null) screenBlocker.SetActive(!waitsForEvent);

        if (continueHintText != null)
        {
            continueHintText.gameObject.SetActive(!waitsForEvent);
            continueHintText.text = waitsForEvent ? "" : (pageIndex == pages.Length - 1 ? "(click to close)" : "(click to continue)");
        }

        Debug.Log($"[Tutorial] Popup page {pageIndex + 1}/{pages.Length} - waitsForEvent={(waitsForEvent ? page.waitForEventId : "none")} - \"{page.text}\"");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // only left click advances the popup - right click is used elsewhere (deselecting a
        // plant, canceling skill targeting) and shouldn't double as a way to click through this
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // pages can go null if a script recompile/domain reload happened while this popup was
        // still showing (editing a .cs file mid-Play-session resets non-serialized fields like
        // this one, but doesn't deactivate the GameObject or clear its stale text) - just hide it
        // instead of throwing on a click against that leftover state
        if (pages == null)
        {
            Debug.LogWarning("[Tutorial] Popup clicked with no active page data (likely a stale popup after a script reload) - hiding it");
            gameObject.SetActive(false);
            if (screenBlocker != null) screenBlocker.SetActive(false);
            return;
        }
        Debug.Log("[Tutorial] Popup clicked");
        Advance();
    }

    // called by TutorialManager whenever a named in-game action fires - only advances if the
    // current page is actually waiting on this exact event, otherwise ignored
    public void NotifyEvent(string eventId)
    {
        if (pages == null || pageIndex >= pages.Length) return;
        if (pages[pageIndex].waitForEventId != eventId)
        {
            Debug.Log($"[Tutorial] Popup.NotifyEvent('{eventId}') ignored - current page waits for '{pages[pageIndex].waitForEventId}'");
            return;
        }
        Debug.Log($"[Tutorial] Popup.NotifyEvent('{eventId}') matched current page - advancing");
        Advance();
    }

    private void Advance()
    {
        if (pages == null) { gameObject.SetActive(false); return; } // see OnPointerClick's guard
        pageIndex++;
        if (pageIndex >= pages.Length)
        {
            Debug.Log("[Tutorial] Popup closed - last page reached");
            gameObject.SetActive(false);
            if (screenBlocker != null) screenBlocker.SetActive(false);
            System.Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
            return;
        }
        RefreshPage();
    }
}
