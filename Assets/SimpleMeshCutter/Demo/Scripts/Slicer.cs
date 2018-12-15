using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter.Demo
{
    /// <summary>
    /// Slicer (multi cut demo)
    /// </summary>
    [RequireComponent(typeof(CutterTarget))]
    public class Slicer : MonoBehaviour
    {
        #region ### Events ###
        public System.Action OnCutted;
        #endregion ### Events ###

        [SerializeField]
        private Transform _root;
        public Transform Root
        {
            get {
                if (_root == null)
                {
                    return transform;
                }
                return _root;
            }
            set { _root = value; }
        }

        private bool _canCut = true;
        public bool CanCut
        {
            get
            {
                if (WarmupTime > 0)
                {
                    return false;
                }
                return _canCut;
            }
            set { _canCut = value; }
        }

        private bool _isCutting = false;
        public bool Iscutting
        {
            get { return _isCutting; }
            set { _isCutting = value; }
        }

        private float _warmupTime = 0.1f;
        public float WarmupTime
        {
            get { return _warmupTime; }
            set { _warmupTime = value; }
        }

        public CutterTarget CutTarget { get; set; }

        private Vector3 _velocity = Vector3.zero;
        private Rigidbody _rigidbody;

        private SlicerCutSequencer _sequencer;
        public SlicerCutSequencer Sequencer
        {
            get { return _sequencer; }
            set { _sequencer = value; }
        }

        private bool _isCutted = false;
        public bool IsCutted
        {
            get { return _isCutted; }
        }

        #region ### MonoBehaviour ###
        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            CutTarget = GetComponent<CutterTarget>();
        }

        private void Update()
        {
            if (WarmupTime <= 0)
            {
                return;
            }

            WarmupTime -= Time.deltaTime;
        }
        #endregion ### MonoBehaviour ###

        public void Cutted()
        {
            _isCutted = true;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }

            if (OnCutted != null)
            {
                OnCutted.Invoke();
            }
        }

        /// <summary>
        /// 切断時の演出速度を子に伝播させる
        /// </summary>
        /// <param name="child"></param>
        public void Inherit(Slicer child, Vector3 normal)
        {
            child._velocity = _velocity + normal;
        }

        /// <summary>
        /// Move victim in cutting.
        /// </summary>
        public void MoveForCutting(float deltaTime)
        {
            transform.position += _velocity * deltaTime;
        }
    }
}
