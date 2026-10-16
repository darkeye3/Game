using System;
using System.Collections.Generic;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat
{
    /// <summary>
    /// 1~4열 진형. 리스트 순서 = 열(rank) 순서이며 1열이 가장 앞이다.
    /// 사망자는 제거되고 뒤쪽 인원이 앞으로 당겨진다(빈칸 없음 → 위치 계산이 단순해진다).
    /// </summary>
    public sealed class Formation
    {
        public const int MaxRanks = 4;

        private readonly List<Combatant> _members = new List<Combatant>(MaxRanks);

        public Formation(Team team)
        {
            Team = team;
        }

        public Team Team { get; }
        public IReadOnlyList<Combatant> Members => _members;
        public int Count => _members.Count;
        public bool IsEmpty => _members.Count == 0;

        public void Add(Combatant combatant)
        {
            Guard.NotNull(combatant, nameof(combatant));
            Guard.That(combatant.Team == Team, "다른 팀을 진형에 넣을 수 없습니다: " + combatant.DisplayName);
            Guard.That(_members.Count < MaxRanks, "진형이 가득 찼습니다.");
            Guard.That(!_members.Contains(combatant), "이미 진형에 있습니다: " + combatant.DisplayName);
            _members.Add(combatant);
        }

        /// <returns>1부터 시작하는 열 번호. 없으면 0.</returns>
        public int RankOf(Combatant combatant)
        {
            int index = _members.IndexOf(combatant);
            return index < 0 ? 0 : index + 1;
        }

        public Combatant AtRank(int rank)
        {
            if (rank < 1 || rank > _members.Count) return null;
            return _members[rank - 1];
        }

        public bool Contains(Combatant combatant) => _members.Contains(combatant);

        /// <summary>
        /// delta만큼 이동(음수 = 앞으로, 양수 = 뒤로). 중간 인원은 한 칸씩 밀린다.
        /// </summary>
        /// <returns>실제 이동한 칸 수(부호 포함)</returns>
        public int Move(Combatant combatant, int delta)
        {
            int index = _members.IndexOf(combatant);
            if (index < 0) throw new InvalidOperationException("진형에 없는 대상: " + combatant?.DisplayName);

            int targetIndex = Math.Max(0, Math.Min(_members.Count - 1, index + delta));
            if (targetIndex == index) return 0;

            _members.RemoveAt(index);
            _members.Insert(targetIndex, combatant);
            return targetIndex - index;
        }

        /// <summary>죽은 인원을 제거하고 앞당긴다.</summary>
        /// <returns>제거된 인원을 buffer에 채워 넣는다(할당 방지용 외부 버퍼).</returns>
        public void RemoveDead(List<Combatant> removedBuffer)
        {
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                if (!_members[i].IsDead) continue;
                removedBuffer?.Add(_members[i]);
                _members.RemoveAt(i);
            }
        }
    }
}
