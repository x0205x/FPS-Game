using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Marks a thruster mount and its local thrust direction for VFX.
    /// </summary>
    public class AircraftThrusterPoint : MonoBehaviour
    {
        public enum ThrusterKind
        {
            Main,
            Maneuver
        }

        [SerializeField] private ThrusterKind kind = ThrusterKind.Maneuver;
        [SerializeField] private Vector3 localThrustDirection = Vector3.back;

        public ThrusterKind Kind => kind;
        public Vector3 LocalThrustDirection => localThrustDirection.sqrMagnitude > 0.001f
            ? localThrustDirection.normalized
            : Vector3.back;
    }
}
