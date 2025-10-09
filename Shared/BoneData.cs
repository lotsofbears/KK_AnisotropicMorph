using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AniMorph
{
    internal readonly struct BoneData
    {
        internal BoneData(BoneModifier boneModifier)
        {
            this.boneModifier = boneModifier;
            this.boneModifierData = new();
        }
        internal BoneData(BoneModifier boneModifier, BoneModifierData boneModifierData)
        {
            this.boneModifier = boneModifier;
            this.boneModifierData = boneModifierData;
        }
        internal readonly BoneModifier boneModifier;
        internal readonly BoneModifierData boneModifierData;

    }
}
