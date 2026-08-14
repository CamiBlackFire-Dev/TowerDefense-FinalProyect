using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inspector sencillo para editar el nivel de cada casilla del tablero.
// Este script vive en la carpeta Editor, asi que solo existe en el editor
// y los scripts del juego no necesitan preguntar si estan en el.
[CustomEditor(typeof(BoardManager))]
public class BoardManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoardManager board = (BoardManager)target;

        // Se dibujan todos los campos publicos del componente.
        // Si se cambia alguno (modelo, material, tamano...) se rehace la vista.
        if (DrawDefaultInspector())
            Rebuild(board, "Cambiar ajustes del tablero");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tablero " + board.GridWidth + "x" + board.GridHeight, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("0 deja la casilla vacia. Los valores mayores representan el nivel de la torre.", MessageType.Info);

        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        for (int y = 0; y < board.GridHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < board.GridWidth; x++)
            {
                int currentLevel = board.GetEditorLevel(x, y);
                int newLevel = EditorGUILayout.IntField(currentLevel, GUILayout.Width(42));
                if (newLevel != currentLevel)
                {
                    Undo.RecordObject(board, "Cambiar nivel de casilla");
                    board.SetEditorLevel(x, y, newLevel);
                    MarkDirty(board);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        if (GUILayout.Button("Reconstruir vista del tablero"))
            Rebuild(board, "Reconstruir tablero");
    }

    // Rehace las casillas y las torres, y marca la escena para guardarla.
    private void Rebuild(BoardManager board, string undoName)
    {
        Undo.RecordObject(board, undoName);
        board.RebuildBoardView();
        MarkDirty(board);
    }

    // Avisa a Unity de que hay cambios sin guardar.
    private void MarkDirty(BoardManager board)
    {
        EditorUtility.SetDirty(board);
        if (board.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
    }
}
