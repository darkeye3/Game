using System;
using System.IO;
using Darkest.Domain.Core;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 저장/불러오기 흐름(직렬화 + 저장소) 조합. 실패 사유를 숨기지 않되, 게임이 멈추지 않도록
    /// 예상 가능한 실패(IO, 손상된 세이브)만 구체적으로 잡아 로그와 함께 결과로 돌려준다.
    /// </summary>
    public sealed class SaveGateway
    {
        private const string LogCategory = "Save";

        private readonly ISaveRepository _repository;
        private readonly ISaveSerializer _serializer;
        private readonly IGameLogger _logger;

        public SaveGateway(ISaveRepository repository, ISaveSerializer serializer, IGameLogger logger)
        {
            _repository = Guard.NotNull(repository, nameof(repository));
            _serializer = Guard.NotNull(serializer, nameof(serializer));
            _logger = logger ?? NullLogger.Instance;
        }

        /// <returns>실패 사유. 성공 시 null.</returns>
        public string TrySave(GameSaveData data)
        {
            Guard.NotNull(data, nameof(data));
            try
            {
                _repository.Save(_serializer.Serialize(data));
                return null;
            }
            catch (IOException e)
            {
                _logger.Error(LogCategory, "저장 실패(IO): " + e.Message);
                return "저장 실패: " + e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.Error(LogCategory, "저장 실패(권한): " + e.Message);
                return "저장 실패: 권한이 없습니다.";
            }
        }

        /// <returns>불러올 세이브가 있고 정상이면 true. 손상된 세이브는 에러 로그 후 false.</returns>
        public bool TryLoad(out GameSaveData data)
        {
            data = null;
            if (!_repository.TryLoad(out var text)) return false;
            try
            {
                data = _serializer.Deserialize(text);
                return true;
            }
            catch (FormatException e)
            {
                _logger.Error(LogCategory, "세이브 손상 — 새 게임으로 시작합니다: " + e.Message);
                return false;
            }
        }

        public void Delete() => _repository.Delete();
    }
}
