using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Sprinkler.TextEffects
{
    public interface IVertexModifier
    {
        void Setup(ExpandableArray<TextProcessor.CharAttribute> param, int idx);
        void Update(ExpandableArray<TextProcessor.CharAttribute> param, int idx);
        void Modify(ExpandableArray<TextProcessor.CharAttribute> param, int idx, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop);
    }
}
