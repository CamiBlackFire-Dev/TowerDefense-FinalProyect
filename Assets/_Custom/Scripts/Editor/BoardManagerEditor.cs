using UnityEditor;
using UnityEngine;

namespace TowerDefense
{
    // Inspector sencillo para editar el nivel de cada casilla del tablero.
    [CustomEditor(typeof(BoardManager))]
    public class BoardManagerEditor : Editor
    {
        private SerializedProperty _cellSize;
        private SerializedProperty _cellThickness;
        private SerializedProperty _cellMaterial;
        private SerializedProperty _towerPrefab;
        private SerializedProperty _inputController;

        private void OnEnable()
        {
            _cellSize = serializedObject.FindProperty("cellSize");
            _cellThickness = serializedObject.FindProperty("cellThickness");
            _cellMaterial = serializedObject.FindProperty("cellMaterial");
            _towerPrefab = serializedObject.FindProperty("towerPrefab");
            _inputController = serializedObject.FindProperty("inputController");
        }

        public override void OnInspectorGUI()
        {
            BoardManager board = (BoardManager)target;
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_cellSize);
            EditorGUILayout.PropertyField(_cellThickness);
            EditorGUILayout.PropertyField(_cellMaterial);
            EditorGUILayout.PropertyField(_towerPrefab);
            EditorGUILayout.PropertyField(_inputController);
            bool visualSettingsChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (visualSettingsChanged)
                board.RebuildBoardView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tablero 4x4", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("0 deja la casilla vacia. Los valores mayores representan el nivel de la torre.", MessageType.Info);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            for (int y = 0; y < board.GridSize; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < board.GridSize; x++)
                {
                    int currentLevel = board.GetEditorLevel(x, y);
                    int newLevel = EditorGUILayout.IntField(currentLevel, GUILayout.Width(42));
                    if (newLevel != currentLevel)
                    {
                        Undo.RecordObject(board, "Cambiar nivel de casilla");
                        board.SetEditorLevel(x, y, newLevel);
                        EditorUtility.SetDirty(board);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Reconstruir vista del tablero"))
            {
                Undo.RecordObject(board, "Reconstruir tablero");
                board.RebuildBoardView();
                EditorUtility.SetDirty(board);
            }
        }
    }
}
