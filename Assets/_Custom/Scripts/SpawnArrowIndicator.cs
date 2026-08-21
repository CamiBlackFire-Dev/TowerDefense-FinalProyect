using System.Collections.Generic;
using UnityEngine;

// Flechas rojas flotando sobre cada salida de enemigos, para que al entrar
// al nivel se vea de donde van a venir. Se muestran solo antes de la
// primera oleada: en cuanto arranca desaparecen y no vuelven en toda la
// partida (es una ayuda de presentacion, no un indicador permanente).
//
// La flecha se dibuja tumbada sobre el suelo apuntando hacia donde va a
// caminar el enemigo, que desde la camara cenital del juego se lee mejor
// que una flecha vertical.
public class SpawnArrowIndicator : MonoBehaviour
{
    [Header("Referencias")]
    // Se busca solo en la escena si se deja vacio.
    public EnemySpawner spawner;
    // Material de la flecha. Sin uno asignado se ve blanca.
    public Material arrowMaterial;

    [Header("Aspecto")]
    public Color arrowColor = new Color(1f, 0.15f, 0.1f, 0.9f);
    public float arrowLength = 2.6f;
    public float arrowWidth = 1.4f;
    // Altura sobre la loseta de salida.
    public float height = 1.6f;

    [Header("Movimiento")]
    // Sube y baja para que llame la atencion. En 0 se queda quieta.
    public float bobAmplitude = 0.25f;
    public float bobSpeed = 2f;

    private readonly List<Transform> _arrows = new List<Transform>();
    private bool _hidden;

    private void OnEnable()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();

        if (spawner != null)
            spawner.WaveStarted += HandleWaveStarted;
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.WaveStarted -= HandleWaveStarted;
    }

    private void Start()
    {
        if (!Application.isPlaying || spawner == null)
            return;

        // Si la partida ya empezo (por ejemplo al recargar en mitad de un
        // nivel) no tiene sentido mostrarlas.
        if (spawner.WaveNumber > 0)
        {
            _hidden = true;
            return;
        }

        BuildArrows();
    }

    private void Update()
    {
        if (_hidden || bobAmplitude <= 0f)
            return;

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        for (int i = 0; i < _arrows.Count; i++)
        {
            if (_arrows[i] == null)
                continue;

            Vector3 p = _arrows[i].localPosition;
            p.y = height + offset;
            _arrows[i].localPosition = p;
        }
    }

    private void HandleWaveStarted()
    {
        if (_hidden)
            return;

        _hidden = true;
        for (int i = 0; i < _arrows.Count; i++)
        {
            if (_arrows[i] != null)
                _arrows[i].gameObject.SetActive(false);
        }

        // Ya cumplio: no hace falta seguir escuchando ni animando.
        if (spawner != null)
            spawner.WaveStarted -= HandleWaveStarted;
    }

    // Una flecha por salida, mirando hacia el siguiente waypoint (o sea,
    // hacia donde va a arrancar a caminar el enemigo).
    private void BuildArrows()
    {
        Mesh mesh = BuildArrowMesh();

        foreach (SpawnPointInfo info in CollectSpawnPoints())
        {
            GameObject arrow = new GameObject("SpawnArrow");
            arrow.transform.SetParent(transform, false);
            arrow.transform.position = info.Position + Vector3.up * height;

            if (info.Direction.sqrMagnitude > 0.0001f)
                arrow.transform.rotation = Quaternion.LookRotation(info.Direction, Vector3.up);

            MeshFilter filter = arrow.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = arrow.AddComponent<MeshRenderer>();
            if (arrowMaterial != null)
                renderer.sharedMaterial = arrowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", arrowColor);
            block.SetColor("_Color", arrowColor);
            renderer.SetPropertyBlock(block);

            _arrows.Add(arrow.transform);
        }
    }

    private struct SpawnPointInfo
    {
        public Vector3 Position;
        public Vector3 Direction;
    }

    // De donde salen los enemigos: cada salida configurada, o la unica de
    // siempre si el nivel no usa salidas multiples.
    private List<SpawnPointInfo> CollectSpawnPoints()
    {
        var points = new List<SpawnPointInfo>();

        if (spawner.HasSpawnSources())
        {
            foreach (SpawnSource source in spawner.spawnSources)
            {
                if (source == null || !source.active)
                    continue;

                PathBuilder builder = source.pathBuilder != null ? source.pathBuilder : spawner.pathBuilder;
                AddPoint(points, builder, source.startWaypointIndex);
            }
        }
        else
        {
            AddPoint(points, spawner.pathBuilder, spawner.spawnWaypointIndex);
        }

        return points;
    }

    private void AddPoint(List<SpawnPointInfo> points, PathBuilder builder, int index)
    {
        if (builder == null || builder.Path == null)
            return;

        Transform[] waypoints = builder.Path.GetWaypoints();
        if (waypoints == null || waypoints.Length == 0)
            return;

        int start = Mathf.Clamp(index, 0, waypoints.Length - 1);
        if (waypoints[start] == null)
            return;

        Vector3 position = waypoints[start].position;
        Vector3 direction = Vector3.forward;

        // Apunta al siguiente waypoint; si la salida es el ultimo, al anterior.
        if (start + 1 < waypoints.Length && waypoints[start + 1] != null)
            direction = waypoints[start + 1].position - position;
        else if (start > 0 && waypoints[start - 1] != null)
            direction = position - waypoints[start - 1].position;

        direction.y = 0f;
        points.Add(new SpawnPointInfo { Position = position, Direction = direction });
    }

    // Flecha plana tumbada en el plano XZ, apuntando a +Z: un mastil
    // rectangular y una punta triangular.
    private Mesh BuildArrowMesh()
    {
        float halfWidth = arrowWidth * 0.5f;
        float shaftHalf = halfWidth * 0.35f;
        float headStart = arrowLength * 0.35f;

        Vector3[] vertices =
        {
            // mastil
            new Vector3(-shaftHalf, 0f, -arrowLength * 0.5f),
            new Vector3( shaftHalf, 0f, -arrowLength * 0.5f),
            new Vector3( shaftHalf, 0f, headStart),
            new Vector3(-shaftHalf, 0f, headStart),
            // punta
            new Vector3(-halfWidth, 0f, headStart),
            new Vector3( halfWidth, 0f, headStart),
            new Vector3(0f, 0f, arrowLength * 0.5f),
        };

        int[] triangles = { 0, 3, 2, 0, 2, 1, 4, 6, 5 };

        Mesh mesh = new Mesh();
        mesh.name = "SpawnArrow";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
