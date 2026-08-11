using System.Collections.Generic;
using UnityEngine;

// Arma el camino de los enemigos: una "C" cuadrada que rodea al tablero.
// Crea las losetas que se ven y los waypoints que sigue TestEnemyMovement.
// El recorrido empieza arriba a la derecha, va hacia la izquierda, baja por
// el lado izquierdo y sale hacia la derecha por abajo.
// ExecuteAlways permite verlo y ajustarlo sin entrar en Play Mode.
[ExecuteAlways]
public class PathBuilder : MonoBehaviour
{
    [Header("Medidas")]
    public int boardCells = 4;     // casillas del tablero al que rodea
    public float tileSize = 2f;    // ancho de cada loseta (igual que Cell Size del tablero)
    public float surfaceY = 0.6f;  // altura de la superficie del camino
    // Mitad de la altura del enemigo: sube los waypoints para que no se
    // hunda en el camino. La capsula de prueba mide 2, asi que va 1.
    public float enemyHalfHeight = 1f;

    [Header("Losetas")]
    public Mesh spawnTile;         // tile-spawn-end: por donde salen los enemigos
    public Mesh straightTile;      // tile-straight: los tramos rectos
    public Mesh cornerTile;        // tile-corner-large: las dos esquinas
    public Mesh endTile;           // tile-end: donde termina el camino
    public Mesh bridgeTile;        // tile-river-bridge: se usa una sola vez
    public int bridgeStep = 2;     // en que tramo recto va el puente (0 = sin puente)
    public Material tileMaterial;  // material de las losetas (colormap)

    [Header("Giro de los modelos")]
    // Si un modelo sale mirando para otro lado se corrige aqui.
    // Normalmente basta con 0, 90, 180 o 270.
    public float spawnYaw = 0f;
    public float straightYaw = 0f;
    public float cornerYaw = 0f;
    public float endYaw = 0f;

    private Transform _tileContainer;
    private Transform _waypointContainer;
    private Path _path;

    // Camino ya armado, listo para que lo usen los enemigos.
    public Path Path
    {
        get
        {
            EnsurePath();
            return _path;
        }
    }

    private void OnEnable()
    {
        EnsureContainers();

        // Si el camino ya esta en la escena solo se vuelve a conectar.
        if (_tileContainer.childCount == 0 || _waypointContainer.childCount == 0)
            Rebuild();
        else
            LinkWaypoints();
    }

    // Borra el camino anterior y lo vuelve a construir.
    public void Rebuild()
    {
        EnsureContainers();
        ClearContainer(_tileContainer);
        ClearContainer(_waypointContainer);

        List<Vector3> points = BuildPathPoints();
        CreateTiles(points);
        CreateWaypoints(points);
        LinkWaypoints();
    }

    // Centros de todas las losetas, en orden de recorrido.
    // Es solo matematica: se puede probar sin abrir el juego.
    public List<Vector3> BuildPathPoints()
    {
        List<Vector3> points = new List<Vector3>();

        // El carril queda justo por fuera del tablero.
        float ring = boardCells * tileSize * 0.5f + tileSize * 0.5f;
        int steps = boardCells + 1;

        // Lado de arriba: de derecha a izquierda.
        for (int i = 0; i <= steps; i++)
            points.Add(new Vector3(ring - i * tileSize, 0f, ring));

        // Lado izquierdo: de arriba hacia abajo.
        for (int i = 1; i <= steps; i++)
            points.Add(new Vector3(-ring, 0f, ring - i * tileSize));

        // Lado de abajo: de izquierda a derecha.
        for (int i = 1; i <= steps; i++)
            points.Add(new Vector3(-ring + i * tileSize, 0f, -ring));

        return points;
    }

    // Indica si en ese punto el camino da la vuelta.
    public bool IsCorner(List<Vector3> points, int index)
    {
        if (index <= 0 || index >= points.Count - 1)
            return false;

        Vector3 entra = (points[index] - points[index - 1]).normalized;
        Vector3 sale = (points[index + 1] - points[index]).normalized;
        return Vector3.Angle(entra, sale) > 1f;
    }

    private void CreateTiles(List<Vector3> points)
    {
        int straightCount = 0;

        for (int i = 0; i < points.Count; i++)
        {
            Mesh mesh;
            float yaw;

            if (i == 0)
            {
                mesh = spawnTile;
                yaw = spawnYaw;
            }
            else if (i == points.Count - 1)
            {
                mesh = endTile;
                yaw = endYaw;
            }
            else if (IsCorner(points, i))
            {
                mesh = cornerTile;
                yaw = cornerYaw;
            }
            else
            {
                straightCount++;
                // El puente aparece una sola vez, en el tramo recto elegido.
                bool esPuente = bridgeTile != null && straightCount == bridgeStep;
                mesh = esPuente ? bridgeTile : straightTile;
                yaw = straightYaw;
            }

            CreateTile(i, mesh, points[i], DirectionAt(points, i), yaw);
        }
    }

    // Hacia donde mira la loseta: la direccion por la que entra el camino.
    private Vector3 DirectionAt(List<Vector3> points, int index)
    {
        if (index == 0)
            return (points[1] - points[0]).normalized;

        return (points[index] - points[index - 1]).normalized;
    }

    // Crea una loseta escalada al tamano del camino y apoyada en su superficie.
    private void CreateTile(int index, Mesh mesh, Vector3 position, Vector3 forward, float yaw)
    {
        GameObject tile = new GameObject("Tile " + index.ToString("00"));
        tile.transform.SetParent(_tileContainer, false);
        tile.transform.localRotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, yaw, 0f);

        // Sin modelo asignado la loseta queda vacia, pero el camino igual funciona.
        if (mesh == null)
        {
            Vector3 vacia = position;
            vacia.y = surfaceY;
            tile.transform.localPosition = vacia;
            return;
        }

        MeshFilter filter = tile.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = tile.AddComponent<MeshRenderer>();
        if (tileMaterial != null)
            renderer.sharedMaterial = tileMaterial;

        // La loseta se escala parejo para ocupar tileSize sin deformarse.
        Bounds bounds = mesh.bounds;
        float ancho = Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = ancho > 0f ? tileSize / ancho : 1f;
        tile.transform.localScale = Vector3.one * scale;

        // La cara de arriba del modelo queda a la altura del camino.
        Vector3 place = position;
        place.y = surfaceY - bounds.max.y * scale;
        tile.transform.localPosition = place;

        // Collider en el piso para que el dron pueda apuntar sobre el camino.
        BoxCollider collider = tile.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;

        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0)
            tile.layer = floorLayer;
    }

    private void CreateWaypoints(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            GameObject waypoint = new GameObject("Waypoint " + i.ToString("00"));
            waypoint.transform.SetParent(_waypointContainer, false);

            Vector3 place = points[i];
            place.y = surfaceY + enemyHalfHeight;
            waypoint.transform.localPosition = place;
        }
    }

    // Le pasa los waypoints al componente Path que leen los enemigos.
    private void LinkWaypoints()
    {
        EnsurePath();

        Transform[] waypoints = new Transform[_waypointContainer.childCount];
        for (int i = 0; i < waypoints.Length; i++)
            waypoints[i] = _waypointContainer.GetChild(i);

        _path.SetWaypoints(waypoints);
    }

    private void EnsurePath()
    {
        if (_path != null)
            return;

        _path = GetComponent<Path>();
        if (_path == null)
            _path = gameObject.AddComponent<Path>();
    }

    private void EnsureContainers()
    {
        _tileContainer = transform.Find("Tiles");
        if (_tileContainer == null)
        {
            GameObject tiles = new GameObject("Tiles");
            tiles.transform.SetParent(transform, false);
            _tileContainer = tiles.transform;
        }

        _waypointContainer = transform.Find("Waypoints");
        if (_waypointContainer == null)
        {
            GameObject waypoints = new GameObject("Waypoints");
            waypoints.transform.SetParent(transform, false);
            _waypointContainer = waypoints.transform;
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
