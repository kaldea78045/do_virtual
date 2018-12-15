using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MeshCutter
{


    /// <summary>
    /// 実際に頂点位置を計算し、カットを実行するクラス
    /// </summary>
    public class MeshCut
    {
        class MeshCutData
        {
            public MeshCutData(CutterMesh victimMesh, CutterPlane blade)
            {
                VictimMesh = victimMesh;
                LeftSide.SetTargetMesh(victimMesh);
                RightSide.SetTargetMesh(victimMesh);

                Blade = blade;
            }

            public CutterMesh VictimMesh;
            public CutterPlane Blade;

            public CutterMeshSide LeftSide = new CutterMeshSide();
            public CutterMeshSide RightSide = new CutterMeshSide();

            public List<Vector3> NewVertices = new List<Vector3>();
            public List<BoneWeight> NewBoneWeights = new List<BoneWeight>();

            public List<Vector3> CapVertTracker = new List<Vector3>();
            public List<Vector3> CapVertpolygon = new List<Vector3>();
            public List<BoneWeight> CapBoneWeightpolygon = new List<BoneWeight>();
        }

        /// <summary>
        /// Cut the specified victim, blade_plane and capMaterial.
        /// （指定された「victim」をカットする。ブレード（平面）とマテリアルから切断を実行する）
        /// </summary>
        /// <param name="victim">Victim.</param>
        /// <param name="blade_plane">Blade plane.</param>
        public CutterMesh[] Cut(CutterObject victim, CutterPlane blade)
        {
            // 対象のメッシュを取得
            MeshCutData data = new MeshCutData(victim.CutterMesh, blade);

            // ここでの「3」はトライアングル
            bool[] sides = new bool[3];
            int[] indices;
            int p1, p2, p3;

            // go throught the submeshes
            // サブメッシュの数だけループ
            for (int sub = 0; sub < victim.SubMeshCount; sub++)
            {
                // サブメッシュのインデックス数を取得
                indices = victim.CutterMesh.GetIndices(sub);

                // List<List<int>>型のリスト。サブメッシュ一つ分のインデックスリスト
                data.LeftSide.SubIndices.Add(new List<int>());  // 左
                data.RightSide.SubIndices.Add(new List<int>()); // 右

                // サブメッシュのインデックス数分ループ
                for (int i = 0; i < indices.Length; i += 3)
                {
                    // p1 - p3のインデックスを取得。つまりトライアングル
                    p1 = indices[i + 0];
                    p2 = indices[i + 1];
                    p3 = indices[i + 2];

                    // それぞれ評価中のメッシュの頂点が、冒頭で定義された平面の左右どちらにあるかを評価。
                    // `GetSide` メソッドによりboolを得る。
                    sides[0] = blade.GetSide(data.VictimMesh.Vertices[p1]);
                    sides[1] = blade.GetSide(data.VictimMesh.Vertices[p2]);
                    sides[2] = blade.GetSide(data.VictimMesh.Vertices[p3]);

                    // whole triangle
                    // 頂点０と頂点１および頂点２がどちらも同じ側にある場合はカットしない
                    if (sides[0] == sides[1] && sides[0] == sides[2])
                    {
                        if (sides[0])
                        {   // left side
                            // GetSideメソッドでポジティブ（true）の場合は左側にあり
                            data.LeftSide.AddTriangle(p1, p2, p3, sub);
                        }
                        else
                        {
                            data.RightSide.AddTriangle(p1, p2, p3, sub);
                        }
                    }
                    else
                    {   // cut the triangle
                        // そうではなく、どちらかの点が平面の反対側にある場合はカットを実行する
                        CutFace(sub, sides, p1, p2, p3, data);
                    }
                }
            }

            // Check separating verticies.
            if (data.LeftSide.Vertices.Count == 0 || data.RightSide.Vertices.Count == 0)
            {
                Debug.Log("No need cutting, because all verticies are in one side.");
                return null;
            }

            data.LeftSide.SubIndices.Add(new List<int>());
            data.RightSide.SubIndices.Add(new List<int>());

            // カット開始
            Capping(data);

            // 左側のメッシュを生成
            // MeshCutSideクラスのメンバから各値をコピー
            CutterMesh leftHalfMesh = new CutterMesh
            {
                Vertices = data.LeftSide.Vertices.ToArray(),
                Triangles = data.LeftSide.Triangles.ToArray(),
                Normals = data.LeftSide.Normals.ToArray(),
                UVs = data.LeftSide.UVs.ToArray(),
                BoneWeights = data.LeftSide.BoneWeights.ToArray(),
            };

            int[][] leftSubIndices = new int[data.LeftSide.SubIndices.Count][];
            for (int i = 0; i < data.LeftSide.SubIndices.Count; i++)
            {
                leftSubIndices[i] = data.LeftSide.SubIndices[i].ToArray();
            }
            leftHalfMesh.SetIndices(leftSubIndices);

            // 右側のメッシュも同様に生成
            CutterMesh rightHalfMesh = new CutterMesh
            {
                Vertices = data.RightSide.Vertices.ToArray(),
                Triangles = data.RightSide.Triangles.ToArray(),
                Normals = data.RightSide.Normals.ToArray(),
                UVs = data.RightSide.UVs.ToArray(),
                BoneWeights = data.RightSide.BoneWeights.ToArray(),
            };

            int[][] rightSubIndices = new int[data.RightSide.SubIndices.Count][];
            for (int i = 0; i < data.RightSide.SubIndices.Count; i++)
            {
                rightSubIndices[i] = data.RightSide.SubIndices[i].ToArray();
            }
            rightHalfMesh.SetIndices(rightSubIndices);

            Debug.Log("cutter finish");

            // 左右のGameObjectの配列を返す
            return new CutterMesh[] { leftHalfMesh, rightHalfMesh };
        }

        /// <summary>
        /// カットを実行する。ただし、実際のメッシュの操作ではなく、あくまで頂点の振り分け、事前準備としての実行
        /// </summary>
        /// <param name="submesh">サブメッシュのインデックス</param>
        /// <param name="sides">評価した3頂点の左右情報</param>
        /// <param name="index1">頂点1</param>
        /// <param name="index2">頂点2</param>
        /// <param name="index3">頂点3</param>
        private void CutFace(int submesh, bool[] sides, int index1, int index2, int index3, MeshCutData data)
        {
            // 左右それぞれの情報を保持するための配列郡
            Vector3[] leftPoints = new Vector3[2];
            Vector3[] leftNormals = new Vector3[2];
            Vector2[] leftUvs = new Vector2[2];
            BoneWeight[] leftBoneWeights = new BoneWeight[2];
            Vector3[] rightPoints = new Vector3[2];
            Vector3[] rightNormals = new Vector3[2];
            Vector2[] rightUvs = new Vector2[2];
            BoneWeight[] rightBoneWeights = new BoneWeight[2];

            bool didset_left = false;
            bool didset_right = false;

            // 3頂点分繰り返す
            // 処理内容としては、左右を判定して、左右の配列に3頂点を振り分ける処理を行っている
            int p = index1;
            for (int side = 0; side < 3; side++)
            {
                switch (side)
                {
                    case 0:
                        p = index1;
                        break;
                    case 1:
                        p = index2;
                        break;
                    case 2:
                        p = index3;
                        break;
                }

                // sides[side]がtrue、つまり左側の場合
                if (sides[side])
                {
                    // すでに左側の頂点が設定されているか（3頂点が左右に振り分けられるため、必ず左右どちらかは2つの頂点を持つことになる）
                    if (!didset_left)
                    {
                        didset_left = true;

                        // ここは0,1ともに同じ値にしているのは、続く処理で
                        // leftPoints[0,1]の値を使って分割点を求める処理をしているため。
                        // つまり、アクセスされる可能性がある

                        // 頂点の設定
                        leftPoints[0] = data.VictimMesh.Vertices[p];
                        leftPoints[1] = leftPoints[0];

                        // UVの設定
                        leftUvs[0] = data.VictimMesh.UVs[p];
                        leftUvs[1] = leftUvs[0];

                        // 法線の設定
                        leftNormals[0] = data.VictimMesh.Normals[p];
                        leftNormals[1] = leftNormals[0];

                        if (data.VictimMesh.HasBoneWeight)
                        {
                            leftBoneWeights[0] = data.VictimMesh.BoneWeights[p];
                            leftBoneWeights[1] = leftBoneWeights[0];
                        }
                    }
                    else
                    {
                        // 2頂点目の場合は2番目に直接頂点情報を設定する
                        leftPoints[1] = data.VictimMesh.Vertices[p];
                        leftUvs[1] = data.VictimMesh.UVs[p];
                        leftNormals[1] = data.VictimMesh.Normals[p];

                        if (data.VictimMesh.HasBoneWeight)
                        {
                            leftBoneWeights[1] = data.VictimMesh.BoneWeights[p];
                        }
                    }
                }
                else
                {
                    // 左と同様の操作を右にも行う
                    if (!didset_right)
                    {
                        didset_right = true;

                        rightPoints[0] = data.VictimMesh.Vertices[p];
                        rightPoints[1] = rightPoints[0];
                        rightUvs[0] = data.VictimMesh.UVs[p];
                        rightUvs[1] = rightUvs[0];
                        rightNormals[0] = data.VictimMesh.Normals[p];
                        rightNormals[1] = rightNormals[0];

                        if (data.VictimMesh.HasBoneWeight)
                        {
                            rightBoneWeights[0] = data.VictimMesh.BoneWeights[p];
                            rightBoneWeights[1] = rightBoneWeights[0];
                        }
                    }
                    else
                    {
                        rightPoints[1] = data.VictimMesh.Vertices[p];
                        rightUvs[1] = data.VictimMesh.UVs[p];
                        rightNormals[1] = data.VictimMesh.Normals[p];

                        if (data.VictimMesh.HasBoneWeight)
                        {
                            rightBoneWeights[1] = data.VictimMesh.BoneWeights[p];
                        }
                    }
                }
            }

            // 分割された点の比率計算のための距離
            float normalizedDistance = 0f;

            // 距離
            float distance = 0f;

            // ---------------------------
            // 左側の処理

            // 定義した面と交差する点を探す。
            // つまり、平面によって分割される点を探す。
            // 左の点を起点に、右の点に向けたレイを飛ばし、その分割点を探る。
            data.Blade.Raycast(new Ray(leftPoints[0], (rightPoints[0] - leftPoints[0]).normalized), out distance);

            // 見つかった交差点を、頂点間の距離で割ることで、分割点の左右の割合を算出する
            normalizedDistance = distance / (rightPoints[0] - leftPoints[0]).magnitude;

            // カット後の新頂点に対する処理。フラグメントシェーダでの補完と同じく、分割した位置に応じて適切に補完した値を設定する
            Vector3 newVertex1 = Vector3.Lerp(leftPoints[0], rightPoints[0], normalizedDistance);
            Vector2 newUv1 = Vector2.Lerp(leftUvs[0], rightUvs[0], normalizedDistance);
            Vector3 newNormal1 = Vector3.Lerp(leftNormals[0], rightNormals[0], normalizedDistance);

            BoneWeight newBoneWeight1 = new BoneWeight();
            if (data.VictimMesh.HasBoneWeight)
            {
                newBoneWeight1.boneIndex0 = leftBoneWeights[0].boneIndex0;
                newBoneWeight1.boneIndex1 = leftBoneWeights[0].boneIndex1;
                newBoneWeight1.boneIndex2 = leftBoneWeights[0].boneIndex2;
                newBoneWeight1.boneIndex3 = leftBoneWeights[0].boneIndex3;
                newBoneWeight1.weight0 = Mathf.Lerp(leftBoneWeights[0].weight0, rightBoneWeights[0].weight0, normalizedDistance);
                newBoneWeight1.weight1 = Mathf.Lerp(leftBoneWeights[0].weight1, rightBoneWeights[0].weight1, normalizedDistance);
                newBoneWeight1.weight2 = Mathf.Lerp(leftBoneWeights[0].weight2, rightBoneWeights[0].weight2, normalizedDistance);
                newBoneWeight1.weight3 = Mathf.Lerp(leftBoneWeights[0].weight3, rightBoneWeights[0].weight3, normalizedDistance);

                // 新ボーンウェイト郡に追加
                data.NewBoneWeights.Add(newBoneWeight1);
            }

            // 新頂点郡に新しい頂点を追加
            data.NewVertices.Add(newVertex1);


            // ---------------------------
            // 右側の処理

            data.Blade.Raycast(new Ray(leftPoints[1], (rightPoints[1] - leftPoints[1]).normalized), out distance);

            normalizedDistance = distance / (rightPoints[1] - leftPoints[1]).magnitude;
            Vector3 newVertex2 = Vector3.Lerp(leftPoints[1], rightPoints[1], normalizedDistance);
            Vector2 newUv2 = Vector2.Lerp(leftUvs[1], rightUvs[1], normalizedDistance);
            Vector3 newNormal2 = Vector3.Lerp(leftNormals[1], rightNormals[1], normalizedDistance);
            BoneWeight newBoneWeight2 = new BoneWeight();

            if (data.VictimMesh.HasBoneWeight)
            {
                newBoneWeight2.boneIndex0 = leftBoneWeights[1].boneIndex0;
                newBoneWeight2.boneIndex1 = leftBoneWeights[1].boneIndex1;
                newBoneWeight2.boneIndex2 = leftBoneWeights[1].boneIndex2;
                newBoneWeight2.boneIndex3 = leftBoneWeights[1].boneIndex3;
                newBoneWeight2.weight0 = Mathf.Lerp(leftBoneWeights[1].weight0, rightBoneWeights[1].weight0, normalizedDistance);
                newBoneWeight2.weight1 = Mathf.Lerp(leftBoneWeights[1].weight1, rightBoneWeights[1].weight1, normalizedDistance);
                newBoneWeight2.weight2 = Mathf.Lerp(leftBoneWeights[1].weight2, rightBoneWeights[1].weight2, normalizedDistance);
                newBoneWeight2.weight3 = Mathf.Lerp(leftBoneWeights[1].weight3, rightBoneWeights[1].weight3, normalizedDistance);

                // 新ボーンウェイト郡に追加
                data.NewBoneWeights.Add(newBoneWeight2);
            }

            // 新頂点郡に新しい頂点を追加
            data.NewVertices.Add(newVertex2);

            // 計算された新しい頂点を使って、新トライアングルを左右ともに追加する
            // memo: どう分割されても、左右どちらかは1つの三角形になる気がするけど、縮退三角形的な感じでとにかく2つずつ追加している感じだろうか？
            data.LeftSide.AddTriangle(
                new Vector3[] { leftPoints[0], newVertex1, newVertex2 },
                new Vector3[] { leftNormals[0], newNormal1, newNormal2 },
                new Vector2[] { leftUvs[0], newUv1, newUv2 },
                new BoneWeight[] { leftBoneWeights[0], newBoneWeight1, newBoneWeight2 },
                newNormal1,
                submesh
            );

            data.LeftSide.AddTriangle(
                new Vector3[] { leftPoints[0], leftPoints[1], newVertex2 },
                new Vector3[] { leftNormals[0], leftNormals[1], newNormal2 },
                new Vector2[] { leftUvs[0], leftUvs[1], newUv2 },
                new BoneWeight[] { leftBoneWeights[0], leftBoneWeights[1], newBoneWeight2 },
                newNormal2,
                submesh
            );

            data.RightSide.AddTriangle(
                new Vector3[] { rightPoints[0], newVertex1, newVertex2 },
                new Vector3[] { rightNormals[0], newNormal1, newNormal2 },
                new Vector2[] { rightUvs[0], newUv1, newUv2 },
                new BoneWeight[] { rightBoneWeights[0], newBoneWeight1, newBoneWeight2 },
                newNormal1,
                submesh
            );

            data.RightSide.AddTriangle(
                new Vector3[] { rightPoints[0], rightPoints[1], newVertex2 },
                new Vector3[] { rightNormals[0], rightNormals[1], newNormal2 },
                new Vector2[] { rightUvs[0], rightUvs[1], newUv2 },
                new BoneWeight[] { rightBoneWeights[0], rightBoneWeights[1], newBoneWeight2 },
                newNormal2,
                submesh
            );
        }

        /// <summary>
        /// カットを実行
        /// </summary>
        private void Capping(MeshCutData data)
        {
            // カット用頂点追跡リスト
            // 具体的には新頂点全部に対する調査を行う。その過程で調査済みをマークする目的で利用する。
            data.CapVertTracker.Clear();

            // 新しく生成した頂点分だけループする＝全新頂点に対してポリゴンを形成するため調査を行う
            // 具体的には、カット面を構成するポリゴンを形成するため、カット時に重複した頂点を網羅して「面」を形成する頂点を調査する
            for (int i = 0; i < data.NewVertices.Count; i++)
            {
                // 対象頂点がすでに調査済みのマークされて（追跡配列に含まれて）いたらスキップ
                if (data.CapVertTracker.Contains(data.NewVertices[i]))
                {
                    continue;
                }

                // カット用ポリゴン配列をクリア
                data.CapVertpolygon.Clear();
                data.CapBoneWeightpolygon.Clear();

                // 調査頂点と次の頂点をポリゴン配列に保持する
                data.CapVertpolygon.Add(data.NewVertices[i + 0]);
                data.CapVertpolygon.Add(data.NewVertices[i + 1]);

                // 頂点に紐づくボーンウェイトを配列に保持する

                if (data.VictimMesh.HasBoneWeight)
                {
                    data.CapBoneWeightpolygon.Add(data.NewBoneWeights[i + 0]);
                    data.CapBoneWeightpolygon.Add(data.NewBoneWeights[i + 1]);
                }

                // 追跡配列に自身と次の頂点を追加する（調査済みのマークをつける）
                data.CapVertTracker.Add(data.NewVertices[i + 0]);
                data.CapVertTracker.Add(data.NewVertices[i + 1]);

                // 重複頂点がなくなるまでループし調査する
                bool isDone = false;
                while (!isDone)
                {
                    isDone = true;

                    // 新頂点郡をループし、「面」を構成する要因となる頂点をすべて抽出する。抽出が終わるまでループを繰り返す
                    // 2頂点ごとに調査を行うため、ループは2単位ですすめる
                    for (int k = 0; k < data.NewVertices.Count; k += 2)
                    { // go through the pairs
                      // ペアとなる頂点を探す
                      // ここでのペアとは、いちトライアングルから生成される新頂点のペア。
                      // トライアングルからは必ず2頂点が生成されるため、それを探す。
                      // また、全ポリゴンに対して分割点を生成しているため、ほぼ必ず、まったく同じ位置に存在する、別トライアングルの分割頂点が存在するはずである。
                        if (data.NewVertices[k] == data.CapVertpolygon[data.CapVertpolygon.Count - 1] && !data.CapVertTracker.Contains(data.NewVertices[k + 1]))
                        {   // if so add the other
                            // ペアの頂点が見つかったらそれをポリゴン配列に追加し、
                            // 調査済マークをつけて、次のループ処理に回す
                            isDone = false;
                            data.CapVertTracker.Add(data.NewVertices[k + 1]);
                            data.CapVertpolygon.Add(data.NewVertices[k + 1]);

                            if (data.VictimMesh.HasBoneWeight)
                            {
                                data.CapBoneWeightpolygon.Add(data.NewBoneWeights[k + 1]);
                            }
                        }
                        else if (data.NewVertices[k + 1] == data.CapVertpolygon[data.CapVertpolygon.Count - 1] && !data.CapVertTracker.Contains(data.NewVertices[k]))
                        {   // if so add the other
                            isDone = false;
                            data.CapVertTracker.Add(data.NewVertices[k]);
                            data.CapVertpolygon.Add(data.NewVertices[k]);

                            if (data.VictimMesh.HasBoneWeight)
                            {
                                data.CapBoneWeightpolygon.Add(data.NewBoneWeights[k]);
                            }
                        }
                    }
                }

                // 見つかった頂点郡を元に、ポリゴンを形成する
                FillCap(data);
            }
        }

        /// <summary>
        /// カット面を埋める？
        /// </summary>
        /// <param name="vertices">ポリゴンを形成する頂点リスト</param>
        /// <param name="boneWeights">ポリゴンを形成する頂点に紐付いたボーンウェイト</param>
        /// <param name="blade">カット平面</param>
        private void FillCap(MeshCutData data)
        {
            #region ### Center of vertices ###
            // カット平面の中心点を計算する
            Vector3 center = Vector3.zero;

            // 引数で渡された頂点位置をすべて合計する
            foreach (Vector3 point in data.CapVertpolygon)
            {
                center += point;
            }

            // それを頂点数の合計で割り、中心とする
            center = center / data.CapVertpolygon.Count;
            #endregion ### Center of vertices ###

            #region ### Center of bone weights ###
            BoneWeight centerWeight = new BoneWeight();
            Dictionary<int, float> weights = new Dictionary<int, float>();
            for (int i = 0; i < data.CapBoneWeightpolygon.Count; i++)
            {
                BoneWeight weight = data.CapBoneWeightpolygon[i];
                for (int j = 0; j < 4; j++)
                {
                    switch (j)
                    {
                        case 0:
                            if (weights.ContainsKey(weight.boneIndex0)) { weights[weight.boneIndex0] += weight.weight0; }
                            else { weights[weight.boneIndex0] = weight.weight0; }
                            break;
                        case 1:
                            if (weights.ContainsKey(weight.boneIndex1)) { weights[weight.boneIndex1] += weight.weight1; }
                            else { weights[weight.boneIndex1] = weight.weight1; }
                            break;
                        case 2:
                            if (weights.ContainsKey(weight.boneIndex2)) { weights[weight.boneIndex2] += weight.weight2; }
                            else { weights[weight.boneIndex2] = weight.weight2; }
                            break;
                        case 3:
                            if (weights.ContainsKey(weight.boneIndex3)) { weights[weight.boneIndex3] += weight.weight3; }
                            else { weights[weight.boneIndex3] = weight.weight3; }
                            break;
                    }
                }
            }

            var list = weights.OrderByDescending(w => w.Value).ToArray();
            int len = 4;
            if (list.Length < 4)
            {
                len = list.Length;
            }

            float total = 0;
            for (int i = 0; i < len; i++)
            {
                total += list[i].Value;
            }
            for (int i = 0; i < len; i++)
            {
                switch (i)
                {
                    case 0:
                        centerWeight.boneIndex0 = list[i].Key;
                        centerWeight.weight0 = list[i].Value / total;
                        break;
                    case 1:
                        centerWeight.boneIndex1 = list[i].Key;
                        centerWeight.weight1 = list[i].Value / total;
                        break;
                    case 2:
                        centerWeight.boneIndex2 = list[i].Key;
                        centerWeight.weight2 = list[i].Value / total;
                        break;
                    case 3:
                        centerWeight.boneIndex3 = list[i].Key;
                        centerWeight.weight3 = list[i].Value / total;
                        break;
                }
            }
            #endregion ### Center of bone weights ###

            // you need an axis based on the cap
            // カット平面をベースにしたupward
            Vector3 upward = Vector3.zero;

            // 90 degree turn
            // カット平面の法線を利用して、「上」方向を求める
            // 具体的には、平面の左側を上として利用する
            upward.x = data.Blade.Normal.y;
            upward.y = -data.Blade.Normal.x;
            upward.z = data.Blade.Normal.z;

            // 法線と「上方向」から、横軸を算出
            Vector3 left = Vector3.Cross(data.Blade.Normal, upward);

            Vector3 displacement = Vector3.zero;
            Vector3 newUV1 = Vector3.zero;
            Vector3 newUV2 = Vector3.zero;

            // 引数で与えられた頂点分ループを回す
            for (int i = 0; i < data.CapVertpolygon.Count; i++)
            {
                // 計算で求めた中心点から、各頂点への方向ベクトル
                displacement = data.CapVertpolygon[i] - center;

                // 新規生成するポリゴンのUV座標を求める。
                // displacementが中心からのベクトルのため、UV的な中心である0.5をベースに、内積を使ってUVの最終的な位置を得る
                newUV1 = Vector3.zero;
                newUV1.x = 0.5f + Vector3.Dot(displacement, left);
                newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
                newUV1.z = 0.5f + Vector3.Dot(displacement, data.Blade.Normal);

                // 次の頂点。ただし、最後の頂点の次は最初の頂点を利用するため、若干トリッキーな指定方法をしている（% vertices.Count）
                displacement = data.CapVertpolygon[(i + 1) % data.CapVertpolygon.Count] - center;

                newUV2 = Vector3.zero;
                newUV2.x = 0.5f + Vector3.Dot(displacement, left);
                newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
                newUV2.z = 0.5f + Vector3.Dot(displacement, data.Blade.Normal);

                // 左側のポリゴンとして、求めたUVを利用してトライアングルを追加
                data.LeftSide.AddTriangle(
                    new Vector3[]{
                        data.CapVertpolygon[i],
                        data.CapVertpolygon[(i + 1) % data.CapVertpolygon.Count],
                        center
                    },
                    new Vector3[]{
                        -data.Blade.Normal,
                        -data.Blade.Normal,
                        -data.Blade.Normal
                    },
                    new Vector2[]{
                        newUV1,
                        newUV2,
                        new Vector2(0.5f, 0.5f)
                    },
                    new BoneWeight[]
                    {
                        data.VictimMesh.HasBoneWeight ? data.CapBoneWeightpolygon[i] : new BoneWeight(),
                        data.VictimMesh.HasBoneWeight ? data.CapBoneWeightpolygon[(i + 1) % data.CapVertpolygon.Count] : new BoneWeight(),
                        data.VictimMesh.HasBoneWeight ? centerWeight : new BoneWeight(),
                    },
                    -data.Blade.Normal,
                    data.LeftSide.SubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
                );

                // 右側のトライアングル。基本は左側と同じだが、法線だけ逆向き。
                data.RightSide.AddTriangle(
                    new Vector3[]{
                        data.CapVertpolygon[i],
                        data.CapVertpolygon[(i + 1) % data.CapVertpolygon.Count],
                        center
                    },
                    new Vector3[]{
                        data.Blade.Normal,
                        data.Blade.Normal,
                        data.Blade.Normal
                    },
                    new Vector2[]{
                        newUV1,
                        newUV2,
                        new Vector2(0.5f, 0.5f)
                    },
                    new BoneWeight[]
                    {
                        data.VictimMesh.HasBoneWeight ? data.CapBoneWeightpolygon[i] : new BoneWeight(),
                        data.VictimMesh.HasBoneWeight ? data.CapBoneWeightpolygon[(i + 1) % data.CapVertpolygon.Count] : new BoneWeight(),
                        data.VictimMesh.HasBoneWeight ? centerWeight : new BoneWeight(),
                    },
                    data.Blade.Normal,
                    data.RightSide.SubIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
                );
            }
        }
    }
}
