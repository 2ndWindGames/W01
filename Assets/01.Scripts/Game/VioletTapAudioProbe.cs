#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>Editor QA only: records the peak on the audio thread without changing samples.</summary>
public sealed class VioletTapAudioProbe : MonoBehaviour
{
    public volatile float Peak;

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float peak = Peak;
        foreach (float sample in data) peak = Math.Max(peak, Math.Abs(sample));
        Peak = peak;
    }
}
#endif
