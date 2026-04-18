using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Events;

public class GhostChaseSequence : MonoBehaviour
{
    public enum GhostState
    {
        IdleHidden,
        CrawlingOut,
        StandingUp,
        ShowcaseWalk,
        Chase,
        Finished
    }

    [Header("Visual Root")]
    [SerializeField] private GameObject ghostRoot;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Transform crawlEndPoint;
    [SerializeField] private Transform showcasePoint;

    [Header("Timing")]
    [SerializeField] private float delayBeforeCrawl = 0.5f;
    [SerializeField] private float standUpDuration = 1.0f;
    [SerializeField] private float showcaseIdleDuration = 1.0f;
    [SerializeField] private float maxChaseDuration = 10.0f;

    [Header("Movement Speeds")]
    [SerializeField] private float crawlSpeed = 1.5f;
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float acc = 0f;
    [SerializeField] private float angular = 0f;

    [Header("Catch Logic")]
    [SerializeField] private float catchDistance = 1.5f;
    [SerializeField] private float catchCheckInterval = 0.05f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string crawlStateName = "Crawl";
    [SerializeField] private string standTriggerName = "Stand";
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private string runStateName = "Run";

    [Header("Trigger")]
    [SerializeField] private Collider triggerCollider;


    [Header("Sound")]
    [SerializeField]  public AudioClip StartSound;
    [SerializeField] public AudioClip MetalDrop;

    [SerializeField] public AudioClip GhostRun;

    public AudioSource audioSource1;
    public AudioSource audioSource2;

    [Header("Events")]
    public UnityEvent onSequenceStart;
    public UnityEvent onChaseStart;
    public UnityEvent onPlayerCaught;
    public UnityEvent onChaseFailed;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private GhostState state = GhostState.IdleHidden;
    private bool sequenceStarted = false;
    private bool playerCaught = false;
    private bool chaseFinished = false;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        agent.enabled = false;
        if (ghostRoot != null)
        {
            ghostRoot.SetActive(false);
            if (debugLog) Debug.Log("[GhostChaseSequence] Start: hide ghostRoot");
        }
        else
        {
            if (debugLog) Debug.LogWarning("[GhostChaseSequence] ghostRoot is NULL");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (sequenceStarted) return;

        sequenceStarted = true;
        if (debugLog) Debug.Log("[GhostChaseSequence] OnTriggerEnter by Player, start sequence");
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        state = GhostState.IdleHidden;

        if (ghostRoot != null)
            ghostRoot.SetActive(true);

        onSequenceStart?.Invoke();
        PlayGhostEncounterSound();



        if (delayBeforeCrawl > 0f)
            yield return new WaitForSeconds(delayBeforeCrawl);

        if (crawlEndPoint != null)
        {
            state = GhostState.CrawlingOut;

            if (agent != null)
                
            agent.enabled = false;


            if (animator != null && !string.IsNullOrEmpty(crawlStateName))
                animator.CrossFade(crawlStateName, 0.1f);

            Vector3 startPos = transform.position;
            Vector3 targetPos = crawlEndPoint.position;
            float t = 0f;
            float crawlDuration = 2.0f;

            while (t < 1f)
            {
                t += Time.deltaTime / crawlDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        state = GhostState.StandingUp;

        if (agent != null)
        {
            agent.enabled = true;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }

        if (animator != null && !string.IsNullOrEmpty(standTriggerName))
            animator.SetTrigger(standTriggerName);

        if (standUpDuration > 0f)
            yield return new WaitForSeconds(standUpDuration);

        if (showcasePoint != null)
        {
            state = GhostState.ShowcaseWalk;
            SetAgentForMovement(walkSpeed, true);

            if (animator != null && !string.IsNullOrEmpty(walkStateName))
            {
                animator.CrossFade(walkStateName, 0.1f);
            }

            agent.SetDestination(showcasePoint.position);
            if (debugLog) Debug.Log("[GhostChaseSequence] Walk to " + showcasePoint.name);
            yield return StartCoroutine(WaitUntilReachDestination(showcasePoint.position, 0.2f));
        }

        if (showcaseIdleDuration > 0f)
        {
            SetAgentForMovement(0f, false);
            yield return new WaitForSeconds(showcaseIdleDuration);
        }

        if (player != null)
        {
            state = GhostState.Chase;
            onChaseStart?.Invoke();
            PlayGhostRunSound();

            SetAgentForMovement(runSpeed, true);
            if (animator != null && !string.IsNullOrEmpty(runStateName))
            {
                animator.CrossFade(runStateName, 0.1f);
            }

            if (debugLog) Debug.Log("[GhostChaseSequence] Start chase");
            yield return StartCoroutine(ChaseRoutine());
        }

        if (!playerCaught && !chaseFinished)
        {
            chaseFinished = true;
            if (debugLog) Debug.Log("[GhostChaseSequence] Chase failed");
            onChaseFailed?.Invoke();
        }

        state = GhostState.Finished;
    }

    private IEnumerator ChaseRoutine()
    {
        float timer = 0f;
        float catchCheckTimer = 0f;

        while (timer < maxChaseDuration && !playerCaught)
        {
            if (player == null) yield break;

            timer += Time.deltaTime;
            catchCheckTimer += Time.deltaTime;

            agent.SetDestination(player.position);

            if (catchCheckTimer >= catchCheckInterval)
            {
                catchCheckTimer = 0f;
                float sqrDist = (player.position - transform.position).sqrMagnitude;
                if (sqrDist <= catchDistance * catchDistance)
                {
                    playerCaught = true;
                    if (debugLog) Debug.Log("[GhostChaseSequence] Player caught");
                    HandlePlayerCaught();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void HandlePlayerCaught()
    {
        SetAgentForMovement(0f, false);
        onPlayerCaught?.Invoke();
    }

    private void SetAgentForMovement(float speed, bool enable)
    {
        if (agent == null) return;

        agent.isStopped = !enable;
        agent.speed = speed;
        Debug.Log("Speed Set To: " + speed + " | Agent speed now: " + agent.speed);


        if (enable)
        {
            agent.acceleration = Mathf.Max(acc, speed);
            agent.angularSpeed = angular;
        }
    }

    private IEnumerator WaitUntilReachDestination(Vector3 target, float arriveRadius)
    {
        if (agent == null) yield break;

        while (true)
        {
            float sqrDist = (agent.transform.position - target).sqrMagnitude;
            if (sqrDist <= arriveRadius * arriveRadius) break;

            if (!agent.pathPending && agent.remainingDistance <= arriveRadius)
                break;

            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (crawlEndPoint != null)
            Gizmos.DrawWireSphere(crawlEndPoint.position, 0.25f);

        Gizmos.color = Color.yellow;
        if (showcasePoint != null)
            Gizmos.DrawWireSphere(showcasePoint.position, 0.25f);

        Gizmos.color = Color.red;
        if (player != null && catchDistance > 0f)
            Gizmos.DrawWireSphere(player.position, catchDistance);
    }
    public void PlayGhostEncounterSound()
    {
        // ตรวจสอบว่ามีไฟล์เสียงอยู่หรือไม่
        if (StartSound != null && audioSource1 != null)
        {
            // สั่งให้เล่นเสียงทันที
            audioSource1.PlayOneShot(StartSound);
            audioSource1.PlayOneShot(MetalDrop);
            // (ทางเลือก: ใช้ audioSource.Play() ถ้าต้องการให้เล่นซ้ำ หรือต้องการควบคุมมากกว่านี้)
        }
    }
    public void PlayGhostRunSound()
    {
        // ตรวจสอบว่ามีไฟล์เสียงอยู่หรือไม่
        if (StartSound != null && audioSource2 != null)
        {
            // สั่งให้เล่นเสียงทันที
            audioSource2.PlayOneShot(GhostRun);
            // (ทางเลือก: ใช้ audioSource.Play() ถ้าต้องการให้เล่นซ้ำ หรือต้องการควบคุมมากกว่านี้)
        }
    }
}
