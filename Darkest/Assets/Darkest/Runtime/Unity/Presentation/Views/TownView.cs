using System;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    public enum TownTab
    {
        Heroes,
        Hamlet
    }

    /// <summary>
    /// 마을 화면. 탭 2개: [영웅] 명부·편성·선술집·장신구 장착·보급·출발 / [영지] 건물 업그레이드·장신구 판매.
    /// 탭 전환은 View가 그리고, 어느 탭을 열지는 Presenter가 정한다.
    /// </summary>
    public sealed class TownView : ScreenView
    {
        private const float TabBarBottom = 0.93f;
        /// <summary>탭 버튼 1개 대비 머리글 너비 비율.</summary>
        private const float HeaderWidthWeight = 3f;

        private Text _header;
        private Text _details;
        private Text _supplies;
        private Text _message;
        private Text _hamletMessage;
        private RectTransform _heroTab;
        private RectTransform _hamletTab;

        public event Action<TownTab> TabClicked;
        public event Action TreatClicked;
        public event Action DismissClicked;
        public event Action EmbarkClicked;
        public event Action SaveClicked;
        public event Action NewGameClicked;
        public event Action<int> FoodChanged;
        public event Action<int> TorchChanged;
        public event Action<int> FirewoodChanged;

        public PooledButtonList Roster { get; private set; }
        public PooledButtonList Recruits { get; private set; }
        public PooledButtonList HeroTrinkets { get; private set; }
        public PooledButtonList Buildings { get; private set; }
        public PooledButtonList Stash { get; private set; }

        protected override void BuildContent(RectTransform root)
        {
            var bar = Factory.CreatePanel(root, "TabBar", UiTheme.Panel, new Vector2(0.01f, TabBarBottom), new Vector2(0.99f, 0.99f));
            var barRow = Factory.CreateHorizontal(bar, "TabRow");
            _header = Factory.CreateText(barRow, "Header", UiTheme.BodyFontSize, TextAnchor.MiddleLeft);
            _header.GetComponent<LayoutElement>().flexibleWidth = HeaderWidthWeight;
            AddButton(barRow, "영웅", () => TabClicked?.Invoke(TownTab.Heroes));
            AddButton(barRow, "영지 시설", () => TabClicked?.Invoke(TownTab.Hamlet));

            _heroTab = Factory.CreatePanel(root, "HeroTab", UiTheme.Clear, Vector2.zero, new Vector2(1f, TabBarBottom));
            BuildHeroTab(_heroTab);
            _hamletTab = Factory.CreatePanel(root, "HamletTab", UiTheme.Clear, Vector2.zero, new Vector2(1f, TabBarBottom));
            BuildHamletTab(_hamletTab);
            ShowTab(TownTab.Heroes);
        }

        private void BuildHeroTab(RectTransform tab)
        {
            var left = Column(tab, "Roster", 0.01f, 0.33f, 0.02f, 0.99f);
            Factory.CreateText(left, "RosterTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "명부 (클릭: 선택 / 파티 편성 토글)";
            Roster = new PooledButtonList(Factory, Factory.CreateScrollList(left, "RosterList"));

            var middle = Column(tab, "Details", 0.34f, 0.66f, 0.02f, 0.99f);
            _details = Factory.CreateText(middle, "Details", UiTheme.SmallFontSize);
            var actions = Factory.CreateHorizontal(middle, "HeroActions");
            AddButton(actions, "선술집 (스트레스 치료)", () => TreatClicked?.Invoke());
            AddButton(actions, "해고", () => DismissClicked?.Invoke());
            Factory.CreateText(middle, "TrinketTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "장신구 (슬롯 클릭: 해제 / 보관함 클릭: 장착)";
            HeroTrinkets = new PooledButtonList(Factory, Factory.CreateScrollList(middle, "HeroTrinketList"));
            _message = Factory.CreateText(middle, "Message", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 50f);

            var right = Column(tab, "Provision", 0.67f, 0.99f, 0.02f, 0.99f);
            Factory.CreateText(right, "RecruitTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "역마차 (모집)";
            Recruits = new PooledButtonList(Factory, Factory.CreateScrollList(right, "RecruitList"));
            _supplies = Factory.CreateText(right, "Supplies", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 80f);
            AddStepperRow(right, "식량", d => FoodChanged?.Invoke(d));
            AddStepperRow(right, "횃불", d => TorchChanged?.Invoke(d));
            AddStepperRow(right, "장작", d => FirewoodChanged?.Invoke(d));
            AddButton(right, "▶ 던전 출발", () => EmbarkClicked?.Invoke());
            var saveRow = Factory.CreateHorizontal(right, "SaveRow");
            AddButton(saveRow, "저장", () => SaveClicked?.Invoke());
            AddButton(saveRow, "새 게임", () => NewGameClicked?.Invoke());
        }

        private void BuildHamletTab(RectTransform tab)
        {
            var left = Column(tab, "Buildings", 0.01f, 0.55f, 0.02f, 0.99f);
            Factory.CreateText(left, "BuildingTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "건물 (클릭: 업그레이드)";
            Buildings = new PooledButtonList(Factory, Factory.CreateScrollList(left, "BuildingList"));
            _hamletMessage = Factory.CreateText(left, "HamletMessage", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 50f);

            var right = Column(tab, "Stash", 0.56f, 0.99f, 0.02f, 0.99f);
            Factory.CreateText(right, "StashTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 30f).text = "장신구 보관함 (클릭: 판매)";
            Stash = new PooledButtonList(Factory, Factory.CreateScrollList(right, "StashList"));
        }

        private void AddStepperRow(Transform parent, string label, Action<int> changed)
        {
            var row = Factory.CreateHorizontal(parent, label + "Row");
            AddButton(row, label + " -", () => changed(-1));
            AddButton(row, label + " +", () => changed(1));
        }

        public void ShowTab(TownTab tab)
        {
            _heroTab.gameObject.SetActive(tab == TownTab.Heroes);
            _hamletTab.gameObject.SetActive(tab == TownTab.Hamlet);
        }

        public void SetHeader(string text) => _header.text = text;
        public void SetDetails(string text) => _details.text = text;
        public void SetSupplies(string text) => _supplies.text = text;
        public void SetMessage(string text) => _message.text = text;
        public void SetHamletMessage(string text) => _hamletMessage.text = text;
    }
}
