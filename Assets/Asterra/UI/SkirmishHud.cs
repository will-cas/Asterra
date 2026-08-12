using UnityEngine;

namespace Asterra.UI
{
    /// <summary>Local-only HUD shell for the skirmish slice.</summary>
    public sealed class SkirmishHud : MonoBehaviour
    {
        [SerializeField] private string lastResourceLine;

        public void SetResources(int gold, int timber, int mana)
        {
            lastResourceLine = $"Gold {gold}  Timber {timber}  Mana {mana}";
        }

        public string LastResourceLine => lastResourceLine;
    }
}
