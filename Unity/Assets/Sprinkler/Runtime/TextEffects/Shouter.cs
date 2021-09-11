using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Shouter : EffectorBase, IVertexModifier
    {
        private float _speed = Mathf.PI / 0.7f;

        public void Modify(in CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var t = attr.Time * _speed;
            var b = (t <= Mathf.PI)? t / Mathf.PI : 1.0f;
            var w = (t <= Mathf.PI)? Mathf.Sin(t) : 0.0f;

            var center = (vtx[vtxtop + 0] + vtx[vtxtop + 1] + vtx[vtxtop + 2] + vtx[vtxtop + 3]) * 0.25f;

            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] = center + (vtx[vtxtop+i] - center) * (b + w);
        }
    }
}
