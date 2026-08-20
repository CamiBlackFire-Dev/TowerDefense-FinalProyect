using UnityEngine;

[CreateAssetMenu(
    fileName = "ArrowData",
    menuName = "Tower Defense/Arrow Data"
)]
public class ArrowData : ScriptableObject
{
    [Header("Identity")]
    public string arrowName;

    [Header("Projectile")]
    public GameObject prefab;
    public float speed = 10f;

    [Header("Stats")]
    public float damage = 10f;

    [Header("Effect")]
    public ArrowEffectType effectType;
    public float effectDuration = 3f;
    public float effectValue = 0.5f;

    [Header("Visual Effect")]
    public GameObject effectPrefab;
}

public enum ArrowEffectType
{
    Normal,
    Poison,
    Fire,
    Ice
}