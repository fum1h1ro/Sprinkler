using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public class EffectorBase
    {
        public virtual void Setup(ExpandableArray<CharAttribute>.Span attrs, int idx, int blockIndex) {}
        public virtual void Update(ExpandableArray<CharAttribute>.Span attrs, int idx) {}
    }

    public interface IVertexModifier
    {
        void Modify(in CharAttribute attr, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop);
    }

    public interface IColorModifier
    {
        void Modify(in CharAttribute attr, TMP_CharacterInfo info, Color32[] col, int coltop);
    }
}
