using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    /// <summary>
    /// Applies an ordered server snapshot to a replicated
    /// NetworkList without clearing and retransmitting rows
    /// that are already identical.
    /// </summary>
    public static class NetworkListSnapshotSynchronizer
    {
        public static int Synchronize<T>(
            NetworkList<T> destination,
            IReadOnlyList<T> source)
            where T : unmanaged, IEquatable<T>
        {
            if (destination == null)
            {
                throw new ArgumentNullException(
                    nameof(destination)
                );
            }

            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source)
                );
            }

            if (source.Count == 0)
            {
                if (destination.Count == 0)
                {
                    return 0;
                }

                destination.Clear();
                return 1;
            }

            int mutationCount =
                0;

            int sharedCount =
                Math.Min(
                    destination.Count,
                    source.Count
                );

            for (int index = 0;
                 index < sharedCount;
                 index++)
            {
                T next =
                    source[index];

                if (destination[index].Equals(next))
                {
                    continue;
                }

                destination[index] =
                    next;

                mutationCount++;
            }

            for (int index = destination.Count - 1;
                 index >= source.Count;
                 index--)
            {
                destination.RemoveAt(index);
                mutationCount++;
            }

            for (int index = destination.Count;
                 index < source.Count;
                 index++)
            {
                destination.Add(
                    source[index]
                );

                mutationCount++;
            }

            return mutationCount;
        }
    }
}
