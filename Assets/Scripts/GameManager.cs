using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int maxEnemies = 8;
    public float spawnInterval = 5f;
    public float minSpawnDistance = 8f;

    [Header("Map Bounds")]
    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(96f, 20f, 96f));

    [HideInInspector] public int score = 0;
    [HideInInspector] public bool gameOver = false;
    [HideInInspector] public bool gameStarted = true;

    private float spawnTimer;
    private System.Collections.Generic.List<EnemyController> enemies = new();
    private Vector3 playerSpawnPosition;
    private bool spawnCaptured = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Debug.Log("BUILD_VERSION: 20260619_1900");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var startPlayer = FindObjectOfType<PlayerController>();
        if (startPlayer != null)
        {
            playerSpawnPosition = startPlayer.transform.position;
            spawnCaptured = true;
        }

        FixGroundMaterial();

        for (int i = 0; i < 3; i++) SpawnEnemy();
    }

    void FixGroundMaterial()
    {
        var map = GameObject.Find("Map");
        if (map == null) return;
        var allRenderers = map.GetComponentsInChildren<Renderer>();
        foreach (var r in allRenderers)
        {
            if (r.gameObject.name == "Object_9" && r.sharedMaterial != null)
            {
                var mat = r.material;
                mat.SetFloat("roughnessFactor", 0.85f);
                mat.SetFloat("metallicFactor", 0f);
                mat.SetFloat("normalTexture_scale", 0f);
                break;
            }
        }
    }

    void Update()
    {
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer > spawnInterval && enemies.Count < maxEnemies)
        {
            spawnTimer = 0;
            SpawnEnemy();
        }
    }

    public void RegisterEnemy(EnemyController enemy)
    {
        enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        enemies.Remove(enemy);
        score++;
        UIManager.Instance?.UpdateScore(score);

        var playerShoot = FindObjectOfType<WeaponManager>();
        if (playerShoot != null) playerShoot.AddReserveAmmo(10);

        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 pos = GetSpawnPosition();
        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        var enemy = go.GetComponent<EnemyController>();
        if (enemy != null) enemy.Init(pos);
    }

    private Vector3 GetSpawnPosition()
    {
        var player = Camera.main?.transform;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(mapBounds.min.x + 5f, mapBounds.max.x - 5f);
            float z = Random.Range(mapBounds.min.z + 5f, mapBounds.max.z - 5f);
            var pos = new Vector3(x, 0, z);

            if (player != null)
            {
                float dist = Vector3.Distance(pos, player.position);
                if (dist < minSpawnDistance)
                {
                    var angle = Random.Range(0f, Mathf.PI * 2f);
                    pos = player.position + new Vector3(Mathf.Cos(angle) * 12f, 0, Mathf.Sin(angle) * 12f);
                }
            }

            pos.x = Mathf.Clamp(pos.x, mapBounds.min.x + 2f, mapBounds.max.x - 2f);
            pos.z = Mathf.Clamp(pos.z, mapBounds.min.z + 2f, mapBounds.max.z - 2f);

            if (Physics.CheckSphere(pos + Vector3.up, 0.5f, LayerMask.GetMask("Default")))
                continue;

            return pos;
        }
        return Vector3.zero;
    }

    public void EndGame()
    {
        gameOver = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIManager.Instance?.ShowGameOver(score);
    }

    public void Restart()
    {
        score = 0;
        gameOver = false;

        foreach (var e in enemies.ToArray())
            Destroy(e.gameObject);
        enemies.Clear();

        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null) playerHealth.ResetHealth();

        var weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null) weaponManager.ResetAll();

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Vector3 respawnPos = spawnCaptured ? playerSpawnPosition : player.transform.position;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = respawnPos;
                cc.enabled = true;
            }
            else
            {
                player.transform.position = respawnPos;
            }
            player.ResetState();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UIManager.Instance?.HideGameOver();
        UIManager.Instance?.UpdateScore(0);

        for (int i = 0; i < 3; i++) SpawnEnemy();
    }
}
