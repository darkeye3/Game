using System.IO;
using Darkest.Domain.Core;
using Darkest.Domain.Save;
using UnityEngine;

namespace Darkest.Unity.Infrastructure
{
    /// <summary>ISaveRepository → 로컬 파일 Adapter. 경로: Application.persistentDataPath/fileName</summary>
    public sealed class FileSaveRepository : ISaveRepository
    {
        private const string LogCategory = "Save";
        private readonly string _path;
        private readonly IGameLogger _logger;

        public FileSaveRepository(string fileName, IGameLogger logger)
        {
            _path = Path.Combine(Application.persistentDataPath, Guard.NotBlank(fileName, nameof(fileName)));
            _logger = logger ?? NullLogger.Instance;
        }

        public string FilePath => _path;

        public bool TryLoad(out string text)
        {
            text = null;
            if (!File.Exists(_path)) return false;
            try
            {
                text = File.ReadAllText(_path);
                return true;
            }
            catch (IOException e)
            {
                // 파일 잠김 등: 에러 로그 + "세이브 없음"으로 안전하게 진행.
                _logger.Error(LogCategory, "세이브 읽기 실패: " + _path + " / " + e.Message);
                return false;
            }
        }

        public void Save(string text)
        {
            // 임시 파일에 먼저 쓰고 교체 → 저장 도중 종료돼도 기존 세이브가 깨지지 않는다.
            string temp = _path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temp, _path);
            _logger.Info(LogCategory, "저장 완료: " + _path);
        }

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
