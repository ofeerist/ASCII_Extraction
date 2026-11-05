using UnityEngine;

namespace Pawnshop.Endless
{
    public enum HouseStation { Door, Desk, RitualRug, Shelf, Supplier, Ledger, Item }
    public sealed class HouseInteractable : MonoBehaviour
    {
        public HouseStation station;
        public int index;
        public string title;
    }
    public sealed class InspectionRegion : MonoBehaviour { public int index; }
}
