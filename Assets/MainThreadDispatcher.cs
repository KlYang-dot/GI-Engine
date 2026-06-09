using UnityEngine;
using System;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static readonly List<Action> _executionList = new List<Action>();
    private static MainThreadDispatcher _instance;

    /// <summary>
    /// 利用 Unity 特性，在游戏启动、首个场景加载前，自动在后台创建并常驻此组件
    /// 无需手动去场景里拖拽挂载，彻底杜绝遗漏问题
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("MainThreadDispatcher (Auto)");
        _instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// 将主线程任务投递到队列中
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_queue)
        {
            _queue.Enqueue(action);
        }
    }

    private void Update()
    {
        // 1. 极其短暂地锁住队列，只做数据拷贝，随后立即释放锁
        lock (_queue)
        {
            if (_queue.Count == 0) return;

            _executionList.Clear();
            while (_queue.Count > 0)
            {
                _executionList.Add(_queue.Dequeue());
            }
        }

        // 2. 在锁外面安全地执行任务，此时后台线程可以自由地继续 Enqueue，绝不卡顿
        for (int i = 0; i < _executionList.Count; i++)
        {
            try
            {
                _executionList[i]?.Invoke();
            }
            catch (Exception ex)
            {
                // 3. 异常隔离：单个任务崩溃，不影响队列里其他任务的正常执行
                Debug.LogError($"[MainThreadDispatcher] 执行主线程任务时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            lock (_queue)
            {
                _queue.Clear();
            }
            _instance = null;
        }
    }
}