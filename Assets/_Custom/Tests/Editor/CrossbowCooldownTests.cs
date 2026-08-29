using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CrossbowCooldownTests
{
    private GameObject _object;

    [TearDown]
    public void Limpiar()
    {
        if (_object != null)
            Object.DestroyImmediate(_object);
    }

    [Test]
    public void CooldownProgress_ReflejaElTiempoRealDeRecarga()
    {
        _object = new GameObject("Crossbow Cooldown Test");
        CrossbowRainAttack attack = _object.AddComponent<CrossbowRainAttack>();
        SetPrivateField(attack, "attackCooldown", 8f);
        SetPrivateField(attack, "cooldownTimer", 4f);

        Assert.IsTrue(attack.IsRecharging);
        Assert.AreEqual(0.5f, attack.CooldownProgress, 0.001f);
    }

    [Test]
    public void CooldownProgress_ListaDevuelveUno()
    {
        _object = new GameObject("Crossbow Cooldown Test");
        CrossbowRainAttack attack = _object.AddComponent<CrossbowRainAttack>();

        Assert.IsFalse(attack.IsRecharging);
        Assert.AreEqual(1f, attack.CooldownProgress, 0.001f);
    }

    [Test]
    public void CrossbowPrefab_IncluyeIndicadorVisible()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/JeanAssets/Prefabs/CrossbowSlot.prefab");

        Assert.IsNotNull(prefab.GetComponentInChildren<CrossbowCooldownIndicator>(true));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
