#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DroneViewingSceneInstaller
{
    private const string EWorkScenePath =
        "Assets/Scenes/GameFlow/GameplayEffect_kuba.unity";
    private const string ESystemObjectName = "DroneViewingSystem";

    [MenuItem("Muscle Beat/Setup Drone Viewing In Work Scene")]
    public static void SetupWorkScene()
    {
        Scene scene = EditorSceneManager.OpenScene(
            EWorkScenePath,
            OpenSceneMode.Single);
        DroneViewingSystem existingSystem =
            Object.FindFirstObjectByType<DroneViewingSystem>(
                FindObjectsInactive.Include);
        if (existingSystem == null)
        {
            GameObject systemObject = new GameObject(ESystemObjectName);
            existingSystem = systemObject.AddComponent<DroneViewingSystem>();
        }

        CreateSeparateDroneObjects(existingSystem.transform);

        EditorUtility.SetDirty(existingSystem);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int monitorMaterialSlotCount = CountMonitorMaterialSlots("moniter");
        Debug.Log(
            $"GameplayEffect_kubaへDrone Viewing Systemを設定しました。"
            + $" Monitor Material枠: {monitorMaterialSlotCount}");
    }

    private static void CreateSeparateDroneObjects(Transform _systemTransform)
    {
        Vector3[][] routes =
        {
            new[]
            {
                new Vector3(-14.0f, 8.0f, -12.0f),
                new Vector3(-12.0f, 10.0f, 12.0f),
                new Vector3(10.0f, 8.0f, 14.0f),
                new Vector3(13.0f, 9.0f, -10.0f)
            },
            new[]
            {
                new Vector3(-9.0f, 11.0f, -15.0f),
                new Vector3(-15.0f, 7.0f, 7.0f),
                new Vector3(8.0f, 11.0f, 15.0f),
                new Vector3(15.0f, 7.5f, -6.0f)
            },
            new[]
            {
                new Vector3(-16.0f, 6.5f, -5.0f),
                new Vector3(-6.0f, 12.0f, 15.0f),
                new Vector3(15.0f, 6.5f, 5.0f),
                new Vector3(5.0f, 12.0f, -15.0f)
            }
        };
        float[] speeds = { 4.5f, 5.0f, 5.5f };
        for (int i = 0; i < routes.Length; ++i)
        {
            string droneName = $"VenueViewingDrone_{i + 1}";
            Transform droneTransform = _systemTransform.Find(droneName);
            if (droneTransform != null
                && droneTransform.GetComponent<DroneSplineRoute>() != null)continue;

            GameObject droneObject = droneTransform != null
                ? droneTransform.gameObject
                : new GameObject(droneName);
            droneObject.transform.SetParent(_systemTransform, false);
            DroneSplineRoute route =
                droneObject.GetComponent<DroneSplineRoute>();
            if (route == null)route = droneObject.AddComponent<DroneSplineRoute>();
            route.Configure(routes[i], speeds[i], true);
            EditorUtility.SetDirty(route);
        }
    }

    private static int CountMonitorMaterialSlots(string _keyword)
    {
        int slotCount = 0;
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; ++i)
        {
            Material[] materials = renderers[i].sharedMaterials;
            for (int j = 0; j < materials.Length; ++j)
            {
                Material material = materials[j];
                if (material != null
                    && material.name.ToLowerInvariant().Contains(_keyword))
                {
                    ++slotCount;
                }
            }
        }
        return slotCount;
    }
}
#endif
