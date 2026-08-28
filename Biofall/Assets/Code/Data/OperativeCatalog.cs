using UnityEngine;

namespace Biofall.Data
{
    [CreateAssetMenu(menuName = "Biofall/Operative Catalog", fileName = "OperativeCatalog")]
    public sealed class OperativeCatalog : ScriptableObject
    {
        [SerializeField] private OperativeData[] operatives;

        public OperativeData[] Operatives => operatives;

        public OperativeData Find(string id)
        {
            if (operatives == null || string.IsNullOrEmpty(id)) return null;

            foreach (var op in operatives)
                if (op != null && op.id == id)
                    return op;

            return null;
        }
    }
}
