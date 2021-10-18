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
            var scale = attr.Shout.Scale.FromFX8() - 1.0f;
            var growSpeed = attr.Shout.GrowSpeed.FromFX8();
            var shrinkSpeed = attr.Shout.ShrinkSpeed.FromFX8();
            var total = growSpeed + shrinkSpeed;
            var t = attr.Time;
            var b = (t <= total)? t / total : 1.0f;
            var w =
                (t <= growSpeed)? Mathf.Sin((t / growSpeed) * Mathf.PI * 0.5f) :
                    (t <= total)? Mathf.Sin(Mathf.PI * 0.5f + ((t - growSpeed) / shrinkSpeed) * Mathf.PI * 0.5f) : 0.0f;

            var center = (vtx[vtxtop + 0] + vtx[vtxtop + 1] + vtx[vtxtop + 2] + vtx[vtxtop + 3]) * 0.25f;

            for (var i = 0; i < 4; ++i) vtx[vtxtop+i] = center + (vtx[vtxtop+i] - center) * (b + w * scale);
        }
    }
}
