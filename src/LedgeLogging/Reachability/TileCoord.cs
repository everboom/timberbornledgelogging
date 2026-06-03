using System;

namespace LedgeLogging.Reachability
{
    /// <summary>
    /// An integer terrain tile coordinate: <see cref="X"/>/<see cref="Y"/> identify the
    /// column and <see cref="Z"/> the terrain level. Mirrors the layout of Timberborn's
    /// <c>Vector3Int</c> grid coordinates, but is deliberately defined here in BCL-only
    /// terms so the reachability search can be unit-tested without the game or Unity.
    /// The Harmony glue converts between this and <c>Vector3Int</c> at the patch site.
    /// </summary>
    public readonly struct TileCoord : IEquatable<TileCoord>
    {
        #region Construction

        /// <summary>Creates a tile coordinate.</summary>
        /// <param name="x">Column X.</param>
        /// <param name="y">Column Y.</param>
        /// <param name="z">Terrain level.</param>
        public TileCoord(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region Properties

        /// <summary>Column X.</summary>
        public int X { get; }

        /// <summary>Column Y.</summary>
        public int Y { get; }

        /// <summary>Terrain level (height).</summary>
        public int Z { get; }

        #endregion

        #region Operations

        /// <summary>Returns this coordinate's column with the level replaced by <paramref name="z"/>.</summary>
        public TileCoord WithZ(int z) => new TileCoord(X, Y, z);

        #endregion

        #region Equality

        /// <inheritdoc/>
        public bool Equals(TileCoord other) => X == other.X && Y == other.Y && Z == other.Z;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is TileCoord other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <summary>Equality operator.</summary>
        public static bool operator ==(TileCoord left, TileCoord right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(TileCoord left, TileCoord right) => !left.Equals(right);

        #endregion

        #region Formatting

        /// <inheritdoc/>
        public override string ToString() => $"({X}, {Y}, {Z})";

        #endregion
    }
}
