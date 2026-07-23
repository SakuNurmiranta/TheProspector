using System;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.UI
{
    /// <summary>
    /// Converts authoritative discrete physical-map coordinates into
    /// normalized presentation positions within a map RectTransform.
    ///
    /// Domain coordinates remain Vector2Int. The resulting Vector2 is
    /// presentation data only.
    /// </summary>
    public static class PhysicalMapCoordinateProjection
    {
        public static Vector2 ToNormalizedPoint(
            Vector2Int coordinate)
        {
            if (!PhysicalMapGrid.IsCoordinateInBounds(
                    coordinate
                ))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coordinate),
                    coordinate,
                    "Cannot project a coordinate outside " +
                    "the physical map."
                );
            }

            float normalizedX =
                (coordinate.x + 0.5f) /
                PhysicalMapGrid.Width;

            float normalizedY =
                (coordinate.y + 0.5f) /
                PhysicalMapGrid.Height;

            return new Vector2(
                normalizedX,
                normalizedY
            );
        }
    }
}