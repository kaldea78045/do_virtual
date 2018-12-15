using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter.Demo
{
    public class Blade : MonoBehaviour
    {
        #region ### Events ###
        public System.Action<GameObject> OnCutted;
        #endregion ### Events ###

        [SerializeField]
        private Material _overrideMaterial;

        [SerializeField]
        private float _cutSpeed = 4f;

        [SerializeField]
        private GameObject _effectPrefab;

        [SerializeField]
        private float _effectLife = 4f;

        private MeshCutter.Cutter _cutter;
        private Vector3 _previousPos;
        private AudioSource _audioSource;

        private Vector3 _velocity;
        public Vector3 Velocity
        {
            get { return _velocity; }
        }

        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }
                return _rigidbody;
            }
        }


        // for cutting slicer sequencer list.
        private List<SlicerCutSequencer> _slicerSequencerList = new List<SlicerCutSequencer>();

        /// <summary>
        /// 剣が対象を切れる状態かどうかをチェックする
        /// </summary>
        public bool CanCut
        {
            get
            {
                if (_velocity.sqrMagnitude < _cutSpeed)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 速度から、カット平面の法線を計算する
        /// </summary>
        private Vector3 CutNormal
        {
            get
            {
                Vector3 direction = _velocity.normalized;
                Vector3 normal = Vector3.Cross(direction, transform.forward);

                return normal;
            }
        }

        #region ### MonoBehaviour ###
        private void Start()
        {
            _previousPos = transform.position;
            _audioSource = GetComponent<AudioSource>();
            _cutter = GetComponent<MeshCutter.Cutter>();
            _cutter.AutoDestroyTarget = false;
        }

        private void Update()
        {
            SampleVelocity();
            UpdateVictimCutting();
        }

        private void OnTriggerEnter(Collider other)
        {
            Vector3 position = other.transform.position;
            TryCut(other.gameObject, position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 position = collision.contacts[0].point;
            TryCut(collision.gameObject, position);
        }
        #endregion ### MonoBehaviour ###

        /// <summary>
        /// Update cutting slicer movement.
        /// </summary>
        private void UpdateVictimCutting()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _slicerSequencerList.Count; i++)
            {
                _slicerSequencerList[i].UpdateVictimCutting(deltaTime);
            }
        }

        /// <summary>
        /// 位置をサンプリングして速度を計算する
        /// </summary>
        private void SampleVelocity()
        {
            _velocity = (transform.position - _previousPos) / Time.deltaTime;
            _previousPos = transform.position;
        }

        /// <summary>
        /// 切断できる対象かをチェックする
        /// </summary>
        /// <param name="slicer">対象オブジェクト</param>
        /// <returns>カットできる場合はtrue</returns>
        private bool CanCutTarget(Slicer slicer)
        {
            if (slicer == null)
            {
                return false;
            }

            if (!slicer.CanCut)
            {
                return false;
            }

            if (slicer.Iscutting)
            {
                return false;
            }

            if (!CanCut)
            {
                return false;
            }

            if (slicer.CutTarget == null)
            {
                return false;
            }

            if (!slicer.gameObject.activeInHierarchy)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 切断を試す
        /// 
        /// Victimeを持っていなかったり、速度が足りてない場合などはカットできないケースがある
        /// </summary>
        /// <param name="target"></param>
        private void TryCut(GameObject target, Vector3 position)
        {
            Slicer slicer = target.GetComponentInParent<Slicer>();
            if (!CanCutTarget(slicer))
            {
                return;
            }

            slicer.Iscutting = true;

            _cutter.Cut(slicer.CutTarget, position, CutNormal, OnCuttedHandler, slicer);

            if (OnCutted != null)
            {
                OnCutted.Invoke(target);
            }
        }

        /// <summary>
        /// 切断完了時のハンドラ
        /// </summary>
        /// <param name="cuttedObjects">2つに分割されたオブジェクト</param>
        /// <param name="plane">カット平面</param>
        /// <param name="userdata">ユーザデータ（Victimを想定）</param>
        private void OnCuttedHandler(bool success, GameObject[] cuttedObjects, MeshCutter.CutterPlane plane, object userdata)
        {
            Slicer slicer = userdata as Slicer;
            if (slicer == null)
            {
                return;
            }

            // If cutting don't succes, continue to be able to cut.
            if (!success)
            {
                slicer.Iscutting = false;
                return;
            }

            slicer.Cutted();

            UpdateSequencer(slicer, cuttedObjects, plane);
        }

        /// <summary>
        /// Update sequencer.
        /// </summary>
        /// <param name="slicer">Target slicer.</param>
        /// <param name="cuttedObjects">Separated objects.</param>
        /// <param name="plane">Separating plane.</param>
        private void UpdateSequencer(Slicer slicer, GameObject[] cuttedObjects, MeshCutter.CutterPlane plane)
        {
            if (slicer.Sequencer == null)
            {
                SlicerCutSequenceArgs args = new SlicerCutSequenceArgs
                {
                    LifeTime = 3f,
                    Speed = 0.1f,
                    OverrideMaterial = _overrideMaterial,
                    EffectPrefab = _effectPrefab,
                };

                SlicerCutSequencer sequencer = new SlicerCutSequencer(args);
                sequencer.Add(cuttedObjects, plane, slicer);
                sequencer.OnEnded += OnEndedHandler;
                _slicerSequencerList.Add(sequencer);
            }
            else
            {
                SlicerCutSequencer sequencer = slicer.Sequencer;
                sequencer.Add(cuttedObjects, plane, slicer);
                sequencer.Remove(slicer);
            }

            Destroy(slicer.Root.gameObject);
        }

        private void OnEndedHandler(SlicerCutSequencer sequencer)
        {
            if (_slicerSequencerList.Contains(sequencer))
            {
                _slicerSequencerList.Remove(sequencer);
            }
        }
    }
}
