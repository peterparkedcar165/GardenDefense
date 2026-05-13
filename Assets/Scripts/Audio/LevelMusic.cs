using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private float crossfadeDuration = 1f;

    private void Start()
    {
        if (MusicManager.instance != null && music != null)
            MusicManager.instance.Play(music, crossfadeDuration);
    }
}
