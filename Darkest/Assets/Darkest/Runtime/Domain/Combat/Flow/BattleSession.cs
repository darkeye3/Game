using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;

namespace Darkest.Domain.Combat.Flow
{
    /// <summary>
    /// 전투 1회의 외부 진입점(Facade). Presenter는 Start/Submit만 호출하고, 결과는 이벤트로 받는다.
    /// </summary>
    public sealed class BattleSession
    {
        private const string LogCategory = "Battle";

        /// <summary>자동 진행 상태 전이 안전 한도(한 번의 Run에서). 상태 머신 버그로 인한 무한 루프 차단.</summary>
        private const int MaxAutoTransitions = 10000;

        private readonly BattleStateSet _states = new BattleStateSet();
        private BattleState _state;

        public BattleSession(Formation heroes, Formation monsters, BattleServices services)
        {
            Context = new BattleContext(heroes, monsters, services);
        }

        public BattleContext Context { get; }
        public BattlePhase Phase => _state?.Phase ?? BattlePhase.NotStarted;
        public bool IsOver => Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat;
        public bool IsAwaitingHeroCommand => Phase == BattlePhase.AwaitingHeroCommand;
        public Combatant CurrentActor => Context.CurrentActor;

        public void Start()
        {
            Guard.That(_state == null, "이미 시작된 전투입니다.");
            Guard.That(!Context.Heroes.IsEmpty && !Context.Monsters.IsEmpty, "양 진영 모두 1명 이상 필요합니다.");

            Context.Logger.Info(LogCategory, "전투 시작 seed=" + Context.Random.Seed);
            Context.Events.Publish(new BattleStarted(Context.Heroes, Context.Monsters));
            _state = _states.RoundStart;
            RunUntilWaiting();
        }

        /// <returns>실패 사유. 성공 시 null.</returns>
        public string Submit(IBattleCommand command)
        {
            if (command == null) return "명령이 없습니다.";
            if (!IsAwaitingHeroCommand) return "지금은 명령을 받을 수 없습니다: " + Phase;
            if (command.Actor != Context.CurrentActor) return "현재 차례가 아닙니다: " + command.Actor.DisplayName;

            string error = command.Validate(Context);
            if (error != null)
            {
                Context.Logger.Warn(LogCategory, "명령 거부: " + command.Describe() + " / " + error);
                return error;
            }

            Context.Logger.Info(LogCategory, "명령: " + command.Describe());
            command.Execute(Context);
            Context.Record(command);
            _state = _states.CheckEnd;
            RunUntilWaiting();
            return null;
        }

        private void RunUntilWaiting()
        {
            for (int i = 0; i < MaxAutoTransitions; i++)
            {
                if (_state.IsWaiting) return;
                if (Context.Logger.IsEnabled(LogLevel.Verbose)) Context.Logger.Verbose(LogCategory, "상태: " + _state.Phase);
                _state = _state.Next(Context, _states);
            }
            throw new System.InvalidOperationException("전투 상태 머신이 대기 상태에 도달하지 못했습니다.");
        }
    }
}
