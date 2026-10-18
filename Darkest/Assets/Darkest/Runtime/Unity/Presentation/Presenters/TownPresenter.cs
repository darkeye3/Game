using System;
using System.Collections.Generic;
using System.Text;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    public sealed class TownPresenter : IScreenPresenter
    {
        private static readonly Provisions DefaultProvisions = new Provisions(food: 8, torches: 6, firewood: 1);

        private readonly GameSession _session;
        private readonly TownView _view;
        private readonly SaveGateway _saves;
        private readonly Action _newGameRequested;
        private readonly List<Hero> _party = new List<Hero>(Formation.MaxRanks);
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly HeroTrinketPanel _trinketPanel;
        private readonly HamletTabPresenter _hamletTab;
        private Hero _selected;
        private Provisions _provisions = DefaultProvisions;

        public TownPresenter(GameSession session, TownView view, SaveGateway saves, Action newGameRequested)
        {
            _session = session;
            _view = view;
            _saves = saves;
            _newGameRequested = newGameRequested;
            _trinketPanel = new HeroTrinketPanel(session, view.HeroTrinkets, Show);
            _hamletTab = new HamletTabPresenter(session, view, Render);

            _view.TabClicked += OnTabClicked;
            _view.TreatClicked += OnTreat;
            _view.DismissClicked += OnDismiss;
            _view.EmbarkClicked += OnEmbark;
            _view.SaveClicked += OnSave;
            _view.NewGameClicked += OnNewGame;
            _view.FoodChanged += OnFoodChanged;
            _view.TorchChanged += OnTorchChanged;
            _view.FirewoodChanged += OnFirewoodChanged;
        }

        public GameMode Mode => GameMode.Town;

        public void Enter()
        {
            // 귀환 후 사망/해고된 영웅은 파티 선택에서 제외.
            _party.RemoveAll(h => h.IsDead || _session.Town.FindHero(h.Id) == null);
            if (_selected != null && _session.Town.FindHero(_selected.Id) == null) _selected = null;
            _view.SetMessage(string.Empty);
            _view.SetHamletMessage(string.Empty);
            _view.ShowTab(TownTab.Heroes);
            Render();
        }

        public void Exit() { }

        private void Render()
        {
            var town = _session.Town;
            _view.SetHeader("영지 — " + town.Week + "주차   골드 " + town.Gold + "   명부 " + town.Roster.Count + "/" + _session.TownService.Buildings.RosterCapacity);

            _view.Roster.Begin();
            for (int i = 0; i < town.Roster.Count; i++)
            {
                var hero = town.Roster[i];
                int slot = _party.IndexOf(hero);
                string label = (slot >= 0 ? "[" + (slot + 1) + "열] " : "") + hero.DisplayName + " Lv" + hero.Level + " · " + ClassName(hero)
                               + "  HP " + hero.CurrentHp + "/" + hero.MaxHp + "  스트레스 " + hero.Stress.Current;
                _view.Roster.Add(label, () => OnHeroClicked(hero), true, slot >= 0);
            }
            _view.Roster.End();

            _view.Recruits.Begin();
            for (int i = 0; i < town.Recruits.Count; i++)
            {
                var recruit = town.Recruits[i];
                _view.Recruits.Add(recruit.DisplayName + " · " + ClassName(recruit) + "  (" + _session.TownService.RecruitCost + "G)",
                    () => Show(_session.TownService.TryRecruit(recruit), "모집 완료: " + recruit.DisplayName));
            }
            _view.Recruits.End();

            _view.SetDetails(DescribeSelected());
            var service = _session.TownService;
            _view.SetSupplies("식량 " + _provisions.Food + "(" + service.FoodPrice + "G)  횃불 " + _provisions.Torches + "(" + service.TorchPrice + "G)"
                              + "  장작 " + _provisions.Firewood + "(" + service.FirewoodPrice + "G)"
                              + "\n비용 " + service.ProvisionCost(_provisions) + "G   선술집 " + service.TavernCost + "G");
            _trinketPanel.Render(_selected);
            _hamletTab.Render();
        }

        private string DescribeSelected()
        {
            if (_selected == null) return "영웅을 선택하세요.\n클릭 순서대로 1~4열에 배치됩니다.";
            _sb.Clear();
            _sb.Append(_selected.DisplayName).Append(" · ").Append(ClassName(_selected)).Append('\n');
            _sb.Append("레벨 ").Append(_selected.Level).Append("  경험치 ").Append(_selected.Experience);
            int toNext = _session.Progression.XpToNextLevel(_selected);
            _sb.Append(toNext > 0 ? "  (다음 레벨까지 " + toNext + ")" : "  (최대 레벨)").Append('\n');
            _sb.Append("HP ").Append(_selected.CurrentHp).Append('/').Append(_selected.MaxHp)
               .Append("  스트레스 ").Append(_selected.Stress.Current).Append('/').Append(_selected.Stress.Max).Append('\n');
            if (_selected.Stress.Resolve != null) _sb.Append("결의: ").Append(_selected.Stress.Resolve.DisplayName).Append('\n');
            _sb.Append("속도 ").Append(_selected.GetStat(StatType.Speed))
               .Append("  회피 ").Append(_selected.GetStat(StatType.Dodge))
               .Append("  방어 ").Append(_selected.GetStat(StatType.Protection))
               .Append("  피해 ").Append(_selected.GetStat(StatType.DamageMin)).Append('~').Append(_selected.GetStat(StatType.DamageMax)).Append("\n\n스킬\n");
            foreach (var skill in _selected.Skills)
            {
                _sb.Append("· ").Append(TextFormat.SkillTooltip(skill)).Append('\n');
            }
            return _sb.ToString();
        }

        private string ClassName(Hero hero) => _session.Catalog.GetHeroClass(hero.ClassId).DisplayName;

        private void OnHeroClicked(Hero hero)
        {
            _selected = hero;
            if (_party.Remove(hero)) { Render(); return; }
            if (_party.Count >= Formation.MaxRanks) _view.SetMessage("파티는 최대 4명입니다.");
            else _party.Add(hero);
            Render();
        }

        private void OnTabClicked(TownTab tab)
        {
            _view.ShowTab(tab);
            Render();
        }

        private void OnTreat() => Show(_selected == null ? "영웅을 먼저 선택하세요." : _session.TownService.TryTreatStress(_selected), "선술집에서 휴식했습니다.");

        private void OnDismiss()
        {
            if (_selected == null) { Show("영웅을 먼저 선택하세요.", null); return; }
            var hero = _selected;
            _party.Remove(hero);
            _selected = null;
            Show(_session.TownService.TryDismiss(hero), hero.DisplayName + " 해고");
        }

        private void OnEmbark() => Show(_session.TryEmbark(_party, _provisions), null);

        private void OnSave() => Show(_saves.TrySave(_session.CaptureSave()), "저장했습니다.");

        private void OnNewGame() => _newGameRequested?.Invoke();

        private void OnFoodChanged(int delta) => SetProvisions(_provisions.WithFood(Math.Max(0, _provisions.Food + delta)));
        private void OnTorchChanged(int delta) => SetProvisions(_provisions.WithTorches(Math.Max(0, _provisions.Torches + delta)));
        private void OnFirewoodChanged(int delta) => SetProvisions(_provisions.WithFirewood(Math.Max(0, _provisions.Firewood + delta)));

        private void SetProvisions(Provisions provisions)
        {
            _provisions = provisions;
            Render();
        }

        /// <summary>결과 표시 + 다시 그리기. error가 null이면 성공 메시지.</summary>
        private void Show(string error, string successMessage)
        {
            if (_session.Mode != GameMode.Town) return; // 출발 성공 시 화면이 이미 바뀌었다.
            _view.SetMessage(error ?? successMessage ?? string.Empty);
            Render();
        }

        public void Dispose()
        {
            _view.TabClicked -= OnTabClicked;
            _view.TreatClicked -= OnTreat;
            _view.DismissClicked -= OnDismiss;
            _view.EmbarkClicked -= OnEmbark;
            _view.SaveClicked -= OnSave;
            _view.NewGameClicked -= OnNewGame;
            _view.FoodChanged -= OnFoodChanged;
            _view.TorchChanged -= OnTorchChanged;
            _view.FirewoodChanged -= OnFirewoodChanged;
        }
    }
}
