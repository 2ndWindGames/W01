using System;
using UnityEngine;

namespace _01.Scripts.Game
{
    public enum GameFlowState
    {
        Ready,
        Playing,
        SecondPulseOffer,
        Result
    }

    /// <summary>
    /// Owns only the large lifecycle of a small game round.
    /// Game-specific score, spawn and rule logic stays outside this class.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public GameFlowState State { get; private set; } = GameFlowState.Ready;

        public event Action<GameFlowState> StateChanged;

        public void StartGame()
        {
            if (State != GameFlowState.Ready)
            {
                return;
            }

            SetState(GameFlowState.Playing);
        }

        public void FinishGame()
        {
            if (State != GameFlowState.Playing && State != GameFlowState.SecondPulseOffer)
            {
                return;
            }

            SetState(GameFlowState.Result);
        }

        public void OfferSecondPulse()
        {
            if (State != GameFlowState.Playing)
            {
                return;
            }

            SetState(GameFlowState.SecondPulseOffer);
        }

        public void ResumeFromSecondPulse()
        {
            if (State != GameFlowState.SecondPulseOffer)
            {
                return;
            }

            SetState(GameFlowState.Playing);
        }

        public void Retry()
        {
            if (State != GameFlowState.Result)
            {
                return;
            }

            SetState(GameFlowState.Ready);
        }

        private void SetState(GameFlowState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);
        }
    }
}
