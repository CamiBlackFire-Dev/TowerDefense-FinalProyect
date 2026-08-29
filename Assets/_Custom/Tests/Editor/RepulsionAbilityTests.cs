using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RepulsionAbilityTests
{
    private GameObject _abilityObject;
    private GameObject _enemyObject;

    [TearDown]
    public void Limpiar()
    {
        if (_abilityObject != null)
            Object.DestroyImmediate(_abilityObject);
        if (_enemyObject != null)
            Object.DestroyImmediate(_enemyObject);
    }

    [Test]
    public void ActivateRepulsion_SinPathGlobal_NoLanzaExcepcion()
    {
        _abilityObject = new GameObject("Repulsion Test");
        RepulsionAbility ability = _abilityObject.AddComponent<RepulsionAbility>();
        FieldInfo enemyLayer = typeof(RepulsionAbility).GetField("enemyLayer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        enemyLayer.SetValue(ability, (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));

        _enemyObject = new GameObject("Enemy Test");
        _enemyObject.layer = LayerMask.NameToLayer("Enemy");
        _enemyObject.AddComponent<BoxCollider>();
        _enemyObject.AddComponent<TestEnemyMovement>();
        Physics.SyncTransforms();

        Assert.DoesNotThrow(() => ability.ActivateRepulsion(_enemyObject.transform.position));
    }
}
