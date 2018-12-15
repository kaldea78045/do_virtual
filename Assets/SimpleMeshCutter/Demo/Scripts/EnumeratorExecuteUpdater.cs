using UnityEngine;
using System.Collections;

namespace MeshCutter.Demo
{
    /// <summary>
    /// EnumeratorExecuterのキューを処理する
    /// </summary>
    public class EnumeratorExecuteUpdater : Singleton<EnumeratorExecuteUpdater>
    {
        // Update is called once per frame
        void Update ()
        {
            while(EnumeratorExecuteManager.IsAny())
            {
                StartCoroutine(EnumeratorExecuteManager.Pop());
            }

            while(EnumeratorExecuteManager.IsAnyStop())
            {
                StopCoroutine(EnumeratorExecuteManager.PopStop());
            }
        }

        /// <summary>
        /// 指定されたコルーチンを停止する
        /// </summary>
        /// <param name="coroutine"></param>
        public void Stop(IEnumerator coroutine)
        {
            StopCoroutine(coroutine);
        }
    }
}
