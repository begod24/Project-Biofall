using Unity.Netcode;
using UnityEngine;

namespace Biofall.Net
{
    public sealed class SessionRoot : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (transform.parent != null)
            {
                Debug.LogError("[Session] The session object must be a scene root for " +
                               "DontDestroyOnLoad to apply.");
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
