using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    public class CutterMeshSide
    {
        #region ### Public members ###
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector2> UVs = new List<Vector2>();
        public List<int> Triangles = new List<int>();
        public List<BoneWeight> BoneWeights = new List<BoneWeight>();
        public List<List<int>> SubIndices = new List<List<int>>();
        #endregion ### Public members ###

        private CutterMesh _targetMesh;

        public void SetTargetMesh(CutterMesh targetMesh)
        {
            _targetMesh = targetMesh;
        }

        /// <summary>
        /// 全キャッシュリストをクリア
        /// </summary>
        public void ClearAll()
        {
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Triangles.Clear();
            SubIndices.Clear();
            BoneWeights.Clear();
        }

        /// <summary>
        /// トライアングルとして3頂点を追加
        /// ※ 頂点情報は元のメッシュからコピーする
        /// </summary>
        /// <param name="p1">頂点1</param>
        /// <param name="p2">頂点2</param>
        /// <param name="p3">頂点3</param>
        /// <param name="submesh">対象のサブメシュ</param>
        public void AddTriangle(int p1, int p2, int p3, int submesh)
        {
            // triangle index order goes 1,2,3,4....

            // 頂点配列のカウント。随時追加されていくため、ベースとなるindexを定義する。
            // ※ AddTriangleが呼ばれるたびに頂点数は増えていく。
            int base_index = Vertices.Count;

            // 対象サブメッシュのインデックスに追加していく
            SubIndices[submesh].Add(base_index + 0);
            SubIndices[submesh].Add(base_index + 1);
            SubIndices[submesh].Add(base_index + 2);

            // 三角形郡の頂点を設定
            Triangles.Add(base_index + 0);
            Triangles.Add(base_index + 1);
            Triangles.Add(base_index + 2);

            // 対象オブジェクトの頂点配列から頂点情報を取得し設定する
            Vertices.Add(_targetMesh.Vertices[p1]);
            Vertices.Add(_targetMesh.Vertices[p2]);
            Vertices.Add(_targetMesh.Vertices[p3]);

            // 同様に、対象オブジェクトの法線配列から法線を取得し設定する
            Normals.Add(_targetMesh.Normals[p1]);
            Normals.Add(_targetMesh.Normals[p2]);
            Normals.Add(_targetMesh.Normals[p3]);

            // 同様に、UVも。
            UVs.Add(_targetMesh.UVs[p1]);
            UVs.Add(_targetMesh.UVs[p2]);
            UVs.Add(_targetMesh.UVs[p3]);

            // BoneWeightも
            if (_targetMesh.HasBoneWeight)
            {
                BoneWeights.Add(_targetMesh.BoneWeights[p1]);
                BoneWeights.Add(_targetMesh.BoneWeights[p2]);
                BoneWeights.Add(_targetMesh.BoneWeights[p3]);
            }
        }

        /// <summary>
        /// トライアングルを追加する
        /// ※ オーバーロードしている他メソッドとは異なり、引数の値で頂点（ポリゴン）を追加する
        /// </summary>
        /// <param name="points3">トライアングルを形成する3頂点</param>
        /// <param name="normals3">3頂点の法線</param>
        /// <param name="uvs3">3頂点のUV</param>
        /// <param name="faceNormal">ポリゴンの法線</param>
        /// <param name="submesh">サブメッシュID</param>
        public void AddTriangle(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, BoneWeight[] boneWeights3, Vector3 faceNormal, int submesh)
        {
            // 引数の3頂点から法線を計算
            Vector3 calculated_normal = Vector3.Cross((points3[1] - points3[0]).normalized, (points3[2] - points3[0]).normalized);

            int p1 = 0;
            int p2 = 1;
            int p3 = 2;

            // 引数で指定された法線と逆だった場合はインデックスの順番を逆順にする（つまり面を裏返す）
            if (Vector3.Dot(calculated_normal, faceNormal) < 0)
            {
                p1 = 2;
                p2 = 1;
                p3 = 0;
            }

            int base_index = Vertices.Count;

            SubIndices[submesh].Add(base_index + 0);
            SubIndices[submesh].Add(base_index + 1);
            SubIndices[submesh].Add(base_index + 2);

            Triangles.Add(base_index + 0);
            Triangles.Add(base_index + 1);
            Triangles.Add(base_index + 2);

            Vertices.Add(points3[p1]);
            Vertices.Add(points3[p2]);
            Vertices.Add(points3[p3]);

            Normals.Add(normals3[p1]);
            Normals.Add(normals3[p2]);
            Normals.Add(normals3[p3]);

            UVs.Add(uvs3[p1]);
            UVs.Add(uvs3[p2]);
            UVs.Add(uvs3[p3]);

            if (_targetMesh.HasBoneWeight)
            {
                BoneWeights.Add(boneWeights3[p1]);
                BoneWeights.Add(boneWeights3[p2]);
                BoneWeights.Add(boneWeights3[p3]);
            }
        }
    }
}
