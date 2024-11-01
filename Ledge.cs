using UnityEngine;

public struct Ledge
{
    /// <summary>
    /// faces out from the ledge towards player
    /// </summary>
    public Vector3 Normal { get; set; }
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float DistanceFromGround { get; set; }
    public Vector3 MidPoint => Vector3.Lerp(Start, End, .5f);
    
    public bool IsValid => Start != Vector3.zero && End != Vector3.zero;
    
    public void DrawGizmos(Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(Start, End);
                
        GizmosEx.DrawArrow(Start, -Normal, color);
        GizmosEx.DrawArrow(MidPoint, -Normal, color);
        GizmosEx.DrawArrow(End, -Normal, color);
        
        Gizmos.matrix = Matrix4x4.TRS(MidPoint, Quaternion.LookRotation(End - Start, Vector3.up), Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3( .1f, .1f, (End - Start).magnitude));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
