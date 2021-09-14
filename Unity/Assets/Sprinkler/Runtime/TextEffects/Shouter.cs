using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Shouter : EffectorBase, IVertexModifier
    {
        public void Modify(in CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
        {
            var scale = (float)attr.Shout.Scale / 256.0f - 1.0f;
            var growSpeed = (float)attr.Shout.GrowSpeed / 256.0f;
            var shrinkSpeed = (attr.Shout.ShrinkSpeed <= 0)? 1.0f / 256.0f : (float)attr.Shout.ShrinkSpeed / 256.0f;
            //Debug.Log($"{growSpeed} {shrinkSpeed}");
            var total = growSpeed + shrinkSpeed;
            var t = attr.Time;// * _speed;
            var b = (t <= total)? t / total : 1.0f;
            var w =
                (t <= growSpeed)? Mathf.Sin((t / growSpeed) * Mathf.PI * 0.5f) :
                    (t <= total)? Mathf.Sin(Mathf.PI * 0.5f + ((t - growSpeed) / shrinkSpeed) * Mathf.PI * 0.5f) : 0.0f;

            var center = (vtx[vtxtop + 0] + vtx[vtxtop + 1] + vtx[vtxtop + 2] + vtx[vtxtop + 3]) * 0.25f;

            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] = center + (vtx[vtxtop+i] - center) * (b + w * scale);
        }
    }
}
