// which plant's portrait/name a tutorial popup shows while it's up - resolved against
// TutorialManager's own speaker references (see TutorialManager.ResolveSpeaker) rather than each
// TutorialPage carrying its own portrait/name, since every page within one TryShow() call is
// always the same speaker
public enum TutorialSpeaker
{
    AcornKnight,
    Sunflower,
    Waterlily,
}
