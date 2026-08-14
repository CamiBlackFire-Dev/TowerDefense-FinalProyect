using System.Collections;
using UnityEngine;

// Efecto visual de una explosion: un disco que crece rapido hasta cubrir
// exactamente el radio indicado (para que se vea el area real que afecta)
// y se desvanece. No sabe nada de dano ni de enemigos, solo dibuja encima
// de donde exploto la bomba.
public class ExplosionVisual : MonoBehaviour
{
    // Crea el efecto en el lugar y con el radio pedidos, y se destruye solo.
    public static void Spawn(Vector3 position, float radius, Material material, float duration = 0.4f)
    {
        if (radius <= 0f)
            return;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ExplosionVisual";
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.zero;

        Collider blastCollider = go.GetComponent<Collider>();
        if (blastCollider != null)
            Destroy(blastCollider);

        Renderer visualRenderer = go.GetComponent<Renderer>();
        if (material != null)
            visualRenderer.sharedMaterial = material;

        ExplosionVisual effect = go.AddComponent<ExplosionVisual>();
        effect.StartCoroutine(effect.PlayAndDestroy(visualRenderer, radius, duration));
    }

    private IEnumerator PlayAndDestroy(Renderer visualRenderer, float radius, float duration)
    {
        Color baseColor = visualRenderer.sharedMaterial != null ? visualRenderer.sharedMaterial.color : Color.white;
        float diameter = radius * 2f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            // Crece rapido y frena (ease-out), como una onda expansiva.
            float scaleProgress = 1f - (1f - progress) * (1f - progress);
            float scale = diameter * scaleProgress;
            transform.localScale = new Vector3(scale, scale, scale);

            Color c = baseColor;
            c.a = baseColor.a * (1f - progress);
            visualRenderer.material.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
