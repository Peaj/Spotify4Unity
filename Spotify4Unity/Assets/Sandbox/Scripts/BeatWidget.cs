using System;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class BeatWidget : SpotifyPlayerListener
{
    public Text TimeLabel, BeatTimeLabel, BPMLabel;
    public AnimationCurve AnimationCurve;
    public Color DefaultColor;
    public Color HighlightColor;
    public float MaxScale = 0.2f;

    private FullTrack track;
    private TrackAudioAnalysis audioAnalysis;
    private CurrentlyPlayingContext playback;
    private SpotifyClient client;

    private double nextFetch = 0f;
    private double runTime = 0f;
    private bool fetching = false;

    private CurrentlyPlayingContext lastContext;
    private string debugBPM;
    
    protected override async void OnSpotifyConnectionChanged(SpotifyClient client)
    {
        base.OnSpotifyConnectionChanged(client);

        this.client = client;
        this.runTime = 0f;
    }

    protected override async void PlayingItemChanged(IPlayableItem item)
    {
        this.track = item as FullTrack;
        this.audioAnalysis = this.client == null ? null : await this.client.Tracks.GetAudioAnalysis(this.track.Id);
    }

    private async void Update()
    {
        if (this.client == null) return;

        var newContext = GetCurrentContext();
        if (this.lastContext != newContext)
        {
            this.lastContext = newContext;
            float newRuntime = newContext.ProgressMs / 1000f;
            float offset = newRuntime - (float)this.runTime;
            this.runTime = newRuntime;
            Debug.Log($"Fetch {offset}");
        }
        
        this.runTime += Time.deltaTime;
        
        this.TimeLabel.text = runTime.ToString("000.00");
        double beatTime = GetBeatTime(this.runTime);
        this.BeatTimeLabel.text = beatTime.ToString("000.00");
        this.BPMLabel.text = $"{GetBPM(this.runTime):F0} BPM";

        float t = this.AnimationCurve.Evaluate((float)beatTime);
        var segment = GetBlendedSegment(this.runTime);
        float volume = (segment != null) ? Mathf.Clamp01(DBToGain(segment.LoudnessMax)) : 1f;

        GetComponent<Image>().color = Color.Lerp(this.DefaultColor, this.HighlightColor, t);
        this.transform.localScale = Vector3.one * Mathf.Lerp(1f-this.MaxScale,1f+this.MaxScale, t * volume);
    }

    private double GetBeatTime(double runTime)
    {
        if (this.audioAnalysis == null) return 0d;
        for (int i = 0; i < this.audioAnalysis.Beats.Count; i++)
        {
            var beat = this.audioAnalysis.Beats[i];
            double startTime = beat.Start;
            double endTime = beat.Start + beat.Duration;

            if (runTime > startTime && runTime < endTime)
            {
                return i + Mathf.InverseLerp((float) startTime, (float) endTime, (float) runTime);
            }
        }

        return 0d;
    }

    private double GetBPM(double runTime)
    {
        if (this.audioAnalysis == null) return 0d;
        var sections = this.audioAnalysis.Sections;
        int count = sections.Count;
        for (int i = 0; i < count; i++)
        {
            var current = sections[i];
            
            double startTime = current.Start;
            double endTime = current.Start + current.Duration;

            if (runTime > startTime && runTime <= endTime)
            {
                var next = sections[Mathf.Min(count-1,i+1)];
                var prev = sections[Mathf.Max(0,i-1)];
                
                float t = Mathf.InverseLerp((float)startTime-prev.Duration*0.5f, (float)endTime+next.Duration*0.5f, (float)runTime);
                return Lerp3(prev.Tempo,current.Tempo,next.Tempo, t);
            }
        }
        return 0d;
    }

    private Segment GetBlendedSegment(double time)
    {
        if (this.audioAnalysis == null) return null;
        var segments = this.audioAnalysis.Segments;
        int count = segments.Count;
        for (int i = 0; i < count; i++)
        {
            var current = segments[i];
            
            double startTime = current.Start;
            double endTime = current.Start + current.Duration;

            if (runTime > startTime && runTime <= endTime)
            {
                var next = segments[Mathf.Min(count-1,i+1)];
                var prev = segments[Mathf.Max(0,i-1)];
                
                float t = Mathf.InverseLerp((float)startTime-prev.Duration*0.5f, (float)endTime+next.Duration*0.5f, (float)runTime);

                var segment = new Segment()
                {
                    Confidence = Lerp3(prev.Confidence, current.Confidence, next.Confidence, t),
                    Duration = Lerp3(prev.Duration, current.Duration, next.Duration, t),
                    LoudnessEnd = Lerp3(prev.LoudnessEnd, current.LoudnessEnd, next.LoudnessEnd, t),
                    LoudnessMax = Lerp3(prev.LoudnessMax, current.LoudnessMax, next.LoudnessMax, t),
                    LoudnessMaxTime = Lerp3(prev.LoudnessMaxTime, current.LoudnessMaxTime, next.LoudnessMaxTime, t),
                    LoudnessStart = Lerp3(prev.LoudnessStart, current.LoudnessStart, next.LoudnessStart, t),
                    Pitches = Lerp3Array(prev.Pitches.ToArray(), current.Pitches.ToArray(), next.Pitches.ToArray(), t).ToList(),
                    Start = Lerp3(prev.Start, current.Start, next.Start, t),
                    Timbre = Lerp3Array(prev.Timbre.ToArray(), current.Timbre.ToArray(), next.Timbre.ToArray(), t).ToList(),
                };
                
                return segment;
            }
        }
        return null;
    }

    private static float Lerp3(float a, float b, float c, float t)
    {
        float ta = 1f - Mathf.Clamp01(2f * t);
        float tc = Mathf.Clamp01(t - 0.5f) * 2f;
        float tb = Mathf.Clamp01(1f - ta - tc);
        return a * ta + b * tb + c * tc;
    }
    
    private static float[] Lerp3Array(float[] a, float[] b, float[] c, float t)
    {
        int length = Mathf.Min(a.Length, b.Length, c.Length);
        float[] result = new float[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = Lerp3(a[i], b[i], c[i], t);
        }
        return result;
    }

    private static float DBToGain(float db)
    {
        return Mathf.Pow(10, db / 20f);
    }
}