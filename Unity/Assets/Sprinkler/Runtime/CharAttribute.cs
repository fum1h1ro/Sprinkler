using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    public struct CharAttribute
    {
        public TextEffects.TypeFlag AnimType;
        public float Time;

        public struct QuakeParam
        {
            public float Offset;
        }

        public struct ShoutParam
        {
        }

        public struct FadeParam
        {
        }

        public QuakeParam Quake;
        public ShoutParam Shout;
        public FadeParam Fade;
    }
}