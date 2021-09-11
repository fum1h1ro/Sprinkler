using System;

namespace Sprinkler.TextEffects
{
    public enum Type : sbyte
    {
        Normal,
        Quake,
        Shout,
    }

    [Flags]
    public enum TypeFlag : uint
    {
        //Normal = (1 << Type.Normal),
        Quake = (1 << Type.Quake),
        Shout = (1 << Type.Shout),
    }
}
