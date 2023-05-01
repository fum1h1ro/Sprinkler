using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Quaker : EffectorBase, IVertexModifier
    {
        private float _speed = (Mathf.PI * 2.0f) / 10f;
        private uint _seed = 200;

        public override void Setup(ExpandableArray<CharAttribute>.Span attrs, int idx, int blockIndex)
        {
            attrs[idx].Quake.Offset = GetRand() * 10.0f;
        }

        public void Modify(in CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var h = info.pointSize * attr.Quake.Horizontal.FromFX8();
            var v = info.pointSize * attr.Quake.Vertical.FromFX8();

            var w = Mathf.Sin((attr.Time + attr.Quake.Offset) * _speed);
            var o = (w < 0.0f)? Vector3.zero : new Vector3(GetRand(5) * h, GetRand(5) * v, 0);
            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] += o;
        }

        private float GetRand(int repeat = 1)
        {
            float r = 0f;
            for (var i = 0; i < repeat; ++i)
            {
                r += PcgRandom(ref _seed);
            }
            r /= (float)repeat;
            return (r - 0.5f) * 2.0f;
        }

        uint PcgHash(ref uint seed)
        {
            uint state = seed * 747796405u + 2891336453u;
            uint word = ((state >> (int)((state >> 28) + 4)) ^ state) * 277803737u;
            return (word >> 22) ^ word;
        }

        float PcgRandom(ref uint seed)
        {
            seed = PcgHash(ref seed);
            return (float)(seed >> 9) * (1.0f / 8388608.0f);
        }
    }
}
