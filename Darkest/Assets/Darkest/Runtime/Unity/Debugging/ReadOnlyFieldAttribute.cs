using UnityEngine;

namespace Darkest.Unity.Debugging
{
    /// <summary>인스펙터에 값은 보여주되 편집은 막는다(디버그 표시용). 드로어는 Editor 어셈블리에 있다.</summary>
    public sealed class ReadOnlyFieldAttribute : PropertyAttribute { }
}
