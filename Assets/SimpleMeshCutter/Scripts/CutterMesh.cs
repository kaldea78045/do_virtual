using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    /// <summary>
    /// メッシュカット用の独自メッシュクラス
    ///
    /// ※ スレッドではUnityEngine.Meshが利用できないため
    /// </summary>
    public class CutterMesh
    {
        public Mesh Mesh;
        private int _subMeshCount;
        public int SubMeshCount
        {
            get { return _subMeshCount; }
        }

        private int[][] _subMeshIndices;

        public int[] Triangles;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] UVs;
        public BoneWeight[] BoneWeights;

        // ボーンウェイトを持っているかどうかのフラグ
        private bool _hasBoneWeight = false;
        public bool HasBoneWeight
        {
            get { return _hasBoneWeight; }
        }

        // コンストラクタ
        public CutterMesh() { }

        // コンストラクタ
        public CutterMesh(Mesh mesh)
        {
            Mesh = mesh;

            Vertices = mesh.vertices;
            Normals = mesh.normals;
            UVs = mesh.uv;
            BoneWeights = mesh.boneWeights;

            if (mesh.boneWeights.Length > 0)
            {
                _hasBoneWeight = true;
            }

            _subMeshCount = mesh.subMeshCount;
            _subMeshIndices = new int[_subMeshCount][];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                _subMeshIndices[i] = mesh.GetIndices(i);
            }
        }

        /// <summary>
        /// 指定したindexのSubMeshの頂点を取得
        /// </summary>
        /// <param name="index">対象のサブメッシュのindex</param>
        /// <returns></returns>
        public int[] GetIndices(int index)
        {
            return _subMeshIndices[index];
        }

        /// <summary>
        /// SubMeshの頂点を設定
        /// </summary>
        /// <param name="indices">設定する頂点郡配列</param>
        public void SetIndices(int[][] indices)
        {
            _subMeshIndices = indices;
            _subMeshCount = indices.Length;
        }
    }
}
