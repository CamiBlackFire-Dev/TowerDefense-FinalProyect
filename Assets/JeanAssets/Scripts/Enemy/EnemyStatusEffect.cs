using System.Collections;
using UnityEngine;

public class EnemyStatusEffects : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private TestEnemyMovement enemyMovement;

    private Coroutine burnCoroutine;
    private Coroutine poisonCoroutine;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyMovement = GetComponent<TestEnemyMovement>();
    }

    // FIRE
    public void ApplyBurn(float damagePerSecond, float duration)
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }

        burnCoroutine = StartCoroutine(
            BurnCoroutine(damagePerSecond, duration)
        );
    }

    private IEnumerator BurnCoroutine(
        float damagePerSecond,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (enemyHealth == null ||
                !enemyHealth.IsAlive)
            {
                break;
            }

            enemyHealth.TakeDamage(
                damagePerSecond * Time.deltaTime
            );

            timer += Time.deltaTime;

            yield return null;
        }

        burnCoroutine = null;
    }

    // POISON
    public void ApplyPoison(
        float damagePerTick,
        float tickInterval,
        float duration)
    {
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
        }

        poisonCoroutine = StartCoroutine(
            PoisonCoroutine(
                damagePerTick,
                tickInterval,
                duration
            )
        );
    }

    private IEnumerator PoisonCoroutine(
        float damagePerTick,
        float tickInterval,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (enemyHealth == null ||
                !enemyHealth.IsAlive)
            {
                break;
            }

            enemyHealth.TakeDamage(damagePerTick);

            yield return new WaitForSeconds(tickInterval);

            timer += tickInterval;
        }

        poisonCoroutine = null;
    }

    // ICE
    public void ApplyIce(float speedMultiplier, float duration)
    {
        if (enemyMovement == null)
            return;

        enemyMovement.ApplySlow(
            speedMultiplier,
            duration
        );
    }
}