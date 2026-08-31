using System;

namespace MiniGameKit
{
    /// <summary>
    /// A small, owner-driven countdown. It has no MonoBehaviour or global clock.
    /// </summary>
    public sealed class CountdownTimer
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }

        public event Action Completed;

        public void Start(float duration)
        {
            Duration = Math.Max(0f, duration);
            Remaining = Duration;
            IsComplete = false;
            IsRunning = Duration > 0f;

            if (!IsRunning)
            {
                Complete();
            }
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            Remaining = Math.Max(0f, Remaining - Math.Max(0f, deltaTime));
            if (Remaining <= 0f)
            {
                Complete();
            }
        }

        private void Complete()
        {
            if (IsComplete)
            {
                return;
            }

            IsRunning = false;
            IsComplete = true;
            Remaining = 0f;
            Completed?.Invoke();
        }
    }
}
