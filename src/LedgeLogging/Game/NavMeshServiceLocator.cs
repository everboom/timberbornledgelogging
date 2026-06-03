using System;
using Timberborn.Navigation;
using Timberborn.SingletonSystem;

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
        private static INavigationService? _navigationService;

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
        /// The Game-scope navigation service (used to build a building-respecting destination
        /// to the standing tile). Throws if read before the Game scope has loaded.
        /// </summary>
        public static INavigationService NavigationService =>
            _navigationService ?? throw new InvalidOperationException(
                "[LedgeLogging] NavMeshServiceLocator read before Load(); no Game scope is active.");

        #endregion

        #region Construction

        private readonly INavMeshService _injectedNavMeshService;
        private readonly IDistrictService _injectedDistrictService;
        private readonly INavigationService _injectedNavigationService;

        /// <summary>Injected by Bindito with the Game-scope navigation services.</summary>
        public NavMeshServiceLocator(
            INavMeshService navMeshService, IDistrictService districtService, INavigationService navigationService)
        {
            _injectedNavMeshService = navMeshService;
            _injectedDistrictService = districtService;
            _injectedNavigationService = navigationService;
        }

        #endregion

        #region ILoadableSingleton

        /// <summary>Publishes the injected services to the static accessors.</summary>
        public void Load()
        {
            _navMeshService = _injectedNavMeshService;
            _districtService = _injectedDistrictService;
            _navigationService = _injectedNavigationService;
        }

        #endregion
    }
}
