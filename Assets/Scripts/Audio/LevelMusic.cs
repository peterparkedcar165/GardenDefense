using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private float crossfadeDuration = 1f;
    [Tooltip("per track volume, lower this for louder files")]
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private void Start()
    {
        if (MusicManager.instance != null && music != null)
            MusicManager.instance.Play(music, crossfadeDuration, volume);
    }
}
