using System;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class TrackInfoWidget : SpotifyServiceListener
{
    public string TrackId;
    public Text Name, Key, Tempo;

    private FullTrack track;
    private TrackAudioFeatures audioFeatures;

    protected override async void OnSpotifyConnectionChanged(SpotifyClient client)
    {
        base.OnSpotifyConnectionChanged(client);

        if (string.IsNullOrEmpty(this.TrackId))
        {
            var playback = await client.Player.GetCurrentPlayback();
            if (playback.Item.Type == ItemType.Track)
            {
                this.track = playback.Item as FullTrack;
                this.TrackId = this.track.Id;
            }
        }
        
        this.audioFeatures = client == null ? null : await client.Tracks.GetAudioFeatures(this.TrackId);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (this.audioFeatures != null)
        {
            UpdateTextElement(this.Name, $"Name: {this.track.Name}");
            UpdateTextElement(this.Key, $"Key: {this.audioFeatures.Key}");
            UpdateTextElement(this.Tempo, $"Tempo: {this.audioFeatures.Tempo}");
        }
        else
        {
            UpdateTextElement(this.Key, string.Empty);
            UpdateTextElement(this.Tempo, string.Empty);
        }
    }

    private void UpdateTextElement(Text element, string content)
    {
        if (element != null)
        {
            element.text = content;
        }
    }
}
