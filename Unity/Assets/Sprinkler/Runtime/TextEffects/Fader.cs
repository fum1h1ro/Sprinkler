using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class Fader : EffectorBase, IColorModifier
    {
        private const float Span = 0.7f;
        private float _speed = 1.0f / Span;

        public void Modify(in CharAttribute attr, TMP_CharacterInfo info, Color32[] col, int coltop)
        {
            var b = Mathf.Clamp(attr.Time * _speed, 0, 1);
            var t = Mathf.Clamp((attr.Time - 0.2f) * _speed, 0, 1);
            col[coltop+0].a = (byte)(b * 255);
            col[coltop+1].a = (byte)(t * 255);
            col[coltop+2].a = (byte)(t * 255);
            col[coltop+3].a = (byte)(b * 255);
        }
    }
}
