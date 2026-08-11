using UnityEngine;

public class DroneAbilityController : MonoBehaviour
{
    public enum Ability
    {
        Bomb,
        EMP
    }

    [Header("Ability")]
    [SerializeField] private Ability currentAbility;

    [Header("Cooldown")]
    [SerializeField] private float abilityCooldown = 5f;
    [SerializeField] private float cooldownTimer;

    [Header("Charges")]
    [SerializeField] private int maxCharges = 2;
    private int currentCharges;

    [Header("Abilities")]
    [SerializeField] private BombAbility bombAbility;
    [SerializeField] private EMPAbility empAbility;

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

        if (currentAbility == Ability.Bomb)
        {
            currentCharges--;
        }
        else if (currentAbility == Ability.EMP)
        {
            cooldownTimer = abilityCooldown;
        }

        Debug.Log($"Habilidad activada: {currentAbility}");
    }

    private bool CanUseAbility()
    {
        if (currentAbility == Ability.Bomb)
        {
            return currentCharges > 0;
        }

        if (currentAbility == Ability.EMP)
        {
            return cooldownTimer <= 0f;
        }

        return false;
    }

    private void ActivateAbility()
    {
        if (currentAbility == Ability.Bomb)
        {
            if (bombAbility != null)
            {
                bombAbility.DropBomb();
            }
        }
        else if (currentAbility == Ability.EMP)
        {
            if (empAbility != null)
            {
                empAbility.ActivateEMP();
            }
        }
    }

    public void SwitchAbility()
    {
        currentAbility = currentAbility == Ability.Bomb
            ? Ability.EMP
            : Ability.Bomb;

        UpdateGizmos();

        Debug.Log($"Habilidad seleccionada: {currentAbility}");
    }

    private void UpdateGizmos()
    {
        bombAbility.SetGizmoVisible(currentAbility == Ability.Bomb);

        empAbility.SetGizmoVisible(currentAbility == Ability.EMP);
    }
}