using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter.Demo
{
    public class BladeController : MonoBehaviour
    {
        [SerializeField]
        private float _delay = 1f;

        [SerializeField]
        private Vector3 _rotation;

        [SerializeField]
        private float _duration = 1f;

        private float _time;
        private Vector3 _initRot;

        private bool _isStarted = false;

        private IEnumerator Start()
        {
            _initRot = transform.rotation.eulerAngles;

            yield return new WaitForSeconds(_delay);

            _isStarted = true;
        }

        private void Update()
        {
            if (!_isStarted)
            {
                return;
            }

            if (_time >= _duration)
            {
                transform.rotation = Quaternion.Euler(_initRot + _rotation);
                return;
            }

            _time += Time.deltaTime;

            Vector3 rot = _initRot + (_rotation * (_time / _duration));
            transform.rotation = Quaternion.Euler(rot);
        }
    }
}
