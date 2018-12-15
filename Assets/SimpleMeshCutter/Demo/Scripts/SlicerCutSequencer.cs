using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter.Demo
{
    public class SlicerCutSequenceArgs
    {
        public float LifeTime;
        public float Speed;
        public Material OverrideMaterial;
        public GameObject EffectPrefab;
    }

    /// <summary>
    /// Victimのカット演出をコントロールする
    /// </summary>
    public class SlicerCutSequencer
    {
        public System.Action<SlicerCutSequencer> OnEnded; 

        private Material _overrideMaterial;
        private GameObject _effectPrefab;

        private List<Slicer> _slicerList = new List<Slicer>();
        private List<GameObject> _effectList = new List<GameObject>();
        private float _lifeTime = 1.58f;
        private float _speed = 0.1f;

        private float _time = 0;
        private bool _hasEnded = false;
        private bool _isDestructioning = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SlicerCutSequencer(SlicerCutSequenceArgs args)
        {
            Initialize(args);
        }

        private void Initialize(SlicerCutSequenceArgs args)
        {
            _lifeTime = args.LifeTime;
            _speed = args.Speed;
            _overrideMaterial = args.OverrideMaterial;
            _effectPrefab = args.EffectPrefab;
        }

        /// <summary>
        /// Dispose処理
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _effectList.Count; i++)
            {
                GameObject.Destroy(_effectList[i]);
            }

            _hasEnded = true;

            if (OnEnded != null)
            {
                OnEnded.Invoke(this);
            }
        }

        /// <summary>
        /// Add Victime component to sub objects.
        /// </summary>
        public void Add(GameObject[] cuttedObjects, MeshCutter.CutterPlane blade, Slicer parent)
        {
            // カット後の移動方向を調べるために平面を作成する
            Vector3 position = cuttedObjects[0].transform.TransformPoint(blade.Position);
            Vector3 normal = cuttedObjects[0].transform.TransformDirection(blade.Normal);
            Plane plane = new Plane(normal, position);

            Vector3 center = Vector3.zero;
            for (int i = 0; i < cuttedObjects.Length; i++)
            {
                GameObject cutObj = cuttedObjects[i];

                CutterTarget cutterTarget = cutObj.AddComponent<CutterTarget>();
                cutterTarget.Mesh = cutObj.transform;

                Slicer victim = cutObj.AddComponent<Slicer>();
                cutObj.AddComponent<BoxCollider>();
                victim.Sequencer = this;
                victim.CutTarget = cutterTarget;
                victim.Root = cutObj.transform.parent;
                _slicerList.Add(victim);

                Renderer renderer = cutObj.GetComponent<Renderer>();
                Vector3 checkPos = renderer.bounds.center;
                center += checkPos;
                bool side = plane.GetSide(checkPos);
                if (side)
                {
                    parent.Inherit(victim, plane.normal * _speed);
                }
                else
                {
                    parent.Inherit(victim, -plane.normal * _speed);
                }
            }

            // オブジェクトの中心にエフェクトを表示
            center /= cuttedObjects.Length;

            GameObject effect = GameObject.Instantiate(_effectPrefab, center, Quaternion.identity);
            effect.transform.forward = normal;
            _effectList.Add(effect);
        }

        /// <summary>
        /// Remove destroyed victime.
        /// </summary>
        /// <param name="victim">Remove target.</param>
        public void Remove(Slicer victim)
        {
            if (_slicerList.Contains(victim))
            {
                _slicerList.Remove(victim);
            }
        }

        /// <summary>
        /// ポリゴン分解処理
        /// </summary>
        private void Destruction()
        {
            Material overrideMaterial = GameObject.Instantiate(_overrideMaterial);
            for (int i = 0; i < _slicerList.Count; i++)
            {
                _slicerList[i].CanCut = false;

                Renderer renderer = _slicerList[i].GetComponent<Renderer>();
                Material[] mats = renderer.materials;
                Material[] newMats = new Material[mats.Length];
                for (int j = 0; j < newMats.Length; j++)
                {
                    newMats[j] = overrideMaterial;
                }
                renderer.materials = newMats;
            }

            EnumeratorExecuteManager.Push(StartDestruction(overrideMaterial));
        }

        /// <summary>
        /// Start destruction animation.
        /// </summary>
        /// <param name="overrideMaterial">Override material for destruction.</param>
        /// <returns>IEnumerator.</returns>
        private IEnumerator StartDestruction(Material overrideMaterial)
        {
            float duration = 2.5f;
            yield return DestructionImpl(duration, overrideMaterial);

            // 時間経過したらオブジェクトを破棄する
            for (int i = 0; i < _slicerList.Count; i++)
            {
                GameObject.Destroy(_slicerList[i].transform.parent.gameObject);
            }

            Dispose();
        }

        /// <summary>
        /// Destruction animation impl.
        /// </summary>
        /// <param name="duration">Duration time.</param>
        /// <param name="overrideMaterial">Destruction animation material.</param>
        /// <returns>IEnumerator.</returns>
        private IEnumerator DestructionImpl(float duration, Material overrideMaterial)
        {
            float time = 0;
            while (true)
            {
                time += Time.deltaTime;
                if (time >= duration)
                {
                    overrideMaterial.SetFloat("_Destruction", 1f);
                    break;
                }

                float t = time / duration;
                t = t * t;

                overrideMaterial.SetFloat("_Destruction", t);

                yield return null;
            }
        }

        /// <summary>
        /// ノイズマスク用オブジェクトを生成する
        /// </summary>
        /// <param name="target">生成するターゲット</param>
        /// <param name="noiseMask">ノイズマスク用マテリアル</param>
        /// <returns>生成したオブジェクト</returns>
        private GameObject CreateNoiseObject(GameObject target, Material noiseMask)
        {
            GameObject result = GameObject.Instantiate(target);
            result.transform.SetParent(target.transform, false);
            result.transform.localPosition = Vector3.zero;
            result.transform.rotation = target.transform.rotation;
            result.GetComponent<MeshRenderer>().materials = new[] { noiseMask };

            return result;
        }

        /// <summary>
        /// Update victim cutting animation.
        /// </summary>
        /// <param name="deltaTime">Delta time.</param>
        public void UpdateVictimCutting(float deltaTime)
        {
            if (_isDestructioning)
            {
                return;
            }

            if (_hasEnded)
            {
                return;
            }

            for (int i = 0; i < _slicerList.Count; i++)
            {
                _slicerList[i].MoveForCutting(deltaTime);
            }

            _time += deltaTime;

            if (_time >= _lifeTime)
            {
                Destruction();
                _isDestructioning = true;
            }
        }
    }
}
