using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Arma el Animator de los enemigos con las animaciones de los rigs que
// estan en la carpeta de modelos (Rig_Medium_MovementBasic y Rig_Medium_General).
// Solo tiene dos estados, que es lo que necesita el juego hoy:
//   Walk  -> caminando, se repite sin parar (estado inicial)
//   Death -> se activa con el trigger "Die" cuando una torre lo mata
// Se usa desde el menu Tower Defense o desde EnemyPrefabBuilder.
public static class EnemyAnimatorBuilder
{
    private const string ModelsFolder = "Assets/_Custom/Models/Enemies";
    private const string AnimationsFolder = "Assets/_Custom/Animations";
    private const string ControllerPath = AnimationsFolder + "/EnemyAnimator.controller";

    // Animaciones elegidas. Se pueden cambiar por otras del mismo rig:
    // Walking_B, Walking_C, Running_A, Running_B, Death_B...
    private const string WalkClipName = "Walking_A";
    private const string DeathClipName = "Death_A";

    // Tiene que ser el mismo texto que el campo Die Trigger de EnemyHealth.
    private const string DieTrigger = "Die";

    // Los Rig_* no traen malla, asi que Unity no les puede armar un avatar
    // humanoide solo. Se les copia el avatar de este personaje para que las
    // animaciones se importen bien calibradas (sin esto, el rig queda sin
    // avatar y sus animaciones no sirven para retargeting humanoide).
    private const string ReferenceAvatarModel = "Skeleton_Warrior";

    [MenuItem("Tower Defense/Crear animator de enemigos")]
    public static void CreateFromMenu()
    {
        AnimatorController controller = EnsureController();
        if (controller != null)
            Debug.Log("Animator de enemigos listo: " + ControllerPath, controller);
    }

    // Crea el controlador si falta y le vuelve a armar los estados.
    // Reusa el asset existente para no romper los prefabs que ya lo usan.
    public static AnimatorController EnsureController()
    {
        ShareAvatarAcrossRigs();

        AnimationClip walk = FindClip(WalkClipName, "walk");
        if (walk == null)
        {
            Debug.LogWarning("No se encontro ninguna animacion de caminar en " + ModelsFolder);
            return null;
        }

        // Caminar tiene que repetirse; el fbx viene sin loop por defecto.
        string walkPath = AssetDatabase.GetAssetPath(walk);
        string walkName = walk.name;
        SetClipLooping(walkPath, walkName);
        walk = LoadClip(walkPath, walkName);

        AnimationClip death = FindClip(DeathClipName, "death");

        EnsureFolder();

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        ClearMachine(controller, machine);

        AnimatorState walkState = machine.AddState("Walk");
        walkState.motion = walk;
        machine.defaultState = walkState;

        if (death != null)
        {
            controller.AddParameter(DieTrigger, AnimatorControllerParameterType.Trigger);

            AnimatorState deathState = machine.AddState("Death");
            deathState.motion = death;

            // Desde cualquier estado se puede morir.
            AnimatorStateTransition transition = machine.AddAnyStateTransition(deathState);
            transition.AddCondition(AnimatorConditionMode.If, 0f, DieTrigger);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.canTransitionToSelf = false;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    // Hace que todos los Rig_* usen el mismo avatar humanoide que un
    // personaje real (Skeleton_Warrior), en vez de quedar sin avatar.
    // Si ya lo copiaste a mano desde el Inspector no se toca de nuevo.
    private static void ShareAvatarAcrossRigs()
    {
        string referencePath = ModelsFolder + "/" + ReferenceAvatarModel + ".fbx";
        Avatar reference = AssetDatabase.LoadAssetAtPath<Avatar>(referencePath);
        if (reference == null)
        {
            Debug.LogWarning("No se encontro el avatar de referencia (" + ReferenceAvatarModel +
                ") para repartirlo entre los rigs de animacion.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { ModelsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("Rig_"))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            bool yaListo = importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther &&
                importer.sourceAvatar == reference;
            if (yaListo)
                continue;

            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = reference;
            importer.SaveAndReimport();

            Debug.Log(fileName + ": avatar copiado de " + ReferenceAvatarModel + ".");
        }
    }

    // Deja el controlador vacio para armarlo siempre igual.
    private static void ClearMachine(AnimatorController controller, AnimatorStateMachine machine)
    {
        machine.anyStateTransitions = new AnimatorStateTransition[0];

        ChildAnimatorState[] states = machine.states;
        for (int i = 0; i < states.Length; i++)
            machine.RemoveState(states[i].state);

        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = parameters.Length - 1; i >= 0; i--)
            controller.RemoveParameter(i);
    }

    // Busca una animacion por nombre exacto; si no aparece, por palabra clave.
    private static AnimationClip FindClip(string exactName, string keyword)
    {
        List<AnimationClip> clips = LoadAllClips();

        foreach (AnimationClip clip in clips)
            if (clip.name == exactName)
                return clip;

        foreach (AnimationClip clip in clips)
            if (clip.name.ToLower().Contains(keyword))
                return clip;

        return null;
    }

    // Todas las animaciones que traen los modelos de la carpeta de enemigos.
    private static List<AnimationClip> LoadAllClips()
    {
        List<AnimationClip> clips = new List<AnimationClip>();
        string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { ModelsFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__"))
                    clips.Add(clip);
            }
        }

        return clips;
    }

    private static AnimationClip LoadClip(string path, string clipName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip != null && clip.name == clipName)
                return clip;
        }

        return null;
    }

    // Marca una animacion del fbx como repetible y reimporta el modelo.
    private static void SetClipLooping(string modelPath, string clipName)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        bool cambio = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name != clipName || clips[i].loopTime)
                continue;

            clips[i].loopTime = true;
            cambio = true;
        }

        if (!cambio)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AnimationsFolder))
            AssetDatabase.CreateFolder("Assets/_Custom", "Animations");
    }
}
