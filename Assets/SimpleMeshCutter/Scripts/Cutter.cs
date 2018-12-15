using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    /// <summary>
    /// カット実行後のコールバック
    /// </summary>
    public delegate void CutterCallback(bool success, GameObject[] cuttedObjects, CutterPlane plane, object userdata);

    /// <summary>
    /// カッター処理用引数情報
    /// </summary>
    public class CutterInfo
    {
        public CutterTarget CutterTarget;
        public CutterPlane CutterPlane;
        public CutterObject CutterObject;
        public CutterCallback CutterCallback;
        public CutterMesh[] CutterMeshes;
        public object UserData;
    }

    /// <summary>
    /// カッター。カットを実行するクラス
    /// </summary>
    public class Cutter : MonoBehaviour
    {
        #region ### Events ###
        public Action<GameObject[], CutterPlane> OnCutted;
        public Action<GameObject> OnFailed;
        #endregion ### Events ###

        [SerializeField]
        [Tooltip("Auto destroy after cutting target.")]
        private bool _autoDestroyTarget = true;
        public bool AutoDestroyTarget
        {
            get { return _autoDestroyTarget; }
            set { _autoDestroyTarget = value; }
        }

        [SerializeField]
        [Tooltip("Needs continus animation.")]
        private bool _needAnimation = false;

        public bool NeedAnimation
        {
            get { return _needAnimation; }
            set { _needAnimation = value; }
        }

        [SerializeField]
        [Tooltip("Default material for cutting plane.")]
        private Material _cutDefaultMaterial;
        public Material CutDefaultMaterial
        {
            get { return _cutDefaultMaterial; }
            set { _cutDefaultMaterial = value; }
        }

        private CutWorker _worker;
        //private CutterInfo _lastCutterInfo;

        private List<CutterInfo> _cuttedInfoList = new List<CutterInfo>();
        private List<CutterInfo> _failedCutInfoList = new List<CutterInfo>();
        private Queue<CutterInfo> _cuttedInfoQueue = new Queue<CutterInfo>();
        private Queue<CutterInfo> _failedCutInfoQueue = new Queue<CutterInfo>();

        //private bool _hasCutted = false;
        private bool _isInitialized = false;

        #region ### MonoBehaviour ###
        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (_cuttedInfoList.Count != 0)
            {
                lock(_cuttedInfoList)
                {
                    for (int i = 0; i < _cuttedInfoList.Count; i++)
                    {
                        _cuttedInfoQueue.Enqueue(_cuttedInfoList[i]);
                    }
                    _cuttedInfoList.Clear();
                }
            }

            while (_cuttedInfoQueue.Count != 0)
            {
                CutterInfo info = _cuttedInfoQueue.Dequeue();
                CreateCuttedObjects(info);
            }

            if (_failedCutInfoList.Count != 0)
            {
                lock (_failedCutInfoList)
                {
                    for (int i = 0; i < _failedCutInfoList.Count; i++)
                    {
                        _failedCutInfoQueue.Enqueue(_failedCutInfoList[i]);
                    }
                    _failedCutInfoList.Clear();
                }
            }

            while (_failedCutInfoQueue.Count != 0)
            {
                CutterInfo info = _failedCutInfoQueue.Dequeue();
                if (info.CutterCallback != null)
                {
                    info.CutterCallback.Invoke(false, null, info.CutterPlane, info.UserData);
                }

                if (OnFailed != null)
                {
                    OnFailed.Invoke(info.CutterTarget.gameObject);
                }
            }
        }
        #endregion ### MonoBehaviour ###

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            _worker = new CutWorker();
            _worker.OnCutted += OnCuttedHandler;
            _worker.OnFailed += OnFailedHanlder;
        }

        /// <summary>
        /// Perform cutting.
        /// </summary>
        /// <param name="target">Cut target object.</param>
        /// <param name="position">Cut plane position.</param>
        /// <param name="normal">Cut plane normal.</param>
        /// <param name="callback">Callback for cutting process. (optional)</param>
        /// <param name="userdata">User data. (optional)</param>
        public void Cut(CutterTarget target, Vector3 position, Vector3 normal, CutterCallback callback = null, object userdata = null)
        {
            // Create convert matrix for position.
            Matrix4x4 worldToLocMat = target.MeshTransform.worldToLocalMatrix;

            if (target.NeedsConvertLocal)
            {
                Matrix4x4 scaleMat = Matrix4x4.Scale(target.MeshTransform.lossyScale);
                worldToLocMat = scaleMat * worldToLocMat;
            }

            Vector4 pos = new Vector4(position.x, position.y, position.z, 1f);
            Vector3 localPos = worldToLocMat * pos;

            // Create convert matrix for normal.
            Vector3 nor = new Vector4(normal.x, normal.y, normal.z, 1f);
            Vector4 c0 = worldToLocMat.GetColumn(0);
            Vector4 c1 = worldToLocMat.GetColumn(1);
            Vector4 c2 = worldToLocMat.GetColumn(2);
            Vector4 c3 = new Vector4(0, 0, 0, 1f);

            Matrix4x4 worldToLocNormMat = new Matrix4x4();
            worldToLocNormMat.SetColumn(0, c0);
            worldToLocNormMat.SetColumn(1, c1);
            worldToLocNormMat.SetColumn(2, c2);
            worldToLocNormMat.SetColumn(3, c3);
            worldToLocNormMat = worldToLocNormMat.inverse.transpose;
            Vector3 localNor = worldToLocNormMat * nor;
            localNor.Normalize();

            CutterPlane blade = new CutterPlane(localPos, localNor);

            CutterInfo info = new CutterInfo
            {
                CutterTarget = target,
                CutterObject = new CutterObject(target, _needAnimation),
                CutterPlane = blade,
                CutterCallback = callback,
                UserData = userdata,
            };

            _worker.Cut(info);
        }

        /// <summary>
        /// 対象ゲームオブジェクトからマテリアルリストを取得する
        /// </summary>
        /// <param name="target">対象のゲームオブジェクト</param>
        /// <returns>マテリアルリスト</returns>
        private Material[] GetMaterials(GameObject target)
        {
            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer.sharedMaterials;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.materials;
            }

            return null;
        }
        
        /// <summary>
        /// ルートボーンから再帰的に、指定した名前を持つボーンを検索する
        /// </summary>
        /// <param name="targetName">検索対象のボーン名</param>
        /// <param name="rootBone">ルートボーン</param>
        /// <returns>見つかったボーンへの参照</returns>
        private Transform SearchBone(string targetName, Transform rootBone)
        {
            if (targetName == rootBone.name)
            {
                return rootBone;
            }

            foreach (Transform bone in rootBone)
            {
                if (bone.childCount != 0)
                {
                    Transform findBone = SearchBone(targetName, bone);
                    if (findBone != null)
                    {
                        return findBone;
                    }
                }
                else
                {
                    if (targetName == bone.name)
                    {
                        return bone;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// カットされたメッシュ情報を元にオブジェクトを生成する
        /// </summary>
        private void CreateCuttedObjects(CutterInfo info)
        {
            CutterTarget target = info.CutterTarget;

            if (target == null)
            {
                return;
            }

            // 設定されているマテリアル配列を取得
            Material[] mats = GetMaterials(target.GameObject);

            // カット面分増やしたマテリアル配列を準備
            Material[] newMats = new Material[mats.Length + 1];

            // 既存のものを新しい配列にコピー
            mats.CopyTo(newMats, 0);

            // 新しいマテリアル配列の最後に、カット面用マテリアルを追加
            newMats[mats.Length] = (target.CutMaterial != null) ? target.CutMaterial : _cutDefaultMaterial;

            // 生成したマテリアルリストを再設定
            mats = newMats;

            CutterObject cutterObject = info.CutterObject;
            CutterMesh[] cuttedMeshes = info.CutterMeshes;
            GameObject[] cuttedObject = new GameObject[cuttedMeshes.Length];

            // 生成されたメッシュ分、オブジェクトを生成する
            for (int i = 0; i < cuttedMeshes.Length; i++)
            {
                #region ### Create new object ###
                GameObject newObj = new GameObject("[Cutterd]" + target.Name + i);
                newObj.transform.position = target.transform.position;
                newObj.transform.rotation = target.transform.rotation;
                newObj.transform.localScale = target.transform.localScale;
                #endregion ### Create new object ###

                #region ### Create new mesh ###
                Mesh mesh = new Mesh();
                mesh.name = "Split Mesh";
                mesh.vertices = cuttedMeshes[i].Vertices;
                mesh.triangles = cuttedMeshes[i].Triangles;
                mesh.normals = cuttedMeshes[i].Normals;
                mesh.uv = cuttedMeshes[i].UVs;
                mesh.subMeshCount = cuttedMeshes[i].SubMeshCount;
                for (int j = 0; j < cuttedMeshes[i].SubMeshCount; j++)
                {
                    mesh.SetIndices(cuttedMeshes[i].GetIndices(j), MeshTopology.Triangles, j);
                }
                #endregion ### Create new mesh ###

                GameObject obj;

                // アニメーションが必要な場合はボーンのセットアップを行う
                if (_needAnimation)
                {
                    SkinnedMeshRenderer oriRenderer = target.GameObject.GetComponent<SkinnedMeshRenderer>();
                    Transform newRootBone = Instantiate(oriRenderer.rootBone.gameObject).transform;
                    newRootBone.name = newRootBone.name.Replace("(Clone)", "");

                    Transform[] newBones = new Transform[oriRenderer.bones.Length];
                    for (int j = 0; j < oriRenderer.bones.Length; j++)
                    {
                        Transform bone = oriRenderer.bones[j];
                        Transform newBone = SearchBone(bone.name, newRootBone);
                        newBones[j] = newBone;
                    }

                    Animator oriAnim = target.GetComponent<Animator>();
                    AnimatorStateInfo stateInfo = oriAnim.GetCurrentAnimatorStateInfo(0);

                    newRootBone.SetParent(newObj.transform);
                    Animator anim = newObj.AddComponent<Animator>();
                    anim.runtimeAnimatorController = oriAnim.runtimeAnimatorController;
                    anim.avatar = target.GetComponent<Animator>().avatar;
                    anim.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);

                    mesh.bindposes = cutterObject.Bindposes;
                    mesh.boneWeights = cuttedMeshes[i].BoneWeights;

                    obj = new GameObject("Split Object", new[] {
                        typeof(SkinnedMeshRenderer)
                    });
                    obj.transform.position = target.MeshTransform.position;
                    obj.transform.rotation = target.MeshTransform.rotation;

                    SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = mesh;
                    renderer.materials = mats;
                    renderer.bones = newBones;
                    renderer.rootBone = newRootBone;
                }
                else
                {
                    obj = new GameObject("Split Object " + i, new[] {
                        typeof(MeshFilter),
                        typeof(MeshRenderer)
                    });
                    obj.transform.position = target.MeshTransform.position;
                    obj.transform.rotation = target.MeshTransform.rotation;

                    obj.GetComponent<MeshFilter>().mesh = mesh;
                    obj.GetComponent<MeshRenderer>().materials = mats;
                }

                cuttedObject[i] = obj;
                obj.transform.SetParent(newObj.transform);

                if (cutterObject.NeedsScaleAdjust)
                {
                    obj.transform.localScale = target.transform != target.MeshTransform ? target.MeshTransform.localScale : Vector3.one;
                }
            }

            // Notify end of cutting.
            target.Cutted(cuttedObject);

            if (_autoDestroyTarget)
            {
                // Destroy target object.
                target.transform.localScale = Vector3.zero;
                Destroy(target.gameObject, 1f);
            }

            if (OnCutted != null)
            {
                OnCutted.Invoke(cuttedObject, info.CutterPlane);
            }

            if (info.CutterCallback != null)
            {
                info.CutterCallback.Invoke(true, cuttedObject, info.CutterPlane, info.UserData);
            }
        }

        /// <summary>
        /// カットが終わった際のコールバックハンドラ
        /// </summary>
        /// <param name="cuttedMeshes">切断されたメッシュ</param>
        private void OnCuttedHandler(CutterInfo info)
        {
            lock (_cuttedInfoList)
            {
                _cuttedInfoList.Add(info);
            }
        }

        /// <summary>
        /// Failed event handler.
        /// </summary>
        private void OnFailedHanlder(CutterInfo info)
        {
            lock (_failedCutInfoList)
            {
                _failedCutInfoList.Add(info);
            }
        }
    }
}
