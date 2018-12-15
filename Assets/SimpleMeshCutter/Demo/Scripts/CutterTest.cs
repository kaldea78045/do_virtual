using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter.Demo
{
    public class CutterTest : MonoBehaviour
    {
        private Cutter _cutter;

        [SerializeField]
        private CutterTarget _target;

        private void Start()
        {
            _cutter = GetComponent<Cutter>();
            _cutter.OnCutted += OnCuttedHandler;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("Cut Start");
                _cutter.Cut(_target, transform.position, -transform.up, CuttedHandler, gameObject.name);
            }
        }

        /// <summary>
        /// Test for cutted handle.
        /// </summary>
        /// <param name="cuttedObjects">Cutted objects.</param>
        /// <param name="plane">Cutting plane normal.</param>
        /// <param name="userdata">User data.</param>
        private void CuttedHandler(bool success, GameObject[] cuttedObjects, CutterPlane plane, object userdata)
        {
            string name = userdata as string;
            Debug.Log("On cutted: " + name);
        }

        private void OnCuttedHandler(GameObject[] cuttedObjects, CutterPlane plane)
        {
            cuttedObjects[0].transform.parent.position += new Vector3(0, -0.1f, 0);
            cuttedObjects[1].transform.parent.position += new Vector3(0,  0.1f, 0);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Vector3 lt = transform.position + transform.right * 0.1f;
            Vector3 lb = transform.position - transform.right * 0.1f;
            Vector3 rt = lt + (transform.forward * 3f);
            Vector3 rb = lb + (transform.forward * 3f);
            Gizmos.DrawLine(transform.position, lt);
            Gizmos.DrawLine(transform.position, lb);

            Gizmos.DrawLine(lt, rt);
            Gizmos.DrawLine(lb, rb);
        }
    }
}
