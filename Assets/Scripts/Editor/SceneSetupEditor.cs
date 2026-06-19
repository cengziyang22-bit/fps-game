using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine.AI;

public class SceneSetupEditor : EditorWindow
{
    [MenuItem("FPS/Setup Scene")]
    public static void SetupScene()
    {
        // --- Ground ---
        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.layer = LayerMask.NameToLayer("Default");
        }

        // --- Map Model ---
        var mapObj = GameObject.Find("Map");
        if (mapObj == null)
        {
            var mapAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/fps_map.glb");
            if (mapAsset != null)
            {
                mapObj = (GameObject)PrefabUtility.InstantiatePrefab(mapAsset);
                mapObj.name = "Map";

                // Mark children as static for NavMesh baking
                foreach (Transform t in mapObj.GetComponentsInChildren<Transform>())
                    t.gameObject.isStatic = true;
            }
        }

        // --- Player ---
        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.skinWidth = 0.08f;

            player.AddComponent<PlayerController>();
            player.AddComponent<WeaponManager>();
            player.AddComponent<PlayerHealth>();

            // Camera
            var camObj = new GameObject("MainCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0, 0.9f, 0);
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            camObj.AddComponent<AudioListener>();

            var shoot = camObj.AddComponent<PlayerShoot>();
            shoot.playerCamera = cam;

            // Muzzle point
            var muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(camObj.transform);
            muzzle.transform.localPosition = new Vector3(0.2f, -0.15f, 0.3f);
            shoot.muzzlePoint = muzzle.transform;
        }

        // --- GameManager ---
        var gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        // --- UI ---
        var uiObj = GameObject.Find("UI");
        if (uiObj == null)
        {
            uiObj = new GameObject("UI");
            uiObj.AddComponent<UIManager>();
        }

        // --- Enemy Prefab (skeleton) ---
        var enemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
        if (enemyPrefab == null)
        {
            var enemy = new GameObject("Enemy");
            enemy.AddComponent<NavMeshAgent>();
            enemy.AddComponent<EnemyController>();
            enemy.AddComponent<CapsuleCollider>().height = 1.8f;

            // Simple body parts
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.35f);
            body.AddComponent<BodyPart>().damageMultiplier = 1.5f;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform);
            head.transform.localPosition = new Vector3(0, 1.55f, 0);
            head.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            head.AddComponent<BodyPart>().damageMultiplier = 3f;

            enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemy, enemyPrefabPath);
            DestroyImmediate(enemy);
        }

        // Link enemy prefab to GameManager
        var gm = gmObj.GetComponent<GameManager>();
        gm.enemyPrefab = enemyPrefab;

        Debug.Log("Scene setup complete. Baking NavMesh...");
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("NavMesh baked. Press Play to test.");
    }
}
