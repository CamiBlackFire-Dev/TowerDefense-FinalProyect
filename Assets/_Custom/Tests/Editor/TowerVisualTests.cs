using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Pruebas de la apariencia de las torres con modelos arbitrarios.
public class TowerVisualTests
{
    private GameObject _object;

    [TearDown]
    public void Limpiar()
    {
        if (_object != null)
            Object.DestroyImmediate(_object);
    }

    // Una torre sin modelo crea su propio cubo visual.
    [Test]
    public void SinModelo_CreaUnCuboVisual()
    {
        _object = new GameObject("Tower Test");
        Tower tower = _object.AddComponent<Tower>();

        Assert.IsNotNull(tower.Visual);
        Assert.AreEqual(1, _object.transform.childCount);
    }

    // El color del nivel se aplica a un renderer en un hijo (modelo tipico).
    [Test]
    public void ConRendererEnHijo_AplicaElColorDelNivel()
    {
        _object = new GameObject("Tower Test");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(_object.transform, false);
        Tower tower = _object.AddComponent<Tower>();

        tower.SetLevel(2);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        model.GetComponent<Renderer>().GetPropertyBlock(block);
        Color color = block.GetColor("_BaseColor");
        Assert.AreEqual(1f, color.g, 0.01f); // nivel 2 de la paleta por defecto: verde
    }

    // La escala original del modelo se respeta al aplicar niveles.
    [Test]
    public void RespetaLaEscalaOriginalDelModelo()
    {
        _object = new GameObject("Tower Test");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(_object.transform, false);
        model.transform.localScale = new Vector3(2f, 3f, 1f);
        Tower tower = _object.AddComponent<Tower>();
        tower.Visual.baseScale = 1f;

        tower.SetLevel(1);

        Assert.AreEqual(2f, model.transform.localScale.x, 0.01f);
        Assert.AreEqual(3f, model.transform.localScale.y, 0.01f);
        Assert.AreEqual(1f, model.transform.localScale.z, 0.01f);
    }

    // Un nivel mas alto crece el modelo sin deformarlo.
    [Test]
    public void NivelSuperior_CreceElModeloProporcionalmente()
    {
        _object = new GameObject("Tower Test");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(_object.transform, false);
        Tower tower = _object.AddComponent<Tower>();
        tower.Visual.baseScale = 1f;

        tower.SetLevel(1);
        float sizeNivel1 = model.transform.localScale.x;

        tower.SetLevel(2);

        float sizeNivel2 = model.transform.localScale.x;
        Assert.Greater(sizeNivel2, sizeNivel1);
        Assert.AreEqual(sizeNivel2 / sizeNivel1, 1f + 0.25f, 0.01f);
    }

    // Simula una recarga de escena: el setup se repite pero la escala base
    // guardada evita que se acumule (bug: las torres encogian cada Play Mode).
    [Test]
    public void AlReiniciarSetup_NoAcumulaLaEscala()
    {
        _object = new GameObject("Tower Test");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(_object.transform, false);
        Tower tower = _object.AddComponent<Tower>(); // baseScale por defecto 0.75

        tower.SetLevel(1);
        float primera = model.transform.localScale.x;

        // Simula la recarga de la escena: el setup vuelve a ejecutarse.
        SetPrivateField(tower.Visual, "_setupDone", false);
        tower.SetLevel(1);

        Assert.AreEqual(primera, model.transform.localScale.x, 0.0001f);
    }

    // Cambiar de nivel y volver restaura el tamano base sin corromperlo.
    [Test]
    public void CambiarNivelYVolver_RestauraLaEscalaBase()
    {
        _object = new GameObject("Tower Test");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(_object.transform, false);
        Tower tower = _object.AddComponent<Tower>();
        tower.Visual.baseScale = 1f;

        tower.SetLevel(1);
        float nivel1 = model.transform.localScale.x;

        tower.SetLevel(2);
        tower.SetLevel(1);

        Assert.AreEqual(nivel1, model.transform.localScale.x, 0.0001f);
    }

    // El modelo del nivel reemplaza al modelo anterior de la torre.
    [Test]
    public void SetModel_CambiaElModeloDeLaTorre()
    {
        _object = new GameObject("Tower Test");
        Tower tower = _object.AddComponent<Tower>();
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        tower.SetModel(prefab);

        // Sigue habiendo un solo modelo y es una copia del prefab pedido.
        Assert.AreEqual(1, _object.transform.childCount);
        Assert.IsNotNull(_object.transform.GetChild(0).GetComponent<MeshFilter>());
        Assert.AreEqual(prefab.GetComponent<MeshFilter>().sharedMesh,
            _object.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh);

        Object.DestroyImmediate(prefab);
    }

    // Pedir el mismo modelo dos veces no vuelve a crearlo.
    [Test]
    public void SetModel_ConElMismoModelo_NoLoCreaDeNuevo()
    {
        _object = new GameObject("Tower Test");
        Tower tower = _object.AddComponent<Tower>();
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        tower.SetModel(prefab);
        Transform primero = _object.transform.GetChild(0);
        tower.SetModel(prefab);

        Assert.AreEqual(1, _object.transform.childCount);
        Assert.AreEqual(primero.GetInstanceID(), _object.transform.GetChild(0).GetInstanceID());

        Object.DestroyImmediate(prefab);
    }

    // Asigna un campo privado desde la prueba (los campos serializados son privados).
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
