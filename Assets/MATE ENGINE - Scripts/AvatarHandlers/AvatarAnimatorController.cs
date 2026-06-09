using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 控制 Avatar 的动画状态（空闲、拖拽、跳舞），并根据当前播放音频的应用程序（白名单）自动触发跳舞。
/// 
/// 重要修复：
/// 原代码在 IsValidAppPlaying() 中直接调用 NAudio 的 AudioSessionManager，而该 COM 接口要求 STA 线程，
/// 但 Unity 主线程为 MTA，导致随机崩溃。现已改为通过 AudioSessionService 异步获取音频会话列表，
/// 并在主线程回调中更新缓存状态，彻底避免崩溃。
/// </summary>
public class AvatarAnimatorController : MonoBehaviour
{
    [Header("State Values")]
    public Animator animator;
    public float SOUND_THRESHOLD = 0.02f;               // 音量阈值（未使用，保留兼容）
    public List<string> allowedApps = new();            // 允许触发跳舞的进程名白名单
    public int totalIdleAnimations = 10;                // 空闲动画总数
    public float IDLE_SWITCH_TIME = 12f;                // 空闲动画切换间隔
    public float IDLE_TRANSITION_TIME = 3f;             // 空闲动画过渡时间
    public int DANCE_CLIP_COUNT = 5;                    // 跳舞动画片段数量

    [Header("Dancing")]
    public bool enableDancing = true;                   // 是否允许跳舞
    public bool enableDanceSwitch = true;               // 是否允许切换跳舞动作
    public float DANCE_SWITCH_TIME = 15f;               // 跳舞动作切换间隔
    public float DANCE_TRANSITION_TIME = 2f;            // 跳舞动作过渡时间

    public bool BlockDraggingOverride = false;          // 强制禁止拖拽动画

    // Animator 参数哈希
    private static readonly int danceIndexParam = Animator.StringToHash("DanceIndex");
    private static readonly int isIdleParam = Animator.StringToHash("isIdle");
    private static readonly int isDraggingParam = Animator.StringToHash("isDragging");
    private static readonly int isDancingParam = Animator.StringToHash("isDancing");
    private static readonly int idleIndexParam = Animator.StringToHash("IdleIndex");

    // 音频检测相关（异步，避免主线程 COM 调用）
    private bool cachedIsValidAppPlaying = false;       // 缓存的检测结果
    private bool isCheckingAudio = false;               // 是否正在异步检测中
    private float lastAudioCheckTime = 0f;
    private float audioCheckInterval = 2f;              // 每2秒检测一次（与原协程间隔一致）

    // 状态控制
    private Coroutine soundCheckCoroutine;
    private Coroutine idleTransitionCoroutine;
    private Coroutine danceTransitionCoroutine;
    private float idleTimer;
    private float danceTimer;
    private int idleState;
    private int danceState;
    private float dragLockTimer;
    private bool mouseHeld;
    public bool isDragging, isDancing, isIdle;

    [Header("Character Mode")]
    public bool enableHusbandoMode = false;             // 是否为男性模式
    private static readonly int isMaleParam = Animator.StringToHash("isMale");
    private static readonly int isFemaleParam = Animator.StringToHash("isFemale");

    void OnEnable()
    {
        animator ??= GetComponent<Animator>();
        Application.runInBackground = true;

        // 初始化性别参数
        animator.SetFloat(isFemaleParam, enableHusbandoMode ? 0f : 1f);
        animator.SetFloat(isMaleParam, enableHusbandoMode ? 1f : 0f);

        // 启动音频检测协程（异步方式，不阻塞主线程）
        soundCheckCoroutine = StartCoroutine(CheckSoundContinuously());
    }

    void OnDisable()
    {
        CleanupAudioResources();
    }

    void OnDestroy()
    {
        CleanupAudioResources();
    }

    void OnApplicationQuit()
    {
        CleanupAudioResources();
    }

    /// <summary>
    /// 协程：定期触发音频检测（异步，非阻塞）
    /// </summary>
    IEnumerator CheckSoundContinuously()
    {
        var wait = new WaitForSeconds(audioCheckInterval);
        while (true)
        {
            CheckForSound();
            yield return wait;
        }
    }

    /// <summary>
    /// 触发音频检测（异步请求，不等待结果）
    /// </summary>
    void CheckForSound()
    {
        // 条件检查：是否被阻止或禁用跳舞
        if (MenuActions.IsMovementBlocked() || !enableDancing)
        {
            if (isDancing) SetDancing(false);
            return;
        }

        // 拖拽时不允许跳舞
        if (isDragging) return;

        // 节流：避免过于频繁的异步请求
        if (Time.time - lastAudioCheckTime < audioCheckInterval) return;
        if (isCheckingAudio) return;

        lastAudioCheckTime = Time.time;
        isCheckingAudio = true;

        // 通过 AudioSessionService 异步获取当前播放音频的进程列表（在 STA 线程中执行）
        AudioSessionService.Instance.GetRunningAudioAppsAsync(apps =>
        {
            // 此回调运行在主线程，可以安全更新缓存和 UI
            bool isValid = false;
            if (apps != null)
            {
                // 检查是否有任何一个正在播放音频的进程匹配白名单
                foreach (var app in apps)
                {
                    foreach (var allowed in allowedApps)
                    {
                        if (app.StartsWith(allowed, System.StringComparison.OrdinalIgnoreCase))
                        {
                            isValid = true;
                            break;
                        }
                    }
                    if (isValid) break;
                }
            }
            cachedIsValidAppPlaying = isValid;

            // 根据结果决定跳舞状态
            if (isValid && !isDancing)
                StartDancing();
            else if (!isValid && isDancing)
                SetDancing(false);

            isCheckingAudio = false;
        });
    }

    /// <summary>
    /// 开始跳舞（仅当满足条件时）
    /// </summary>
    void StartDancing()
    {
        if (!enableDancing) return;
        isDancing = true;
        danceTimer = 0f;
        danceState = Random.Range(0, DANCE_CLIP_COUNT);
        animator.SetBool(isDancingParam, true);
        animator.SetFloat(danceIndexParam, danceState);
    }

    /// <summary>
    /// 设置跳舞状态
    /// </summary>
    void SetDancing(bool value)
    {
        if (isDancing == value) return;
        isDancing = value;
        animator.SetBool(isDancingParam, value);
        if (!value && danceTransitionCoroutine != null)
        {
            StopCoroutine(danceTransitionCoroutine);
            danceTransitionCoroutine = null;
        }
    }

    /// <summary>
    /// 获取缓存的检测结果（原 IsValidAppPlaying 改为直接返回缓存，不再进行任何 COM 调用）
    /// </summary>
    bool IsValidAppPlaying()
    {
        return cachedIsValidAppPlaying;
    }

    void Update()
    {
        // 性别参数更新
        animator.SetFloat(isFemaleParam, enableHusbandoMode ? 0f : 1f);
        animator.SetFloat(isMaleParam, enableHusbandoMode ? 1f : 0f);

        // 全局阻止条件
        if (BlockDraggingOverride || MenuActions.IsMovementBlocked() || TutorialMenu.IsActive)
        {
            if (isDragging) SetDragging(false);
            if (isDancing) SetDancing(false);
            return;
        }

        // 拖拽逻辑
        if (Input.GetMouseButtonDown(0))
        {
            SetDragging(true);
            mouseHeld = true;
            dragLockTimer = 0.30f;
            SetDancing(false);          // 拖拽时强制停止跳舞
        }
        if (Input.GetMouseButtonUp(0))
            mouseHeld = false;

        if (dragLockTimer > 0f)
        {
            dragLockTimer -= Time.deltaTime;
            animator.SetBool(isDraggingParam, true);
        }
        else if (!mouseHeld && isDragging)
            SetDragging(false);

        // 空闲动画切换
        idleTimer += Time.deltaTime;
        if (idleTimer > IDLE_SWITCH_TIME)
        {
            idleTimer = 0f;
            int next = (idleState + 1) % totalIdleAnimations;
            if (next == 0)
                animator.SetFloat(idleIndexParam, 0);
            else
            {
                if (idleTransitionCoroutine != null)
                    StopCoroutine(idleTransitionCoroutine);
                idleTransitionCoroutine = StartCoroutine(SmoothIdleTransition(next));
            }
            idleState = next;
        }
        UpdateIdleStatus();

        // 跳舞动画切换（仅当跳舞启用且允许切换）
        if (isDancing && enableDanceSwitch)
        {
            danceTimer += Time.deltaTime;
            if (danceTimer > DANCE_SWITCH_TIME)
            {
                danceTimer = 0f;
                int nextDance = (danceState + 1) % DANCE_CLIP_COUNT;
                if (nextDance == 0)
                    animator.SetFloat(danceIndexParam, 0);
                else
                {
                    if (danceTransitionCoroutine != null)
                        StopCoroutine(danceTransitionCoroutine);
                    danceTransitionCoroutine = StartCoroutine(SmoothDanceTransition(nextDance));
                }
                danceState = nextDance;
            }
        }
    }

    void SetDragging(bool value)
    {
        if (isDragging == value) return;
        isDragging = value;
        animator.SetBool(isDraggingParam, value);
    }

    void UpdateIdleStatus()
    {
        bool inIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        if (isIdle != inIdle)
        {
            isIdle = inIdle;
            animator.SetBool(isIdleParam, isIdle);
        }
    }

    IEnumerator SmoothIdleTransition(int newIdle)
    {
        float elapsed = 0f;
        float start = animator.GetFloat(idleIndexParam);
        while (elapsed < IDLE_TRANSITION_TIME)
        {
            elapsed += Time.deltaTime;
            animator.SetFloat(idleIndexParam, Mathf.Lerp(start, newIdle, elapsed / IDLE_TRANSITION_TIME));
            yield return null;
        }
        animator.SetFloat(idleIndexParam, newIdle);
    }

    IEnumerator SmoothDanceTransition(int newDance)
    {
        float elapsed = 0f;
        float start = animator.GetFloat(danceIndexParam);
        while (elapsed < DANCE_TRANSITION_TIME)
        {
            elapsed += Time.deltaTime;
            animator.SetFloat(danceIndexParam, Mathf.Lerp(start, newDance, elapsed / DANCE_TRANSITION_TIME));
            yield return null;
        }
        animator.SetFloat(danceIndexParam, newDance);
    }

    public bool IsInIdleState() => isIdle;

    /// <summary>
    /// 清理所有协程和音频资源（AudioSessionService 是单例，无需手动释放）
    /// </summary>
    void CleanupAudioResources()
    {
        if (soundCheckCoroutine != null)
        {
            StopCoroutine(soundCheckCoroutine);
            soundCheckCoroutine = null;
        }
        if (idleTransitionCoroutine != null)
        {
            StopCoroutine(idleTransitionCoroutine);
            idleTransitionCoroutine = null;
        }
        if (danceTransitionCoroutine != null)
        {
            StopCoroutine(danceTransitionCoroutine);
            danceTransitionCoroutine = null;
        }
        // 注意：不再需要手动释放 MMDevice 和 MMDeviceEnumerator，因为已完全移除同步调用
    }
}