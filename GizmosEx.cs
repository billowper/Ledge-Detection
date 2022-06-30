using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GizmosEx
{
    public static void DrawArrow(Vector3 start, Vector3 direction, float size = 0.2f, bool arrowsOnly = false, string label = null)
    {
        DrawArrow(start, direction, Color.white, size, arrowsOnly, label);
    }
        
    public static void DrawArrow(Vector3 start, Vector3 direction, Color color, float size = 0.2f, bool arrowsOnly = false, string label = null)
    {
        var gizmosColor = Gizmos.color;
        Gizmos.color = color;
            
        var end = start + direction;

        if (arrowsOnly == false)
        {
            Gizmos.DrawLine(start, end);
        }

        var arrowTerm = Vector3.Lerp(start, end, 1 - (size / (start - end).magnitude));
        var offsetVector = Quaternion.LookRotation(direction, Vector3.up) * Vector3.right;

        Gizmos.DrawLine(end, arrowTerm + (-offsetVector * size));
        Gizmos.DrawLine(end, arrowTerm + (offsetVector * size));

        if (label != null)
        {
            var color32 = (Color32) color;
            var colorHex = $"{color32.r:X2}{color32.g:X2}{color32.b:X2}";
            
#if UNITY_EDITOR
            Handles.Label(end + Vector3.up * .1f, $"<color={colorHex}><size=11>{label}</size></color>", new GUIStyle()
            {
                richText = true,
                alignment = TextAnchor.UpperCenter,
                clipping = TextClipping.Overflow,
                fixedHeight = 1, 
                fixedWidth = 1
            });
#endif
        }
            
        Gizmos.color = gizmosColor;
    }
    
    public static void DrawDisc(Vector3 center, Color color, float radius)
    {
        Handles.zTest = CompareFunction.Always;

        Handles.color = new Color(color.r, color.g, color.b, 0.05f);
        Handles.DrawSolidDisc(center, Vector3.up, radius);
        Handles.color = color;
        Handles.DrawWireDisc(center, Vector3.up, radius);
    }

}