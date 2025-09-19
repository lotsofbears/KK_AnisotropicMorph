using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace KineticShift
{
    internal readonly struct BoneData(BoneModifier boneModifier)
    {
        internal readonly BoneModifier boneModifier = boneModifier;
        internal readonly BoneModifierData boneModifierData = new();

    }
}
