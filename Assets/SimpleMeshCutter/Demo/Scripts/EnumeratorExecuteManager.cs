using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MeshCutter.Demo
{
    /// <summary>
    /// Enumeratorをコルーチンに渡すためのマネージャクラス
    /// </summary>
    public class EnumeratorExecuteManager
    {
        static Queue<IEnumerator> _queue;
        static Queue<IEnumerator> Queue
        {
            get
            {
                if (_queue == null)
                {
                    _queue = new Queue<IEnumerator>();
                }
                return _queue;
            }
        }

        static Queue<IEnumerator> _stopQueue;
        static Queue<IEnumerator> StopQueue
        {
            get
            {
                if (_stopQueue == null)
                {
                    _stopQueue = new Queue<IEnumerator>();
                }
                return _stopQueue;
            }
        }
        
        /// <summary>
        /// キューにIEnumeratorを追加する
        /// </summary>
        /// <param name="enumerator"></param>
        static public void Push(IEnumerator enumerator)
        {
            // インスタンスが生成されていないことを考慮して、インスタンスにアクセスする
            var instance = EnumeratorExecuteUpdater.Instance;
            Queue.Enqueue(enumerator);
        }

        /// <summary>
        /// キューに追加された最後の要素を取り出す
        /// </summary>
        /// <returns></returns>
        static public IEnumerator Pop()
        {
            return Queue.Dequeue();
        }

        /// <summary>
        /// キューにひとつ以上オブジェクトがある
        /// </summary>
        /// <returns></returns>
        static public bool IsAny()
        {
            return Queue.Count > 0;
        }

        /// <summary>
        /// ストップキューにIEnumeratorを追加する
        /// </summary>
        /// <param name="enumerator"></param>
        static public void PushStop(IEnumerator enumerator)
        {
            StopQueue.Enqueue(enumerator);
        }

        /// <summary>
        /// キューに追加された最後の要素を取り出す
        /// </summary>
        /// <returns></returns>
        static public IEnumerator PopStop()
        {
            return StopQueue.Dequeue();
        }

        /// <summary>
        /// キューにひとつ以上オブジェクトがある
        /// </summary>
        /// <returns></returns>
        static public bool IsAnyStop()
        {
            return StopQueue.Count > 0;
        }
    }
}
