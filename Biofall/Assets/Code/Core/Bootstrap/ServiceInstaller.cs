using UnityEngine;

namespace Biofall.Core
{
    // Biofall.Core may not reference Biofall.Net, so the composition root cannot construct a
    // network service directly. It knows only this abstraction, and each higher assembly
    // supplies a subclass that registers its own services. Dependencies keep pointing down.
    public abstract class ServiceInstaller : MonoBehaviour
    {
        public abstract int Order { get; }

        public abstract void Install();

        public virtual void Uninstall() { }
    }
}
