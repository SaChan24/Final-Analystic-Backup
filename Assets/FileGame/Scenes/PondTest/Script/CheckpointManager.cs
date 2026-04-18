using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Debug (Read Only)")]
    [SerializeField] private Vector3 lastCheckpointPosition;
    [SerializeField] private Quaternion lastCheckpointRotation;
    [SerializeField] private string lastSceneName;

    private bool hasCheckpoint = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// เรียกตอนผู้เล่นเหยียบ checkpoint
    /// </summary>
    public void SetCheckpoint(PlayerController3D player)
    {
        if (player == null) return;

        Transform t = player.transform;

        lastCheckpointPosition = t.position;
        lastCheckpointRotation = t.rotation;
        lastSceneName = SceneManager.GetActiveScene().name;
        hasCheckpoint = true;

        // เซฟ sanity + inventory ผ่าน player (static snapshot)
        player.SaveCheckpointState();

        Debug.Log($"[CheckpointManager] Saved checkpoint at {lastCheckpointPosition} in scene {lastSceneName}");
    }

    /// <summary>
    /// รีโหลดซีนแล้ว Respawn ผู้เล่นกลับ checkpoint
    /// </summary>
    public void RespawnPlayer(PlayerController3D _)
    {
        if (!hasCheckpoint)
        {
            Debug.LogWarning("[CheckpointManager] No checkpoint yet. Respawn ignored.");
            return;
        }

        // สมัคร event ไว้ก่อนโหลดซีน
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // โหลดซีนที่เซฟไว้ (จะรีทุกอย่างในฉาก)
        SceneManager.LoadScene(lastSceneName);
    }

    // ถูกเรียกอัตโนมัติหลังจาก SceneManager.LoadScene เสร็จ
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // หา Player ตัวใหม่ในฉาก
        var player = Object.FindObjectOfType<PlayerController3D>();
        if (player == null)
        {
            Debug.LogWarning("[CheckpointManager] No PlayerController3D found after scene load.");
            return;
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = lastCheckpointPosition;
        player.transform.rotation = lastCheckpointRotation;

        if (cc != null) cc.enabled = true;

        // โหลด sanity + inventory กลับจาก checkpoint
        player.RestoreCheckpointState();

        Debug.Log("[CheckpointManager] Scene reloaded & player respawned at checkpoint.");
    }
}
