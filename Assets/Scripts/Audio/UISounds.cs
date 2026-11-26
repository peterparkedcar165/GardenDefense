using UnityEngine;

// the ui's own sounds that code plays (Tommy, 2026-11-26), loaded from Resources/Sounds/UI and played like
// the other sfx (SfxPlayer: the game's sound mixer group, the same as the button click), so they need no
// wiring in any scene. they play while the game is paused too
public static class UISounds
{
    private static SoundEffect slide;
    private static int slideFrame = -1;

    // a panel, card or title sliding into place (sliding.mp3, Tommy: volume 1, pitch 0.9 to 1.1). once a
    // frame at most, so things that start sliding together sound once
    public static void Slide()
    {
        if (slideFrame == Time.frameCount) return;
        if (slide == null)
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/UI/sliding");
            if (clip == null) return;
            slide = new SoundEffect
            {
                clips = new[] { new SoundClip { clip = clip, volume = 1f, minPitch = 0.9f, maxPitch = 1.1f } },
            };
        }
        slideFrame = Time.frameCount;
        SfxPlayer.Play(slide, Vector3.zero);
    }
}
