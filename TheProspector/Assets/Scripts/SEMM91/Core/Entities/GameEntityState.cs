

namespace SEMM91.Core.Entities
{
    [System.Flags]
    public enum GameEntityState
    {
        None = 0,
        Damaged = 1 << 0,
        Destroyed = 1 << 1,
        Hidden = 1 << 2,
        Locked = 1 << 3,
        Unavailable = 1 << 4,
        Exhausted = 1 << 5
    }
}