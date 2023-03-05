using UnityEngine;

namespace MoreGeneInfo;

internal static class Util {

    public static void StepX(this ref Rect rect, float margin)
        => rect.x += rect.width + margin;

    public static void StepY(this ref Rect rect, float margin)
        => rect.y += rect.height + margin;
}