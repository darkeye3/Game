using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Progression;
using Darkest.Domain.Save;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Game
{
    /// <summary>
    /// GameSession의 저장/불러오기 연결부: 마을·원정 매퍼와 난수 상태를 한 곳에서 다룬다.
    /// 난수 상태까지 담아 두면 불러온 뒤의 결과도 저장 직후와 똑같이 이어진다(버그 재현용).
    /// </summary>
    internal sealed class SessionPersistence
    {
        private const string LogCategory = "Save";

        private readonly SaveMapper _townMapper;
        private readonly ExpeditionSaveMapper _expeditionMapper;
        private readonly IRandom _random;
        private readonly IGameLogger _logger;

        public SessionPersistence(GameConfig config, IContentCatalog catalog, CombatantFactory factory, StressService stress,
            HeroProgression progression, IRandom random, IEventBus events, IGameLogger logger)
        {
            _random = Guard.NotNull(random, nameof(random));
            _logger = logger ?? NullLogger.Instance;
            _townMapper = new SaveMapper(catalog, factory, stress, progression, _logger);
            _expeditionMapper = new ExpeditionSaveMapper(config.Expedition, config.Combat, stress, catalog, random, events, _logger);
        }

        /// <param name="expedition">탐험 중 저장이면 그 원정, 마을 저장이면 null.</param>
        public GameSaveData Capture(Town town, Expedition expedition)
        {
            var data = _townMapper.Capture(town);
            if (expedition != null) data.Expedition = ExpeditionSaveMapper.Capture(expedition);
            if (_random is IRestorableRandom restorable) data.RandomState = restorable.State;
            return data;
        }

        /// <summary>다른 무엇보다 먼저 호출해야 이후 뽑기(모집 후보 등)가 저장 당시와 같은 순서로 나온다.</summary>
        public void RestoreRandom(GameSaveData save)
        {
            if (!save.RandomState.HasValue) return;
            if (_random is IRestorableRandom restorable)
            {
                restorable.RestoreState(save.RandomState.Value);
                _logger.Info(LogCategory, "난수 상태 복원");
            }
            else _logger.Warn(LogCategory, "난수 상태를 복원할 수 없는 IRandom 구현입니다: " + _random.GetType().Name);
        }

        public Town RestoreTown(GameSaveData save) => _townMapper.Restore(save);

        /// <returns>복원된 원정. 원정 저장이 없거나 복원에 실패하면 null(실패는 매퍼가 로그를 남겼다).</returns>
        public Expedition RestoreExpedition(GameSaveData save, Town town) =>
            save.Expedition == null ? null : _expeditionMapper.Restore(save.Expedition, town);
    }
}
