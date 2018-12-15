using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter
{
    /// <summary>
    /// メッシュカットに利用する平面の定義と
    /// 頂点位置判定などのメソッドを実装
    /// </summary>
    public class CutterPlane
    {
        // 平面の位置
        private Vector3 _position;
        public Vector3 Position
        {
            get { return _position; }
        }

        // 平面の法線
        private Vector3 _normal;
        public Vector3 Normal
        {
            get { return _normal; }
        }

        // コンストラクタ
        public CutterPlane(Vector3 position, Vector3 normal)
        {
            _position = position;
            _normal = normal.normalized;
        }

        /// <summary>
        /// 平面へのレイキャスト
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool Raycast(Ray ray, out float distance)
        {
            Vector3 n = _normal;
            Vector3 x = _position;
            Vector3 x0 = ray.origin;
            Vector3 m = ray.direction;
            float h = Vector3.Dot(n, x);

            Vector3 intersectPoint = x0 + ((h - Vector3.Dot(n, x0)) / (Vector3.Dot(n, m))) * m;

            distance = Vector3.Distance(intersectPoint, ray.origin);

            return false;
        }

        /// <summary>
        /// 与えられた点が平面の左右どちらにあるかを判断する
        /// </summary>
        /// <param name="point">チェックしたい点</param>
        /// <returns>面の表側にある場合はtrue</returns>
        public bool GetSide(Vector3 point)
        {
            Vector3 delta = (point - _position).normalized;
            float dot = Vector3.Dot(_normal, delta);
            if (dot == 0)
            {
                return false;
            }
            if (dot < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
