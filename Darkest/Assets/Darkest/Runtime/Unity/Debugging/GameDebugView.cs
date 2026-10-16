using System;
using System.Collections.Generic;
using System.Text;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using UnityEngine;

namespace Darkest.Unity.Debugging
{
    /// <summary>
    /// 인스펙터용 상태 스냅샷. Update로 폴링하지 않고 도메인 이벤트가 올 때만 갱신한다.
    /// 버그 제보 시 seed + 명령 기록(Context Menu → Dump Battle History)을 함께 남기면 재현할 수 있다.
    /// </summary>
    public sealed class GameDebugView : MonoBehaviour
    {
        [SerializeField, ReadOnlyField] private int seed;
        [SerializeField, ReadOnlyField] private string mode;
        [SerializeField, ReadOnlyField] private int week;
        [SerializeField, ReadOnlyField] private int gold;
        [SerializeField, ReadOnlyField] private string expedition;
        [SerializeField, ReadOnlyField] private int battleRound;
        [SerializeField, ReadOnlyField] private string currentActor;
        [SerializeField, ReadOnlyField] private string battlePhase;
        [SerializeField, ReadOnlyField] private int commandsRecorded;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private GameSession _session;

        public void Bind(GameSession session)
        {
            Unbind();
            _session = session;
            if (session == null) return;
            _subscriptions.Add(session.Events.Subscribe<GameModeChanged>(_ => Refresh()));
            _subscriptions.Add(session.Events.Subscribe<TurnStarted>(_ => Refresh()));
            _subscriptions.Add(session.Events.Subscribe<RoomEntered>(_ => Refresh()));
            _subscriptions.Add(session.Events.Subscribe<BattleEnded>(_ => Refresh()));
            Refresh();
        }

        public void Refresh()
        {
            if (_session == null) return;
            seed = _session.Seed;
            mode = _session.Mode.ToString();
            week = _session.Town.Week;
            gold = _session.Town.Gold;

            var exp = _session.Expedition;
            expedition = exp == null ? "-" : "room " + exp.CurrentRoomId + " / " + exp.Phase + " / light " + exp.Light.Value;

            var battle = _session.Battle;
            battleRound = battle?.Context.Round ?? 0;
            currentActor = battle?.CurrentActor?.ToString() ?? "-";
            battlePhase = battle?.Phase.ToString() ?? "-";
            commandsRecorded = battle?.Context.History.Count ?? 0;
        }

        [ContextMenu("Dump Battle History")]
        private void DumpBattleHistory()
        {
            var battle = _session?.Battle;
            if (battle == null)
            {
                Debug.Log("[Darkest][Debug] 진행 중인 전투가 없습니다.");
                return;
            }
            var sb = new StringBuilder("[Darkest][Debug] seed=").Append(_session.Seed).Append('\n');
            foreach (var command in battle.Context.History) sb.Append(command.Describe()).Append('\n');
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Dump Dungeon Map")]
        private void DumpDungeonMap()
        {
            var exp = _session?.Expedition;
            if (exp == null) return;
            var sb = new StringBuilder("[Darkest][Debug] map seed=").Append(exp.Map.Seed).Append('\n');
            foreach (Room room in exp.Map.Rooms) sb.Append(room).Append(" -> ").Append(string.Join(",", room.Neighbors)).Append('\n');
            Debug.Log(sb.ToString());
        }

        private void Unbind()
        {
            for (int i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
            _session = null;
        }

        private void OnDestroy() => Unbind();
    }
}
