using UnityEngine;

public class AbilitiesManager : MonoBehaviour
{
    public enum Ability
    {
        Bomb,
        EMP,
        Repair,
        Repulsion
    }

    [Header("Ability")]
    [SerializeField] private Ability currentAbility;

    [Header("Cooldown")]
    [SerializeField] private float abilityCooldown = 5f;
    private float cooldownTimer;

    [Header("Charges")]
    [SerializeField] private int maxCharges = 2;
    [SerializeField] private int currentCharges;

    [Header("Drag Abilities")]
    [SerializeField] private BombDragAbility bombDragAbility;
    [SerializeField] private EMPDragAbility empDragAbility;
    [SerializeField] private RepairDragAbility repairDragAbility;
    [SerializeField] private RepulsionDragAbility repulsionDragAbility;

    private void Start()
    {
        currentCharges = maxCharges;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public Ability GetCurrentAbility()
    {
        return currentAbility;
    }

    public bool CanUseAbility()
    {
        switch (currentAbility)
        {
            case Ability.Bomb:
            case Ability.Repair:
                return currentCharges > 0;

            case Ability.EMP:
            case Ability.Repulsion:
                return cooldownTimer <= 0f;

            default:
                return false;
        }
    }

    public void UseAbility()
    {
        if (!CanUseAbility())
            return;

        ApplyAbilityCost();

        Debug.Log($"Habilidad seleccionada: {currentAbility}");
    }

    private void ApplyAbilityCost()
    {
        switch (currentAbility)
        {
            case Ability.Bomb:
            case Ability.Repair:
                currentCharges--;
                break;

            case Ability.EMP:
            case Ability.Repulsion:
                cooldownTimer = abilityCooldown;
                break;
        }
    }

    public void SwitchAbility()
    {
        int abilityCount =
            System.Enum.GetValues(typeof(Ability)).Length;

        currentAbility = (Ability)(((int)currentAbility + 1) % abilityCount);

        Debug.Log($"Habilidad seleccionada: {currentAbility}");
    }
}