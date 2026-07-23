using System;
using NUnit.Framework;
using UnityEngine;

namespace SEMM91.UI.Tests.Editor
{
    public class
        PhysicalMapCoordinateProjectionTests
    {
        private const float Tolerance =
            0.0001f;

        [Test]
        public void BottomLeftCoordinateMapsToFirstCellCenter()
        {
            Vector2 result =
                PhysicalMapCoordinateProjection
                    .ToNormalizedPoint(
                        new Vector2Int(0, 0)
                    );

            Assert.That(
                result.x,
                Is.EqualTo(0.05f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.y,
                Is.EqualTo(0.05f)
                    .Within(Tolerance)
            );
        }

        [Test]
        public void StartingCoordinateMapsDeterministically()
        {
            Vector2 result =
                PhysicalMapCoordinateProjection
                    .ToNormalizedPoint(
                        new Vector2Int(4, 4)
                    );

            Assert.That(
                result.x,
                Is.EqualTo(0.45f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.y,
                Is.EqualTo(0.45f)
                    .Within(Tolerance)
            );
        }

        [Test]
        public void TopRightCoordinateMapsToLastCellCenter()
        {
            Vector2 result =
                PhysicalMapCoordinateProjection
                    .ToNormalizedPoint(
                        new Vector2Int(9, 9)
                    );

            Assert.That(
                result.x,
                Is.EqualTo(0.95f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.y,
                Is.EqualTo(0.95f)
                    .Within(Tolerance)
            );
        }

        [Test]
        public void OutOfBoundsCoordinateIsRejected()
        {
            Assert.Throws<
                ArgumentOutOfRangeException
            >(
                () =>
                    PhysicalMapCoordinateProjection
                        .ToNormalizedPoint(
                            new Vector2Int(10, 0)
                        )
            );
        }
    }
}