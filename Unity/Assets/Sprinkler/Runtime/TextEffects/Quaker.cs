using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Quaker : IVertexModifier
    {
        private float _speed = 1.0f/0.7f;
        private int _seed = 200;
        private const int A = 1664525;
        private const int C = 1013904223;
        private const int M = 0x7fffffff;

        public void Setup(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx)
        {
            var p = parameters[idx];
            p.Time = GetRand() * 10.0f;
            parameters[idx] = p;
        }

        public void Update(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx)
        {
            var p = parameters[idx];
            p.Time += Mathf.PI * 2.0f * Time.deltaTime * _speed;

            if (p.Time >= Mathf.PI * 2.0f)
            {
                p.Time -= Mathf.PI * 2.0f;
            }
            parameters[idx] = p;
        }

        public void Modify(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var p = parameters[idx];
            var w = Mathf.Sin(p.Time) + 0.5f;
            var o = (w < 0.0f)? Vector3.zero : new Vector3(GetRand(), GetRand(), 0);
            var sz = info.pointSize * 0.1f;
            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] += o * sz;
        }

        private float GetRand()
        {
            _seed = (_seed * A + C) & M;
            var r = _seed * (1.0f / (float)M);
            return (r - 0.5f) * 2.0f;
        }
    }

    public class Shouter : IVertexModifier
    {
        private float _speed = 1.0f/0.7f;

        public void Setup(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx)
        {
            var p = parameters[idx];
            p.Time = 0.0f;
            parameters[idx] = p;
        }

        public void Update(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx)
        {
            var p = parameters[idx];
            p.Time += Mathf.PI * 2.0f * Time.deltaTime * _speed;
            parameters[idx] = p;
        }

        public void Modify(ExpandableArray<TextProcessor.CharAttribute> parameters, int idx, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var p = parameters[idx];
            var b = (p.Time <= Mathf.PI)? p.Time / Mathf.PI : 1.0f;
            var w = (p.Time <= Mathf.PI)? Mathf.Sin(p.Time) : 0.0f;

            var center = (vtx[vtxtop + 0] + vtx[vtxtop + 1] + vtx[vtxtop + 2] + vtx[vtxtop + 3]) * 0.25f;

            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] = center + (vtx[vtxtop+i] - center) * (b + w);
        }
    }
}
