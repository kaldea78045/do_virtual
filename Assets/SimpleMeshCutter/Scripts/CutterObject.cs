using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    /// <summary>
    /// カット対象を表すオブジェクトクラス
    /// </summary>
    public class CutterObject
    {
        public CutterMesh CutterMesh;

        private Mesh _mesh;
        public Mesh Mesh
        {
            get { return _mesh; }
        }

        private Matrix4x4[] _bindposes;
        public Matrix4x4[] Bindposes
        {
            get
            {
                return _bindposes;
            }
        }

        public int SubMeshCount
        {
            get { return CutterMesh.SubMeshCount; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool NeedsScaleAdjust { get; set; }
        private CutterTarget _target;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">カットターゲット</param>
        public CutterObject(CutterTarget target, bool needAnimation = false)
        {
            _target = target;
            _name = target.Name;
            GetMeshInfo(target.GameObject, out _mesh, out _bindposes);
            CutterMesh = new CutterMesh(_mesh);
        }

        private void GetMeshInfo(GameObject target, out Mesh outMesh, out Matrix4x4[] outBindposes)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter != null)
            {
                NeedsScaleAdjust = true;

                outMesh = filter.mesh;
                outBindposes = new Matrix4x4[0];

                return;
            }

            SkinnedMeshRenderer renderer = target.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                NeedsScaleAdjust = false;

                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh);
                mesh.boneWeights = renderer.sharedMesh.boneWeights;
                outMesh = mesh;

                Matrix4x4 scale = Matrix4x4.Scale(_target.transform.localScale).inverse;
                outBindposes = new Matrix4x4[renderer.bones.Length];
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    outBindposes[i] = renderer.bones[i].worldToLocalMatrix * target.transform.localToWorldMatrix * scale;
                }

                return;
            }

            outMesh = null;
            outBindposes = new Matrix4x4[0];
        }
    }
}
