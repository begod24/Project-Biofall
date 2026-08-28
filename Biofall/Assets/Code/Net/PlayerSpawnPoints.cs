using UnityEngine;
using UnityEngine.AI;

namespace Biofall.Net
{
    // Where a co-op body enters the run. This lives in the network assembly because the spawner
    // needs it before any player object exists, and it deliberately knows nothing about gameplay:
    // it reads the scene and the NavMesh, and hands back a point.
    public static class PlayerSpawnPoints
    {
        private const string MissionSpawnAnchorName = "CoopSpawnPoint";
        private const string MissionFloorName = "Plane";
        private const float GroundProbeHeight = 12f;
        private const float GroundProbeDistance = 40f;
        private const float HeightPadding = 0.05f;

        private static readonly Vector3 MissionFallback = new(-2.1622f, 0.05f, 19.6f);

        private static readonly Vector3[] Offsets =
        {
            Vector3.zero,
            new Vector3(2f, 0f, 0f),
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, 0f, 2f)
        };

        public static Vector3 ResolveMission(ulong ownerClientId)
        {
            Vector3 position = MissionFallback;
            Vector3 offset = Offsets[(int)(ownerClientId % (ulong)Offsets.Length)];

            GameObject anchor = GameObject.Find(MissionSpawnAnchorName);
            if (anchor != null)
            {
                position = anchor.transform.position;
            }
            else
            {
                GameObject floor = GameObject.Find(MissionFloorName);
                if (floor != null && floor.TryGetComponent(out Renderer renderer))
                {
                    Bounds bounds = renderer.bounds;
                    position = new Vector3(bounds.center.x, bounds.max.y + HeightPadding, bounds.center.z);
                }
            }

            position += offset;

            if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
                position = navHit.position + Vector3.up * HeightPadding;

            Vector3 rayOrigin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y + HeightPadding;

            return position;
        }
    }
}
