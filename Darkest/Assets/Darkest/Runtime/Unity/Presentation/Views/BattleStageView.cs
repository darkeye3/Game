using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Flow;
using Darkest.Unity.Infrastructure;
using UnityEngine;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>
    /// 월드 공간 진형 표시. 영웅은 왼쪽(1열이 중앙 쪽), 몬스터는 오른쪽.
    /// UnitView는 Object Pool로 재사용하고, Gizmos로 열 위치/현재 행동자/대상 후보를 씬 뷰에 그린다.
    /// </summary>
    public sealed class BattleStageView : MonoBehaviour
    {
        private const float CenterGap = 1.2f;
        private const float RankSpacing = 1.6f;
        private const float GroundY = 1.0f;

        // 카메라 프레이밍용 여유 공간: 스프라이트 반폭, 발밑 라벨 깊이, 머리 위 떠오르는 숫자 높이.
        private const float UnitHalfWidth = 0.75f;
        private const float LabelDepth = 0.9f;
        private const float Headroom = 2.5f;
        private const float GizmoSlotRadius = 0.45f;
        private const float GizmoActorMarkerHeight = 1.6f;
        private const float GizmoActorMarkerRadius = 0.15f;
        private static readonly Vector3 GizmoTargetBoxOffset = new Vector3(0f, 0.6f, 0f);
        private static readonly Vector3 GizmoTargetBoxSize = new Vector3(1f, 1.4f, 0f);

        private static readonly Color CurrentActorTint = new Color(1f, 0.85f, 0.4f);
        private static readonly Color DeathsDoorColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color LabelColor = new Color(0.95f, 0.92f, 0.85f);

        private readonly List<UnitView> _pool = new List<UnitView>();
        private readonly List<UnitView> _active = new List<UnitView>();
        private readonly Dictionary<string, UnitView> _byId = new Dictionary<string, UnitView>();
        private readonly Dictionary<string, Vector3> _lastSlotById = new Dictionary<string, Vector3>();
        private ISpriteProvider _sprites;
        private Font _font;

        // 디버그(Gizmos)용 마지막 렌더 상태
        private Vector3 _currentActorPosition;
        private bool _hasCurrentActor;
        private readonly List<Vector3> _targetPositions = new List<Vector3>();

        public void Initialize(ISpriteProvider sprites, Font font)
        {
            _sprites = sprites;
            _font = font;
        }

        public void Render(BattleContext context, IReadOnlyList<Combatant> highlightedTargets)
        {
            ReleaseAll();
            _hasCurrentActor = false;
            _targetPositions.Clear();
            if (context == null) return;

            RenderFormation(context, context.Heroes, -1f, highlightedTargets);
            RenderFormation(context, context.Monsters, 1f, highlightedTargets);
        }

        public void Clear()
        {
            ReleaseAll();
            _lastSlotById.Clear();
            _hasCurrentActor = false;
            _targetPositions.Clear();
        }

        /// <summary>연출용: 현재 그려진 유닛.</summary>
        public bool TryGetUnit(string combatantId, out UnitView view) => _byId.TryGetValue(combatantId, out view);

        /// <summary>연출용: 마지막으로 그려졌던 슬롯 위치(이미 사망해 사라진 유닛도 포함).</summary>
        public bool TryGetLastSlot(string combatantId, out Vector3 position) => _lastSlotById.TryGetValue(combatantId, out position);

        /// <summary>유닛·라벨·연출 텍스트가 차지하는 로컬 영역. StageCameraFitter가 이 영역이 보이도록 카메라를 맞춘다.</summary>
        public static Rect LocalBounds
        {
            get
            {
                float halfWidth = CenterGap + (Formation.MaxRanks - 1) * RankSpacing + UnitHalfWidth;
                float bottom = GroundY - LabelDepth;
                return new Rect(-halfWidth, bottom, halfWidth * 2f, LabelDepth + Headroom);
            }
        }

        public static Vector3 SlotPosition(Team team, int rank)
        {
            float side = team == Team.Heroes ? -1f : 1f;
            return new Vector3(side * (CenterGap + (rank - 1) * RankSpacing), GroundY, 0f);
        }

        private void RenderFormation(BattleContext context, Formation formation, float side, IReadOnlyList<Combatant> targets)
        {
            for (int i = 0; i < formation.Members.Count; i++)
            {
                var combatant = formation.Members[i];
                var view = Acquire();
                var position = SlotPosition(formation.Team, i + 1);
                view.Bind(combatant.Id, _sprites.Get(combatant.PortraitKey), side > 0f, position);
                _byId[combatant.Id] = view;
                _lastSlotById[combatant.Id] = position;

                bool isCurrent = combatant == context.CurrentActor;
                view.SetHighlight(isCurrent, CurrentActorTint);
                view.SetLabel(BuildLabel(combatant), IsAtDeathsDoor(combatant) ? DeathsDoorColor : LabelColor);

                if (isCurrent) { _hasCurrentActor = true; _currentActorPosition = position; }
                if (targets != null && Contains(targets, combatant)) _targetPositions.Add(position);
            }
        }

        private static string BuildLabel(Combatant combatant)
        {
            string label = combatant.DisplayName + "\n" + combatant.CurrentHp + "/" + combatant.MaxHp;
            if (combatant is Hero hero) label += "  스트레스 " + hero.Stress.Current;
            return label;
        }

        private static bool IsAtDeathsDoor(Combatant combatant) => combatant is Hero hero && hero.IsAtDeathsDoor;

        private static bool Contains(IReadOnlyList<Combatant> list, Combatant item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item) return true;
            }
            return false;
        }

        private UnitView Acquire()
        {
            UnitView view;
            if (_pool.Count > 0)
            {
                view = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
            }
            else view = UnitView.Create(transform, _font);
            _active.Add(view);
            return view;
        }

        private void ReleaseAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Release();
                _pool.Add(_active[i]);
            }
            _active.Clear();
            _byId.Clear();
        }

        private void OnDrawGizmos()
        {
            for (int rank = 1; rank <= Formation.MaxRanks; rank++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.TransformPoint(SlotPosition(Team.Heroes, rank)), GizmoSlotRadius);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.TransformPoint(SlotPosition(Team.Monsters, rank)), GizmoSlotRadius);
            }

            var bounds = LocalBounds;
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(transform.TransformPoint(bounds.center), bounds.size);

            if (_hasCurrentActor)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.TransformPoint(_currentActorPosition + Vector3.up * GizmoActorMarkerHeight), GizmoActorMarkerRadius);
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < _targetPositions.Count; i++)
            {
                var target = transform.TransformPoint(_targetPositions[i]);
                Gizmos.DrawWireCube(target + GizmoTargetBoxOffset, GizmoTargetBoxSize);
                if (_hasCurrentActor) Gizmos.DrawLine(transform.TransformPoint(_currentActorPosition), target);
            }
        }
    }
}
