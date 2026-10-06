// one page of a tutorial popup's text.
// waitForEventId left blank (the common case): clicking the popup advances to the next page.
// waitForEventId set: this page does NOT consume/advance on click - clicks pass straight through
// to the game underneath, and the page only advances once TutorialManager.NotifyEvent() is
// called with this exact id, from wherever that actual action happens in the game (a hover, a
// specific button click, etc)
public struct TutorialPage
{
    public string text;
    public string waitForEventId;

    public TutorialPage(string text, string waitForEventId = null)
    {
        this.text = text;
        this.waitForEventId = waitForEventId;
    }
}
