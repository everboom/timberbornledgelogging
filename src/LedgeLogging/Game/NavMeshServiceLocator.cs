using System;
using LedgeLogging.Settings;
using Timberborn.Navigation;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Bridges Game-scope DI services to the static Harmony patch code, which cannot
    /// receive injected dependencies. Bound as a loadable singleton in
    /// <see cref="LedgeLoggingConfigurator"/>; <see cref="Load"/> runs once when the
    /// Game scope initialises and stashes the services into static fields the patches read.
    /// </summary>
    internal sealed class NavMeshServiceLocator : ILoadableSingleton
    {
        #region Static access

        private static INavMeshService? _navMeshService;
        private static IDistrictService? _districtService;
        private static ITerrainService? _terrainService;
        private static RestrictedNodeMap? _restrictedNodeMap;
        private static NodeIdService? _nodeIdService;
        private static LedgeLoggingSettings? _settings;

        /// <summary>
        /// The Game-scope navmesh service. Throws if read before the Game scope has
        /// loaded — failing loudly rather than letting a patch operate on a null service.
        /// </summary>
        public static INavMeshService NavMeshService =>
            _navMeshService ?? throw new InvalidOperationException(
                "[LedgeLogging] NavMeshServiceLocator read before Load(); no Game scope is active.");

        /// <summary>
        /// The Game-scope district service (used for the demolish-reachability UI status).
        /// Throws if read before the Game scope has loaded.
        /// </summary>
        public static IDistrictService DistrictService =>
            _districtService ?? throw new InvalidOperationException(
                "[LedgeLogging] NavMeshServiceLocator read before Load(); no Game scope is active.");

        /// <summary>
        /// Whether <paramref name="coordinates"/> is a <em>restricted</em> navmesh node — one a
        /// worker may path <em>through</em> but not stand on as a destination (e.g. a tile inside
        /// a building). This is vanilla's own rule for valid work tiles, so a ledge standing tile
        /// that is restricted must be rejected: vanilla would never send a worker to stand there.
        /// Caller must ensure the tile is on the navmesh first. Throws if read before Load().
        /// </summary>
        public static bool IsRestricted(Vector3Int coordinates)
        {
            if (_restrictedNodeMap == null || _nodeIdService == null)
            {
                throw new InvalidOperationException(
                    "[LedgeLogging] NavMeshServiceLocator read before Load(); no Game scope is active.");
            }
            return _restrictedNodeMap.IsNodeRestricted(_nodeIdService.GridToId(coordinates));
        }

        /// <summary>
        /// The player-chosen maximum number of terrain levels below a worker that a marked
        /// resource may be cleared from, resolved live each read (so changing the setting takes
        /// effect on the next reachability evaluation) and capped at the map height for "Any".
        /// Throws if read before the Game scope has loaded.
        /// </summary>
        public static int MaxDepthBelow
        {
            get
            {
                if (_settings == null || _terrainService == null)
                {
                    throw new InvalidOperationException(
                        "[LedgeLogging] NavMeshServiceLocator read before Load(); no Game scope is active.");
                }
                return _settings.MaxDepthBelow(_terrainService.Size.z);
            }
        }

        #endregion

        #region Construction

        private readonly INavMeshService _injectedNavMeshService;
        private readonly IDistrictService _injectedDistrictService;
        private readonly ITerrainService _injectedTerrainService;
        private readonly RestrictedNodeMap _injectedRestrictedNodeMap;
        private readonly NodeIdService _injectedNodeIdService;
        private readonly LedgeLoggingSettings _injectedSettings;

        /// <summary>Injected by Bindito with the Game-scope navigation services and mod settings.</summary>
        public NavMeshServiceLocator(
            INavMeshService navMeshService,
            IDistrictService districtService,
            ITerrainService terrainService,
            RestrictedNodeMap restrictedNodeMap,
            NodeIdService nodeIdService,
            LedgeLoggingSettings settings)
        {
            _injectedNavMeshService = navMeshService;
            _injectedDistrictService = districtService;
            _injectedTerrainService = terrainService;
            _injectedRestrictedNodeMap = restrictedNodeMap;
            _injectedNodeIdService = nodeIdService;
            _injectedSettings = settings;
        }

        #endregion

        #region ILoadableSingleton

        /// <summary>Publishes the injected services to the static accessors.</summary>
        public void Load()
        {
            _navMeshService = _injectedNavMeshService;
            _districtService = _injectedDistrictService;
            _terrainService = _injectedTerrainService;
            _restrictedNodeMap = _injectedRestrictedNodeMap;
            _nodeIdService = _injectedNodeIdService;
            _settings = _injectedSettings;
        }

        #endregion
    }
}
