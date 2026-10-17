using System.Text;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    public sealed class ResultPresenter : IScreenPresenter
    {
        private readonly GameSession _session;
        private readonly ResultView _view;
        private readonly SaveGateway _saves;

        public ResultPresenter(GameSession session, ResultView view, SaveGateway saves)
        {
            _session = session;
            _view = view;
            _saves = saves;
            _view.ReturnClicked += OnReturn;
        }

        public GameMode Mode => GameMode.ExpeditionResult;

        public void Enter()
        {
            var expedition = _session.Expedition;
            if (expedition == null) return;

            var sb = new StringBuilder();
            sb.Append(Title(expedition.Phase)).Append("\n\n");
            sb.Append("전리품: ").Append(expedition.Phase == ExpeditionPhase.Failed ? "잃어버림" : expedition.Inventory.Gold + "G").Append('\n');
            sb.Append("이동 거리: ").Append(expedition.StepsTaken).Append("칸\n");
            if (expedition.Fallen.Count > 0)
            {
                sb.Append("\n전사자\n");
                foreach (var hero in expedition.Fallen) sb.Append("✝ ").Append(hero.DisplayName).Append('\n');
            }
            _view.SetSummary(sb.ToString());
        }

        public void Exit() { }

        private static string Title(ExpeditionPhase phase)
        {
            switch (phase)
            {
                case ExpeditionPhase.Completed: return "원정 성공 — 보스를 쓰러뜨렸다!";
                case ExpeditionPhase.Retreated: return "후퇴 — 살아 돌아왔다.";
                case ExpeditionPhase.Failed: return "전멸 — 어둠이 모두를 삼켰다.";
                default: return phase.ToString();
            }
        }

        /// <summary>
        /// 귀환하면 자동 저장한다. 원정 중 저장이 남아 있으면 다음 실행 때 이미 끝난 원정이 다시 열리기 때문.
        /// 실패해도 귀환은 진행하고, 원인은 SaveGateway가 로그로 남긴다.
        /// </summary>
        private void OnReturn()
        {
            _session.ReturnToTown();
            if (_session.CanSave) _saves.TrySave(_session.CaptureSave());
        }

        public void Dispose() => _view.ReturnClicked -= OnReturn;
    }
}
