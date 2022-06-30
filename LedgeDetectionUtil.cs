using System;
using UnityEngine;

[Serializable]
public class LedgeDetectionSettings
{
    public float MinLedgeWidth = 1f;
    public int MaxSurfaceRaycastSteps = 5;
    public float MaxSurfaceRaycastStepInterval = 2f;
    public float OverhangCheckHeight = 4f;
    public float MinDistanceToGround = 2f;
    public float ClearanceHeight = 4f;
    public float ObstructionCheckSize = .5f;
    public LayerMask GroundLayers;
}

public static class LedgeDetectionUtil
{
    private static readonly Collider[] m_overlaps = new Collider[32];

    public enum LedgeDetectionResults
    {
        FoundNoWall,
        FoundNoSurface_OverhandDistanceTooClose,
        FoundNoSurface_Obstructed,
        TooCloseToGround,
        FoundLedge,
        SurfaceObstructed_NoClearance
    }

    public static bool TryFindLedge(Ray ray, LedgeDetectionSettings settings, out Ledge ledge, out LedgeDetectionResults result, bool drawGizmos = false)
    {
        result = LedgeDetectionResults.FoundNoWall;

        // find a wall
        
        if (Physics.Raycast(
            ray: ray,
            hitInfo: out var wallHit,
            maxDistance: 10f,
            layerMask: settings.GroundLayers))
        {
            // get first overhang distance to clamp our steps?

            var overhangRay = new Ray(wallHit.point - (ray.direction * .2f), Vector3.up);
            var overhangDistance = Mathf.Infinity;

            if (Physics.Raycast(
                ray: overhangRay,
                out var overhangHit,
                maxDistance: settings.OverhangCheckHeight,
                layerMask: settings.GroundLayers))
            {
                overhangDistance = overhangHit.distance;
            }

            // step up from the wall hit until we find a surface we can stand on
            
            var foundSurface = false;
            Vector3 surfacePoint = default;

            var overlapPoint = wallHit.point + Vector3.up * settings.MaxSurfaceRaycastStepInterval;
            
            for (int i = 0; i < settings.MaxSurfaceRaycastSteps; i++)
            {
                overhangDistance -= settings.MaxSurfaceRaycastStepInterval;
                
                if (overhangDistance <= settings.MaxSurfaceRaycastStepInterval)
                {
                    result = LedgeDetectionResults.FoundNoSurface_OverhandDistanceTooClose;
                    break;
                }

                // check for obstruction before casting down to find surface below us

                if (Physics.CheckSphere(overlapPoint, .1f) == false)
                {
                    #if UNITY_EDITOR
                    if (drawGizmos)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(overlapPoint, .1f);
                    }
                    #endif

                    var surfaceRay = new Ray(overlapPoint + Vector3.up + -wallHit.normal.normalized * .5f, Vector3.down);
                    
                    if (Physics.Raycast(
                        ray: surfaceRay,
                        hitInfo: out var surfaceHit,
                        maxDistance: settings.MaxSurfaceRaycastStepInterval * 4f,
                        layerMask: settings.GroundLayers,
                        queryTriggerInteraction: QueryTriggerInteraction.Ignore))
                    {
                        result = LedgeDetectionResults.SurfaceObstructed_NoClearance;
                        
                        #if UNITY_EDITOR
                        if (drawGizmos)
                        {
                            GizmosEx.DrawDisc(surfaceHit.point, Color.green, .3f);
                            GizmosEx.DrawArrow(surfaceHit.point, Vector3.up, .1f);
                        }
                        #endif

                        // make sure we have clearance above us to stand up

                        var boxCenter = surfaceHit.point + // start at the point we found on the surface
                                        Vector3.up * settings.ClearanceHeight * .5f + // move up by half clearance height
                                        Vector3.up * .02f; // and some extra so we dont intersect floor

                        var boxSize = new Vector3(settings.ObstructionCheckSize, settings.ClearanceHeight, settings.ObstructionCheckSize);

                        var overlapHits = Physics.OverlapBoxNonAlloc(boxCenter, boxSize * .5f, m_overlaps);

                        #if UNITY_EDITOR
                        if (drawGizmos)
                        {
                            Gizmos.color = overlapHits == 0 ? Color.green : Color.red;
                            Gizmos.DrawWireCube(boxCenter, boxSize);
                        }
                        #endif

                        if (overlapHits == 0)
                        {
                            foundSurface = true;
                            surfacePoint = surfaceHit.point - ray.direction.normalized * .5f;
                            break;
                        }
                    }
                }
                else
                {
                    #if UNITY_EDITOR
                    if (drawGizmos)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(overlapPoint, .1f);
                    }
                    #endif
                    
                    result = LedgeDetectionResults.FoundNoSurface_Obstructed;
                }

                overlapPoint += Vector3.up * settings.MaxSurfaceRaycastStepInterval;
            }

            if (foundSurface)
            {
                // get distance from ground below the ledge
            
                var groundRay = new Ray(surfacePoint + (wallHit.normal.normalized * .7f), Vector3.down);
                var groundDistance = Mathf.Infinity;

                if (Physics.Raycast(
                    ray: groundRay,
                    out var groundHit,
                    maxDistance: Mathf.Infinity,
                    layerMask: settings.GroundLayers))
                {
                    groundDistance = groundHit.distance;
                    
                    #if UNITY_EDITOR
                    if (drawGizmos)
                    {
                        GizmosEx.DrawArrow(groundRay.origin, groundRay.direction * groundHit.distance, Color.yellow);
                    }
                    #endif
                }
            
                // this isn't a valid ledge if its close to the ground (we'll just step/jump up it)

                if (groundDistance < settings.MinDistanceToGround)
                {
                    ledge = default;
                    result = LedgeDetectionResults.TooCloseToGround;
                    return false;
                }
                
                #if UNITY_EDITOR
                if (drawGizmos)
                {
                    GizmosEx.DrawArrow(ray.origin, ray.direction * wallHit.distance, Color.green);
                    GizmosEx.DrawArrow(surfacePoint + Vector3.up * settings.ClearanceHeight, Vector3.down * settings.ClearanceHeight, Color.green);
                }
                #endif

                // define ledge as a start/end point and normal from the wall
                
                var cross = Vector3.Cross(wallHit.normal, Vector3.up);
                var ledgeStart = surfacePoint + -cross * settings.MinLedgeWidth * .5f;
                var ledgeEnd = surfacePoint + cross * settings.MinLedgeWidth * .5f;

                ledge = new Ledge()
                {
                    Start = ledgeStart,
                    End = ledgeEnd,
                    Normal = wallHit.normal,
                    DistanceFromGround = groundDistance
                };

                if (drawGizmos)
                {
                    ledge.DrawGizmos(Color.red);
                }

                result = LedgeDetectionResults.FoundLedge;
                return true;
            }

            #if UNITY_EDITOR
            if (drawGizmos)
            {
                GizmosEx.DrawArrow(ray.origin, ray.direction * wallHit.distance, Color.red);
            }
            #endif
        }

        ledge = default;
        return false;
    }
}
