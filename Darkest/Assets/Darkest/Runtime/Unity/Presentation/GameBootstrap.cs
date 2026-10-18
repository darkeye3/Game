using System;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using Darkest.Unity.Data;
using Darkest.Unity.Debugging;
using Darkest.Unity.Infrastructure;
using Darkest.Unity.Presentation.Audio;
using Darkest.Unity.Presentation.Presenters;
using UnityEngine;

namespace Darkest.Unity.Presentation
{
    /// <summary>
    /// Composition Root. 의존성을 여기서 한 번에 조립(간단 DI)하고, 이후에는 각자 인터페이스로만 대화한다.
    /// 씬에 이 컴포넌트 하나만 있으면 된다. (전역 싱글톤 없음)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Data (비워두면 코드 더미 콘텐츠/기본 밸런스 사용)")]
        [SerializeField] private ContentDatabaseSO content;
        [SerializeField] private BalanceConfigSO balance;

        [Header("Reproducibility")]
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        [Header("Sound (Resources/Darkest/Sounds/<SoundId> 파일이 없으면 더미 소리)")]
        [SerializeField] private bool muteSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.6f;

        [Header("Debug")]
        [SerializeField] private LogLevel logLevel = LogLevel.Info;
        [SerializeField] private bool loadSaveOnStart = true;
        [SerializeField] private string saveFileName = "darkest_save.txt";
        [SerializeField] private GameDebugView debugView;

        private IGameLogger _logger;
        private SceneScaffold _scaffold;
        private SaveGateway _saves;
        private ScreenRouter _router;
        private EventLogPresenter _logPresenter;
        private BattleAnimationPresenter _animationPresenter;
        private ISoundPlayer _sound;
        private SoundPresenter _soundPresenter;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            _logger = new UnityGameLogger(logLevel);
            _scaffold = new SceneScaffold(transform, new PlaceholderSpriteProvider(_logger), _logger);
            _saves = new SaveGateway(new FileSaveRepository(saveFileName, _logger), new LineSaveSerializer(), _logger);
            if (debugView == null) debugView = gameObject.AddComponent<GameDebugView>();
            _sound = new AudioSourceSoundPlayer(gameObject.AddComponent<AudioSource>(), new ResourcesSoundLibrary(_logger),
                soundVolume, muteSound, _logger);

            StartSession(loadSaveOnStart);
        }

        private void OnDestroy() => DisposeSession();

        private void StartSession(bool tryLoad)
        {
            DisposeSession();

            int seed = useRandomSeed ? Environment.TickCount : fixedSeed;
            var random = new SeededRandom(seed);
            var events = new EventBus(_logger);
            var catalog = CreateCatalog();
            var config = balance != null ? balance.CreateConfig() : new GameConfig();

            Session = tryLoad && _saves.TryLoad(out var save)
                ? GameSession.Load(config, catalog, random, events, _logger, save)
                : GameSession.CreateNew(config, catalog, random, events, _logger);

            _scaffold.Log.Clear();
            _logPresenter = new EventLogPresenter(events, _scaffold.Log);
            _animationPresenter = new BattleAnimationPresenter(events, _scaffold.Animations);
            _soundPresenter = new SoundPresenter(events, _scaffold.Animations, _sound);
            _router = new ScreenRouter(events, _logger);
            _router.Register(_scaffold.Town, new TownPresenter(Session, _scaffold.Town, _saves, OnNewGameRequested));
            _router.Register(_scaffold.Dungeon, new DungeonPresenter(Session, _scaffold.Dungeon, _saves));
            _router.Register(_scaffold.Battle, new BattlePresenter(Session, _scaffold.Battle, _scaffold.Stage, _animationPresenter));
            _router.Register(_scaffold.Result, new ResultPresenter(Session, _scaffold.Result, _saves));
            _router.Show(Session.Mode);

            debugView.Bind(Session);
            _logger.Info("Bootstrap", "게임 시작 seed=" + seed + " (Inspector에서 fixedSeed로 재현 가능)");
        }

        private IContentCatalog CreateCatalog()
        {
            if (content == null)
            {
                _logger.Warn("Bootstrap", "ContentDatabase 미지정 → 코드 더미 콘텐츠(DefaultContent) 사용");
                return DefaultContent.Create();
            }
            return content.BuildCatalog();
        }

        private void OnNewGameRequested()
        {
            _saves.Delete();
            StartSession(false);
        }

        private void DisposeSession()
        {
            _router?.Dispose();
            _logPresenter?.Dispose();
            _animationPresenter?.Dispose();
            _soundPresenter?.Dispose();
            _soundPresenter = null;
            _router = null;
            _logPresenter = null;
            _animationPresenter = null;
            Session = null;
        }

        [ContextMenu("Delete Save File")]
        private void DeleteSave() => new FileSaveRepository(saveFileName, NullLogger.Instance).Delete();
    }
}
