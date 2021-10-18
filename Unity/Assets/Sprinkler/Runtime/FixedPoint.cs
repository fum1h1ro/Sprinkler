using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    public static class FixedPoint
    {
        public static short ToFX8(this float v) => (short)(256 * v);
        public static short ToFX12(this float v) => (short)(1024 * v);
        public static float FromFX8(this short v) => (float)v * (1.0f/256.0f);
        public static float FromFX12(this short v) => (float)v * (1.0f/1024.0f);
    }
}
