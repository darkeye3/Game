using System;
using Darkest.Domain.Combat;
using Darkest.Domain.Game;
using Darkest.Unity.Presentation.Ui;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>[영웅] 탭의 장신구 목록: 선택한 영웅의 슬롯(클릭 해제) + 보관함(클릭 장착).</summary>
    internal sealed class HeroTrinketPanel
    {
        private readonly GameSession _session;
        private readonly PooledButtonList _list;
        private readonly Action<string, string> _show;

        /// <param name="show">(에러, 성공 메시지) 표시 + 다시 그리기.</param>
        public HeroTrinketPanel(GameSession session, PooledButtonList list, Action<string, string> show)
        {
            _session = session;
            _list = list;
            _show = show;
        }

        public void Render(Hero selected)
        {
            var trinkets = _session.TownService.Trinkets;
            _list.Begin();
            if (selected != null)
            {
                for (int slot = 0; slot < Hero.TrinketSlots; slot++)
                {
                    var equipped = selected.GetTrinket(slot);
                    int captured = slot;
                    string label = "슬롯 " + (slot + 1) + ": " + (equipped == null ? "비어 있음" : ItemTextFormat.Trinket(equipped));
                    _list.Add(label, () => _show(trinkets.TryUnequip(selected, captured), "장신구를 뺐습니다."), equipped != null, equipped != null);
                }
            }

            var stash = _session.Town.Trinkets;
            for (int i = 0; i < stash.Count; i++)
            {
                var trinket = stash[i];
                _list.Add("장착 ▸ " + ItemTextFormat.Trinket(trinket),
                    () => _show(trinkets.TryEquip(selected, trinket), trinket.DisplayName + " 장착"), selected != null);
            }
            _list.End();
        }
    }
}
