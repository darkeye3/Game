using System;
using System.Collections.Generic;
using Darkest.Domain.Core;
using Darkest.Domain.Game;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>GameModeChanged 이벤트 → 해당 화면만 보이게 전환.</summary>
    public sealed class ScreenRouter : IDisposable
    {
        private readonly Dictionary<GameMode, (ScreenView view, IScreenPresenter presenter)> _screens =
            new Dictionary<GameMode, (ScreenView, IScreenPresenter)>();
        private readonly IDisposable _subscription;
        private readonly IGameLogger _logger;
        private IScreenPresenter _current;

        public ScreenRouter(IEventBus events, IGameLogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
            _subscription = events.Subscribe<GameModeChanged>(e => Show(e.Mode));
        }

        public void Register(ScreenView view, IScreenPresenter presenter) => _screens[presenter.Mode] = (view, presenter);

        public void Show(GameMode mode)
        {
            if (!_screens.TryGetValue(mode, out var target))
            {
                _logger.Error("UI", "등록되지 않은 화면: " + mode);
                return;
            }

            _current?.Exit();
            foreach (var screen in _screens.Values) screen.view.Hide();

            target.view.Show();
            _current = target.presenter;
            _current.Enter();
        }

        public void Dispose()
        {
            _subscription.Dispose();
            foreach (var screen in _screens.Values) screen.presenter.Dispose();
            _screens.Clear();
            _current = null;
        }
    }
}
