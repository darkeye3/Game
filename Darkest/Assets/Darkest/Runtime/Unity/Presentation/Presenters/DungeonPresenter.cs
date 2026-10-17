using System.Text;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    public sealed class DungeonPresenter : IScreenPresenter
    {
        private readonly GameSession _session;
        private readonly DungeonView _view;
        private readonly SaveGateway _saves;
        private readonly StringBuilder _sb = new StringBuilder();

        public DungeonPresenter(GameSession session, DungeonView view, SaveGateway saves)
        {
            _session = session;
            _view = view;
            _saves = saves;
            _view.SaveClicked += OnSave;
            _view.TorchClicked += OnTorch;
            _view.CurioClicked += OnCurio;
            _view.RetreatClicked += OnRetreat;
            _view.CampClicked += OnCamp;
        }

        public GameMode Mode => GameMode.Exploring;

        public void Enter()
        {
            _view.SetMessage(string.Empty);
            Render();
        }

        public void Exit() { }

        private void Render()
        {
            var expedition = _session.Expedition;
            if (expedition == null) return;

            _view.SetHeader("빛 " + expedition.Light.Value + " (" + TextFormat.LightName(expedition.Light.Level) + ")"
                            + "   식량 " + expedition.Inventory.Food + "   횃불 " + expedition.Inventory.Torches + "   장작 " + expedition.Inventory.Firewood
                            + "\n전리품 " + expedition.Inventory.Gold + "G   이동 " + expedition.StepsTaken + "칸   seed " + expedition.Map.Seed);
            _view.SetMap(DescribeMap(expedition));
            _view.SetParty(TextFormat.Formation(expedition.Party));

            var room = expedition.CurrentRoom;
            _view.SetCurioAvailable(room.Type == RoomType.Curio && !room.IsCleared);
            _view.SetCampAvailable(room.IsCleared && expedition.Inventory.Firewood > 0);

            _view.Exits.Begin();
            for (int i = 0; i < room.Neighbors.Count; i++)
            {
                var next = expedition.Map.Get(room.Neighbors[i]);
                int id = next.Id;
                _view.Exits.Add("→ " + TextFormat.RoomName(next, next.IsVisited), () => OnMove(id), room.IsCleared);
            }
            _view.Exits.End();
        }

        private string DescribeMap(Expedition expedition)
        {
            _sb.Clear();
            foreach (var room in expedition.Map.Rooms)
            {
                _sb.Append(room.Id == expedition.CurrentRoomId ? "▶ " : "   ");
                _sb.Append(TextFormat.RoomName(room, room.IsVisited));
                if (room.Neighbors.Count > 0)
                {
                    _sb.Append("  ⇢ ");
                    for (int i = 0; i < room.Neighbors.Count; i++)
                    {
                        if (i > 0) _sb.Append(',');
                        _sb.Append(room.Neighbors[i]);
                    }
                }
                _sb.Append('\n');
            }
            return _sb.ToString();
        }

        private void OnMove(int roomId) => After(_session.TryMoveTo(roomId));
        private void OnTorch() => After(_session.TryUseTorch());
        private void OnCurio() => After(_session.TryInteractCurio());
        private void OnRetreat() => _session.Retreat();
        private void OnCamp() => After(_session.TryCamp());

        private void OnSave()
        {
            string error = _session.CanSave ? _saves.TrySave(_session.CaptureSave()) : "지금은 저장할 수 없습니다.";
            _view.SetMessage(error ?? "저장했습니다. 다음 실행 때 이 방에서 이어집니다.");
        }

        private void After(string error)
        {
            if (_session.Mode != GameMode.Exploring) return; // 전투/결과 화면으로 전환됨
            _view.SetMessage(error ?? string.Empty);
            Render();
        }

        public void Dispose()
        {
            _view.TorchClicked -= OnTorch;
            _view.CurioClicked -= OnCurio;
            _view.RetreatClicked -= OnRetreat;
            _view.CampClicked -= OnCamp;
            _view.SaveClicked -= OnSave;
        }
    }
}
