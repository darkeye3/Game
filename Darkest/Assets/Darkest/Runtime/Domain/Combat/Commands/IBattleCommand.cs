using Darkest.Domain.Combat.Flow;

namespace Darkest.Domain.Combat.Commands
{
    /// <summary>
    /// 전투 행동 Command. 플레이어 입력과 AI 결정이 같은 형태로 실행되고, 실행 기록(History)이 남아
    /// 리플레이/버그 재현에 쓸 수 있다.
    /// </summary>
    public interface IBattleCommand
    {
        Combatant Actor { get; }

        /// <returns>실행 불가 사유. 가능하면 null.</returns>
        string Validate(BattleContext context);

        void Execute(BattleContext context);

        string Describe();
    }
}
