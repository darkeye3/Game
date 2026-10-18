using System;
using Darkest.Domain.Game;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>화면 하나를 담당하는 Presenter. ScreenRouter가 GameMode에 맞춰 Enter/Exit를 호출한다.</summary>
    public interface IScreenPresenter : IDisposable
    {
        GameMode Mode { get; }
        void Enter();
        void Exit();
    }
}
