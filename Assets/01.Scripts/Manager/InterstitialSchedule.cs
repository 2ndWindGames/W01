namespace SWGUnity2DCore.Manager
{
    public sealed class InterstitialSchedule
    {
        public int CompletedRounds { get; private set; }
        private double m_LastShown = double.NegativeInfinity;
        public void RecordRound() => CompletedRounds++;
        public bool CanShow(double now) => CompletedRounds >= 3 && now - m_LastShown >= 180;
        public void MarkShown(double now)
        {
            CompletedRounds = 0;
            m_LastShown = now;
        }
    }
}
