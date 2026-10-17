using System;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>
    /// 전투 UI(화면 아래쪽). 위쪽은 BattleStageView가 월드 스프라이트로 진형을 그린다.
    /// </summary>
    public sealed class BattleView : ScreenView
    {
        private const float PanelBottom = 0.02f;
        private const float TitleHeight = 26f;
        private const float MessageHeight = 50f;

        private Text _header;
        private Text _heroes;
        private Text _monsters;
        private Text _message;
        private PooledButtonList _skills;
        private PooledButtonList _targets;

        public event Action ForwardClicked;
        public event Action BackwardClicked;
        public event Action PassClicked;
        public event Action AutoClicked;

        public PooledButtonList Skills => _skills;
        public PooledButtonList Targets => _targets;

        protected override void BuildContent(RectTransform root)
        {
            // 배경을 투명하게 → 패널 사이 띠에 월드 스테이지가 보인다(StageCameraFitter가 그 띠에 맞춰 카메라를 잡는다).
            Root.GetComponent<Image>().color = Color.clear;

            var top = Factory.CreatePanel(root, "HeaderBar", UiTheme.Panel, new Vector2(0.01f, UiTheme.BattleHeaderBottom), new Vector2(0.99f, 0.99f));
            _header = Factory.CreateText(Factory.CreateVertical(top, "HeaderLayout"), "Header", UiTheme.BodyFontSize, TextAnchor.MiddleCenter);

            float panelTop = UiTheme.BattlePanelTop;
            var heroes = Column(root, "Heroes", 0.01f, 0.25f, PanelBottom, panelTop);
            _heroes = Factory.CreateText(heroes, "HeroStatus", UiTheme.SmallFontSize);

            var skills = Column(root, "Skills", 0.26f, 0.50f, PanelBottom, panelTop);
            Factory.CreateText(skills, "SkillTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, TitleHeight).text = "스킬";
            _skills = new PooledButtonList(Factory, Factory.CreateScrollList(skills, "SkillList"));
            // 이동/대기/자동은 한 줄로 묶어 스킬 목록에 세로 공간을 더 준다.
            var row = Factory.CreateHorizontal(skills, "ActionRow");
            AddButton(row, "◀ 전진", () => ForwardClicked?.Invoke());
            AddButton(row, "후퇴 ▶", () => BackwardClicked?.Invoke());
            AddButton(row, "대기", () => PassClicked?.Invoke());
            AddButton(row, "자동", () => AutoClicked?.Invoke());

            var targets = Column(root, "Targets", 0.51f, 0.74f, PanelBottom, panelTop);
            Factory.CreateText(targets, "TargetTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, TitleHeight).text = "대상";
            _targets = new PooledButtonList(Factory, Factory.CreateScrollList(targets, "TargetList"));
            _message = Factory.CreateText(targets, "Message", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, MessageHeight);

            var monsters = Column(root, "Monsters", 0.75f, 0.99f, PanelBottom, panelTop);
            _monsters = Factory.CreateText(monsters, "MonsterStatus", UiTheme.SmallFontSize);
        }

        public void SetHeader(string text) => _header.text = text;
        public void SetHeroes(string text) => _heroes.text = text;
        public void SetMonsters(string text) => _monsters.text = text;
        public void SetMessage(string text) => _message.text = text;
    }
}
