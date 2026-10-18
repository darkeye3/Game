using System;
using Darkest.Domain.Game;
using Darkest.Domain.Hamlet;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>마을 화면의 [영지 시설] 탭: 건물 업그레이드와 장신구 판매. TownPresenter가 소유하고 Render를 위임한다.</summary>
    internal sealed class HamletTabPresenter
    {
        private static readonly BuildingType[] AllBuildings = (BuildingType[])Enum.GetValues(typeof(BuildingType));

        private readonly GameSession _session;
        private readonly TownView _view;
        private readonly Action _changed;

        /// <param name="changed">골드/보관함이 바뀌었을 때 화면 전체를 다시 그리게 하는 콜백.</param>
        public HamletTabPresenter(GameSession session, TownView view, Action changed)
        {
            _session = session;
            _view = view;
            _changed = changed;
        }

        public void Render()
        {
            var town = _session.TownService;
            _view.Buildings.Begin();
            for (int i = 0; i < AllBuildings.Length; i++)
            {
                var type = AllBuildings[i];
                int cost = town.Buildings.UpgradeCost(type);
                string label = ItemTextFormat.BuildingName(type) + " " + town.Buildings.Level(type) + "/" + town.Buildings.Rules.MaxLevel + "단계"
                               + "\n  " + ItemTextFormat.BuildingEffect(type, town)
                               + (cost < 0 ? "  (최대)" : "  ▲ " + cost + "G");
                _view.Buildings.Add(label, () => Show(town.Buildings.TryUpgrade(type), ItemTextFormat.BuildingName(type) + " 업그레이드"), cost >= 0);
            }
            _view.Buildings.End();

            var stash = _session.Town.Trinkets;
            _view.Stash.Begin();
            for (int i = 0; i < stash.Count; i++)
            {
                var trinket = stash[i];
                _view.Stash.Add("+" + trinket.SellValue + "G  " + ItemTextFormat.Trinket(trinket),
                    () => Show(town.Trinkets.TrySell(trinket), trinket.DisplayName + " 판매"));
            }
            if (stash.Count == 0) _view.Stash.Add("보관함이 비었습니다. 보물방·보스에서 장신구를 얻습니다.", null, false);
            _view.Stash.End();
        }

        private void Show(string error, string successMessage)
        {
            _view.SetHamletMessage(error ?? successMessage);
            _changed();
        }
    }
}
