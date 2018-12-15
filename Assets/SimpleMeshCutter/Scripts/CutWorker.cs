using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MeshCutter
{
    class CutTask
    {
        public WaitCallback Callback;
        public object Args;
    }

    /// <summary>
    /// メッシュカットをスレッドで実行するタスククラス
    /// </summary>
    public class CutWorker
    {
        #region ### Events ###
        public Action<CutterInfo> OnCutted;
        public Action<CutterInfo> OnFailed;
        #endregion ### Events ###

        [SerializeField]
        private Material _material;

        //private readonly int _defaultQueueSize = 32;
        private readonly AutoResetEvent _putNotification = new AutoResetEvent(false);

        private Queue<CutTask> _taskQueue = new Queue<CutTask>();

        private Thread[] _threadpool;
        //private Semaphore _semaphore;

        private MeshCut _meshCut = new MeshCut();

        /// <summary>
        /// Constructor
        /// </summary>
        public CutWorker()
        {
            int defaultThureadNum = SystemInfo.processorCount;
            Initialize(defaultThureadNum);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="queueSize">Queue size.</param>
        /// <param name="threadNum">Thread number.</param>
        public CutWorker(int threadNum)
        {
            Initialize(threadNum);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Initialize(int threadNum)
        {
            SetupThread(threadNum);
        }

        /// <summary>
        /// Setup thread pool.
        /// </summary>
        /// <param name="queueSize">Queue size.</param>
        /// <param name="threadNum">Thread number.</param>
        private void SetupThread(int threadNum)
        {
            _threadpool = new Thread[threadNum];
            //_semaphore = new Semaphore(0, queueSize);

            for (int i = 0; i < threadNum; i++)
            {
                _threadpool[i] = new Thread(ThreadRun);
                _threadpool[i].Name = "Cutting thread" + i;
                _threadpool[i].IsBackground = true;
                _threadpool[i].Start();
            }
        }

        /// <summary>
        /// Perform cutting in sub thread.
        /// </summary>
        /// <param name="info">Cutter information.</param>
        public void Cut(CutterInfo info)
        {
            EnqueueTask(info);
        }

        /// <summary>
        /// Enqueue cut task.
        /// </summary>
        /// <param name="info">CutterInfo.</param>
        private void EnqueueTask(CutterInfo info)
        {
            CutTask task = new CutTask
            {
                Callback = new WaitCallback(CutProc),
                Args = info,
            };

            _taskQueue.Enqueue(task);

            _putNotification.Set();
        }

        /// <summary>
        /// Cut process in sub thread.
        /// </summary>
        /// <param name="args">Cut args.</param>
        private void CutProc(object args)
        {
            CutterInfo info = args as CutterInfo;
            if (info == null)
            {
                return;
            }

            // Check error.
            // MeshCut maybe occuered error, if gameobject has destroyed in cutting.
            try
            {
                CutterMesh[] cuttedMeshes = _meshCut.Cut(info.CutterObject, info.CutterPlane);
                info.CutterMeshes = cuttedMeshes;
                if (cuttedMeshes == null)
                {
                    if (OnFailed != null)
                    {
                        OnFailed.Invoke(info);
                    }
                    return;
                }

                if (OnCutted != null)
                {
                    OnCutted.Invoke(info);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error occuered in cutting. " + e);

                if (OnFailed != null)
                {
                    OnFailed.Invoke(info);
                }
            }
        }

        /// <summary>
        /// Run thread pool loop.
        /// </summary>
        private void ThreadRun()
        {
            while (true)
            {
                if (_taskQueue.Count == 0)
                {
                    _putNotification.WaitOne();
                }

                CutTask task = _taskQueue.Dequeue();
                if (task == null)
                {
                    continue;
                }

                task.Callback(task.Args);
            }
        }
    }
}

