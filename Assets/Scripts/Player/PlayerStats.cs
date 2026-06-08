using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("RPG attributes")]
    public int strength = 5;
    public int agility = 5;

    [Header("Equipped armor")]
    public float armorWeight = 0f;

    [Header("Strength scaling")]
    public float baseCarry = 10f;
    public float carryPerStrength = 2f;

    [Header("Agility scaling")]
    public float speedPerAgility = 0.1f;

    [Header("Armor penalties")]
    public float staminaPenaltyPerWeight = 1.5f;
    public float speedPenaltyPerWeight = 0.05f;
    public float strengthMitigationPerPoint = 0.04f;
    public float agilityMitigationPerPoint = 0.04f;

    public float CarryCapacity => baseCarry + strength * carryPerStrength;

    public float ArmorMitigation =>
        Mathf.Clamp01(1f - (strength * strengthMitigationPerPoint + agility * agilityMitigationPerPoint));

    public float StaminaPenalty => armorWeight * staminaPenaltyPerWeight * ArmorMitigation;

    public float SpeedModifier =>
        agility * speedPerAgility - armorWeight * speedPenaltyPerWeight * ArmorMitigation;
}
