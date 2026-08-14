using UnityEngine;

public class DroneAbilityController : MonoBehaviour
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
    [SerializeField] private float cooldownTimer;

    [Header("Charges")]
    [SerializeField] private int maxCharges = 2;
    [SerializeField] private int currentCharges;

    [Header("Abilities")]
    [SerializeField] private BombAbility bombAbility;
    [SerializeField] private EMPAbility empAbility;
    [SerializeField] private RepairAbility repairAbility;
    [SerializeField] private RepulsionAbility repulsionAbility;

    private void Start()
    {
        currentCharges = maxCharges;

        UpdateGizmos();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void UseAbility()
    {
        if (!CanUseAbility())
            return;

        ActivateAbility();

        ApplyAbilityCost();

        Debug.Log($"Habilidad activada: {currentAbility}");
    }

    private bool CanUseAbility()
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

    private void ActivateAbility()
    {
        switch (currentAbility)
        {
            case Ability.Bomb:

                if (bombAbility != null)
                    bombAbility.DropBomb();

                break;

            /*case Ability.EMP:

                if (empAbility != null)
                    empAbility.ActivateEMP();

                break;*/

            case Ability.Repair:

                if (repairAbility != null)
                    repairAbility.RepairTowers();

                break;

            case Ability.Repulsion:

                if (repulsionAbility != null)
                    repulsionAbility.ActivateRepulsion();

                break;
        }
    }

    public void SwitchAbility()
    {
        int abilityCount = System.Enum.GetValues(typeof(Ability)).Length;

        currentAbility =
            (Ability)(((int)currentAbility + 1) % abilityCount);

        UpdateGizmos();

        Debug.Log($"Habilidad seleccionada: {currentAbility}");
    }

    private void UpdateGizmos()
    {
        if (bombAbility != null)
            bombAbility.SetGizmoVisible(currentAbility == Ability.Bomb);

        /*if (empAbility != null)
            empAbility.SetGizmoVisible(currentAbility == Ability.EMP);*/

        if (repairAbility != null)
            repairAbility.SetGizmoVisible(currentAbility == Ability.Repair);

        if (repulsionAbility != null)
            repulsionAbility.SetGizmoVisible(currentAbility == Ability.Repulsion);
    }
}