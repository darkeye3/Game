using UnityEngine;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>전투 스테이지의 유닛 1개(스프라이트 + 이름/HP 라벨). BattleStageView의 풀에서 재사용된다.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float LabelOffsetY = -0.25f;
        private const float LabelCharacterSize = 0.08f;
        private const int LabelFontSize = 40;

        private SpriteRenderer _renderer;
        private TextMesh _label;

        public string CombatantId { get; private set; }

        public static UnitView Create(Transform parent, Font font)
        {
            var go = new GameObject("Unit");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<UnitView>();
            view._renderer = go.AddComponent<SpriteRenderer>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, LabelOffsetY, 0f);
            view._label = labelGo.AddComponent<TextMesh>();
            view._label.font = font;
            view._label.fontSize = LabelFontSize;
            view._label.characterSize = LabelCharacterSize;
            view._label.anchor = TextAnchor.UpperCenter;
            view._label.alignment = TextAlignment.Center;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return view;
        }

        public void Bind(string combatantId, Sprite sprite, bool faceLeft)
        {
            CombatantId = combatantId;
            _renderer.sprite = sprite;
            _renderer.flipX = faceLeft;
            gameObject.SetActive(true);
        }

        public void SetLabel(string text, Color color)
        {
            _label.text = text;
            _label.color = color;
        }

        public void SetHighlight(bool active, Color highlight)
        {
            _renderer.color = active ? highlight : Color.white;
        }

        public void Release()
        {
            CombatantId = null;
            gameObject.SetActive(false);
        }
    }
}
