using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// Size of the room (m): one size for the visual room and the Meta XR Audio shoebox (RoomBuilder makes both). The room is
    /// centered on this object's position in x/z with the floor at its y. Lives on the visual room's root.
    /// </summary>
    public class ShoeboxRoom : MonoBehaviour
    {
        public float width = 8.72f;   // x
        public float depth = 15f;     // z
        public float height = 3.5f;   // y

        public double VolumeOverSurface => RoomMath.VolumeOverSurface(width, depth, height);

        public string Describe() => "room=" + GPL.Core.Fmt.F(width) + "x" + GPL.Core.Fmt.F(depth) + "x" + GPL.Core.Fmt.F(height);
    }
}
