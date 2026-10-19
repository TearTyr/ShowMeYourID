using System;

namespace Hollowbrook.Core
{
    public struct GameEvent
    {
        public string Id;
        public object Data;   // typed payload (use Payload<T>())
        public float Value;   // numeric payload (sanity, timer, volume…)
        public string Text;   // text payload (caption, objective, sfx key…)

        public T Payload<T>() => Data is T t ? t : default;

        public static GameEvent Of(string id) => new GameEvent { Id = id };
        public static GameEvent Of(string id, object data) => new GameEvent { Id = id, Data = data };
        public static GameEvent Of(string id, float value) => new GameEvent { Id = id, Value = value };
        public static GameEvent Of(string id, string text) => new GameEvent { Id = id, Text = text };
        public static GameEvent Of(string id, object data, float value, string text) =>
            new GameEvent { Id = id, Data = data, Value = value, Text = text };

        public override string ToString() => "GameEvent(" + Id + ")";
    }

    public sealed class EventSubscription : IDisposable
    {
        private EventBus _bus;
        private readonly string _id;
        private readonly Action<GameEvent> _handler;

        internal EventSubscription(EventBus bus, string id, Action<GameEvent> handler)
        { _bus = bus; _id = id; _handler = handler; }

        public void Dispose()
        {
            var b = _bus;
            _bus = null; // idempotent
            if (b != null) b.Remove(_id, _handler);
        }
    }

    public sealed class EventBus
    {
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Action<GameEvent>>> _subs
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Action<GameEvent>>>();

        public EventSubscription Subscribe(string id, Action<GameEvent> handler)
        {
            if (string.IsNullOrEmpty(id) || handler == null) return null;
            if (!_subs.TryGetValue(id, out var list))
            {
                list = new System.Collections.Generic.List<Action<GameEvent>>();
                _subs[id] = list;
            }
            list.Add(handler);
            return new EventSubscription(this, id, handler);
        }

        internal void Remove(string id, Action<GameEvent> handler)
        {
            if (_subs.TryGetValue(id, out var list)) list.Remove(handler);
        }

        public void Publish(GameEvent e)
        {
            if (string.IsNullOrEmpty(e.Id)) return;
            if (!_subs.TryGetValue(e.Id, out var list) || list.Count == 0) return;
            var snapshot = list.ToArray();
            foreach (var h in snapshot)
            {
                try { h(e); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("[EventBus] handler failed for '" + e.Id + "': " + ex);
                }
            }
        }

        public void Publish(string id) => Publish(GameEvent.Of(id));

        public void Clear() => _subs.Clear();

        public int SubscriberCount(string id) => _subs.TryGetValue(id, out var l) ? l.Count : 0;
    }
}
