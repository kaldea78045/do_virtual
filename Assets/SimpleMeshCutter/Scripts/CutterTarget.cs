using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    /// <summary>
    /// Represent target for mesh cutter.
    /// </summary>
    public class CutterTarget : MonoBehaviour
    {
        #region ### Events ###
        public System.Action<CutterTarget, GameObject[]> OnCutted;
        #endregion ### Events ###

        [SerializeField][Tooltip("Material for cutted surface.")]
        private Material _cutMaterial;
        public Material CutMaterial
        {
            get { return _cutMaterial; }
        }

        [SerializeField]
        private Transform _mesh;
        public Transform Mesh
        {
            get
            {
                return _mesh;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                _mesh = value;
            }
        }

        /// <summary>
        /// In cutting, check position and normal to convert to local.
        /// </summary>
        public bool NeedsConvertLocal
        {
            get { return _mesh.GetComponent<SkinnedMeshRenderer>() != null; }
        }

        /// <summary>
        /// GameObject of mesh
        /// </summary>
        public GameObject GameObject
        {
            get { return _mesh.gameObject; }
        }

        /// <summary>
        /// Transform of mesh
        /// </summary>
        public Transform MeshTransform
        {
            get { return _mesh.transform; }
        }

        /// <summary>
        /// Mesh name
        /// </summary>
        public string Name
        {
            get { return _mesh.name; }
        }

        /// <summary>
        /// Will call back when cutting has been finished.
        /// </summary>
        public void Cutted(GameObject[] cuttedObjects)
        {
            if (OnCutted != null)
            {
                OnCutted.Invoke(this, cuttedObjects);
            }
        }
    }
}
