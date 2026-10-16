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
            // 배경을 투명하게 → 위쪽 절반에 월드 스테이지가 보인다.
            Root.GetComponent<Image>().color = Color.clear;

            var top = Factory.CreatePanel(root, "HeaderBar", UiTheme.Panel, new Vector2(0.01f, 0.92f), new Vector2(0.99f, 0.99f));
            _header = Factory.CreateText(Factory.CreateVertical(top, "HeaderLayout"), "Header", UiTheme.BodyFontSize, TextAnchor.MiddleCenter);

            var heroes = Column(root, "Heroes", 0.01f, 0.25f, 0.02f, 0.45f);
            _heroes = Factory.CreateText(heroes, "HeroStatus", UiTheme.SmallFontSize);

            var skills = Column(root, "Skills", 0.26f, 0.50f, 0.02f, 0.45f);
            Factory.CreateText(skills, "SkillTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 28f).text = "스킬";
            _skills = new PooledButtonList(Factory, Factory.CreateVertical(skills, "SkillList"));
            var row = Factory.CreateHorizontal(skills, "MoveRow");
            AddButton(row, "◀ 전진", () => ForwardClicked?.Invoke());
            AddButton(row, "후퇴 ▶", () => BackwardClicked?.Invoke());
            var row2 = Factory.CreateHorizontal(skills, "PassRow");
            AddButton(row2, "대기", () => PassClicked?.Invoke());
            AddButton(row2, "자동", () => AutoClicked?.Invoke());

            var targets = Column(root, "Targets", 0.51f, 0.74f, 0.02f, 0.45f);
            Factory.CreateText(targets, "TargetTitle", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 28f).text = "대상";
            _targets = new PooledButtonList(Factory, Factory.CreateVertical(targets, "TargetList"));
            _message = Factory.CreateText(targets, "Message", UiTheme.SmallFontSize, TextAnchor.MiddleLeft, 60f);

            var monsters = Column(root, "Monsters", 0.75f, 0.99f, 0.02f, 0.45f);
            _monsters = Factory.CreateText(monsters, "MonsterStatus", UiTheme.SmallFontSize);
        }

        public void SetHeader(string text) => _header.text = text;
        public void SetHeroes(string text) => _heroes.text = text;
        public void SetMonsters(string text) => _monsters.text = text;
        public void SetMessage(string text) => _message.text = text;
    }
}
