using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EntityStatistics
{
    public class Timer
    {
        float end;
        public bool Expired => Time.time > end;
        public float SecondsLeft => end - Time.time;
        public Timer(float duration)
        {
            end = Time.time + duration;
        }
    }

    public abstract class Modifier : IDisposable, IGridItem
    {
        public enum Method
        {
            Addative,
            Multiplicative
        }

        
        public Method method;
        public EntityStats.Stat StatType;
        public float value;
        public event Action OnDispose;

        protected Sprite display;

        public bool isDisplayable => display != null;
        public bool Expired => timer?.Expired ?? false;
        public bool Timed => timer != null;

        Timer timer;
        protected Modifier(EntityStats.Stat StatType, float value, float Duration, Sprite display)
        {
            this.StatType = StatType;
            this.value = value;
            this.display = display;

            if (Duration > 0)
            {
                timer = new Timer(Duration);
            }
        }

        public void Handle(Query query)
        {
            if (query.type == StatType)
            {
                HandleImpl(query);
            }
        }

        protected abstract void HandleImpl(Query query);

        public void Dispose()
        {
            OnDispose?.Invoke();
        }

        public Sprite GetSprite() => display;

        public string GetString() => null;

        public Color GetColor() => Color.white;

        public (string, string) GetTooltipString() => (StatType.ToString().SplitCamelCase(), timer is null ? null : Mathf.CeilToInt(timer.SecondsLeft).ToString() + "s");
    }

    public class BasicModifier : Modifier
    {

        public BasicModifier(Info info) : base(info.StatType, info.value, info.Duration, info.display)
        {
            method = info.method;
        }

        protected override void HandleImpl(Query query)
        {
            query.value = method switch
            {
                Method.Addative => query.value + value,
                Method.Multiplicative => query.value * value,
                _ => throw new NotImplementedException()
            };
        }

        [Serializable]
        public struct Info
        {
            [Header("Display")]
            public Sprite display;
            [Header("Info")]
            public Method method;
            public EntityStats.Stat StatType;
            public float value;
            public float Duration;

            public override string ToString()
            {
                var percent = method == Method.Multiplicative;
                var val = percent ? 100 * value : value;
                var durationString = Duration == 0 ? "" : $" {Duration}s";
                return $"{StatType.ToString().SplitCamelCase()}: {(value > 0 && percent ? "+" : "")}{val}{(percent ? "%":"")}{durationString}";
            }
        }
    }
}
