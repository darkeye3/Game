using System;
using System.Collections.Generic;

namespace Darkest.Domain.Core
{
    /// <summary>도메인 이벤트 표식. 이벤트는 "이미 일어난 사실"만 담는 불변 객체로 만든다.</summary>
    public interface IDomainEvent { }

    /// <summary>
    /// 도메인 → 프레젠테이션 단방향 알림 통로(Observer).
    /// 도메인은 누가 듣는지 모르고, View/Presenter는 도메인 내부를 직접 들여다보지 않는다.
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T domainEvent) where T : IDomainEvent;
        IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent;
    }

    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly IGameLogger _logger;

        public EventBus(IGameLogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public void Publish<T>(T domainEvent) where T : IDomainEvent
        {
            if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
            if (_logger.IsEnabled(LogLevel.Verbose)) _logger.Verbose("EventBus", domainEvent.ToString());

            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // 핸들러 안에서 구독/해제가 일어나도 안전하도록 인덱스 순회 + 해제된(null) 슬롯은 나중에 정리.
            int count = list.Count; // 발행 중 추가된 구독자는 다음 이벤트부터 받는다.
            bool hasEmptySlot = false;
            for (int i = 0; i < count; i++)
            {
                if (list[i] is Action<T> handler) handler(domainEvent);
                else hasEmptySlot = true;
            }
            if (hasEmptySlot) list.RemoveAll(IsEmptySlot);
        }

        private static readonly Predicate<Delegate> IsEmptySlot = d => d == null;

        public IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(typeof(T), list);
            }
            list.Add(handler);
            return new Subscription(list, handler);
        }

        private sealed class Subscription : IDisposable
        {
            private List<Delegate> _list;
            private readonly Delegate _handler;

            public Subscription(List<Delegate> list, Delegate handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_list == null) return;
                int index = _list.IndexOf(_handler);
                if (index >= 0) _list[index] = null; // 순회 중 해제 대비: 즉시 제거하지 않고 비운다.
                _list = null;
            }
        }
    }
}
