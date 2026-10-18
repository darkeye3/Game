using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Game;
using Darkest.Domain.Save;

namespace Darkest.Tests
{
    /// <summary>GameSession을 공개 API로만 만들고 저장/불러오기 왕복시키는 헬퍼.</summary>
    internal static class SessionFixtures
    {
        public static GameSession New(int seed, GameConfig config = null) =>
            GameSession.CreateNew(config ?? new GameConfig(), DefaultContent.Create(), new SeededRandom(seed), new EventBus(), NullLogger.Instance);

        /// <summary>직렬화 → 역직렬화를 거쳐 불러온다(포맷까지 검증).</summary>
        public static GameSession RoundTrip(GameSaveData data, int seed, GameConfig config = null)
        {
            var serializer = new LineSaveSerializer();
            var parsed = serializer.Deserialize(serializer.Serialize(data));
            return GameSession.Load(config ?? new GameConfig(), DefaultContent.Create(), new SeededRandom(seed), new EventBus(),
                NullLogger.Instance, parsed);
        }

        /// <summary>새 게임을 저장하고 보관함에 장신구를 넣은 채로 불러온 세션(마을 내부 API 없이 장신구를 주기 위함).</summary>
        public static GameSession WithStash(int seed, params string[] trinketIds)
        {
            var data = New(seed).CaptureSave();
            data.TrinketStash.AddRange(trinketIds);
            return RoundTrip(data, seed);
        }
    }
}
