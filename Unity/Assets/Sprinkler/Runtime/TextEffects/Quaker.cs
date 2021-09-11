using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Quaker : EffectorBase, IVertexModifier
    {
        private float _speed = (Mathf.PI * 2.0f) / 0.7f;
        private int _seed = 200;
        private const int A = 1664525;
        private const int C = 1013904223;
        private const int M = 0x7fffffff;

        public override void Setup(ExpandableArray<TextProcessor.CharAttribute> attrs, int idx, int blockIndex)
        {
            var a = attrs[idx];
            a.Quake.Offset = GetRand() * 10.0f;
            attrs[idx] = a;
        }

        public override void Update(ExpandableArray<TextProcessor.CharAttribute> attrs, int idx)
        {
            //var p = attrs[idx];
            //p.Time += Mathf.PI * 2.0f * Time.deltaTime * _speed;

            //if (p.Time >= Mathf.PI * 2.0f)
            //{
            //    p.Time -= Mathf.PI * 2.0f;
            //}
            //attrs[idx] = p;
        }

        public void Modify(TextProcessor.CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var w = Mathf.Sin((attr.Time + attr.Quake.Offset) * _speed) + 0.5f;
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
}
