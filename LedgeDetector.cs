using UnityEngine;

/// <summary>
/// test component for ledge detecting in a scene
/// </summary>
[SelectionBase]
public class LedgeDetector : MonoBehaviour
{
    [SerializeField] private LedgeDetectionSettings m_settings;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var ray = new Ray(transform.position + Vector3.up, transform.forward);
        
        LedgeDetectionUtil.TryFindLedge(ray, m_settings, out _, out var result, true);
        
        UnityEditor.Handles.Label(transform.position, $"{result}");
    }
#endif
}
