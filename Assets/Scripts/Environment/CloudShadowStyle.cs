using UnityEngine;

// the cloud shadows' look and numbers (Tommy, 2026-11-26): his black cloud shapes drifting over the map
// on levels with sun (CloudShadows). lives at Resources/CloudShadowStyle.asset, so tuning it in the
// inspector changes the next level that starts
[CreateAssetMenu(fileName = "CloudShadowStyle", menuName = "Scriptable Objects/Cloud Shadow Style")]
public class CloudShadowStyle : ScriptableObject
{
    private static CloudShadowStyle _instance;
    public static CloudShadowStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<CloudShadowStyle>("CloudShadowStyle");
            if (_instance == null) _instance = CreateInstance<CloudShadowStyle>();
            return _instance;
        }
    }

    [Tooltip("Tommy's cloud shadows (Sprites/CloudShadows): pure black, hard edged, 64 px a tile like the map, so " +
        "they're drawn at their own size, never scaled. each shadow picks one at random")]
    public Sprite[] shapes;
    [Tooltip("unlit (Sprite-Unlit-Default), so the level's lights never brighten or tint them")]
    public Material material;

    [Header("Look")]
    [Tooltip("how dark a shadow is: the black shape's alpha")]
    [Range(0f, 1f)] public float opacity = 0.3f;
    [Tooltip("above everything in the world (the Weather layer, over rain and snow) and under the indicators and the ui")]
    public string sortingLayer = "Weather";
    public int sortingOrder = 1000;

    [Header("Numbers (Tommy tunes these after playing; every one changes live while playing)")]
    [Tooltip("shadows over the camera's view at once")]
    [Min(0)] public int count = 10;
    [Tooltip("whole times the shapes' own size: 1 is the file's own pixels (64 a tile, about one tile across); 2, 3 " +
        "and up keep every pixel square and the same size (no mixels), just bigger than the map's")]
    [Range(1, 8)] public int scale = 1;
    [Tooltip("world units (tiles) a real second; the same whatever the game speed, and on while paused, like the rain")]
    [Min(0f)] public float speed = 0.35f;
    [Tooltip("the direction they drift, in degrees up from straight right: right and a little up")]
    [Range(0f, 90f)] public float angle = 20f;
    [Tooltip("real seconds to fade in or out when the sun comes or goes, or rain, snow or a sandstorm starts or ends")]
    [Min(0.01f)] public float fadeSeconds = 2f;
}
