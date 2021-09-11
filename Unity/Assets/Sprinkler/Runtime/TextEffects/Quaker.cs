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

        public override void Setup(ExpandableArray<CharAttribute> attrs, int idx, int blockIndex)
        {
            attrs[idx].Quake.Offset = GetRand() * 10.0f;
        }

        public void Modify(in CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
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
