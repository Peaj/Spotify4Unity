using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class SpotifyPlayerController : SpotifyPlayerListener
{
    [SerializeField]
    private Image _trackIcon;

    [SerializeField]
    private Text _trackName, _artistsNames;

    [SerializeField]
    private Text _currentProgressText, _totalProgressText;
    [SerializeField]
    private Slider _currentProgressSlider;

    [SerializeField]
    private Button _playPauseButton, _previousButton, _nextButton, _shuffleButton, _repeatButton;

    [SerializeField]
    private Slider _volumeSlider;
    [SerializeField]
    private Button _muteButton;

    [SerializeField]
    private Button _addToLibraryButton;

    private bool _currentItemIsInLibrary;

    private bool _progressStartDrag = false;
    private float _progressDragNewValue = -1.0f;

    protected override void Awake()
    {
        base.Awake();

        // Listen to needed events on Awake
        this.OnPlayingItemChanged += this.PlayingItemChanged;
    }

    private void Start()
    {
        if (_playPauseButton != null)
        {
            _playPauseButton.onClick.AddListener(() => this.OnPlayPauseClicked());
        }
        if (_previousButton != null)
        {
            _previousButton.onClick.AddListener(() => this.OnPreviousClicked());
        }
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(() => this.OnNextClicked());
        }
        if (_shuffleButton != null)
        {
            _shuffleButton.onClick.AddListener(() => this.OnToggleShuffle());
        }
        if (_repeatButton != null)
        {
            _repeatButton.onClick.AddListener(() => this.OnToggleRepeat());
        }

        if (_muteButton != null)
        {
            _muteButton.onClick.AddListener(() => this.OnToggleMute());
        }

        if (_addToLibraryButton != null)
        {
            _addToLibraryButton.onClick.AddListener(() => this.OnToggleAddToLibrary());
        }

        // Configure progress slider
        if (_currentProgressSlider != null)
        {
            // Enable only whole numbers
            _currentProgressSlider.wholeNumbers = true;

            // Listen to value change on slider
            _currentProgressSlider.onValueChanged.AddListener(this.OnProgressSliderValueChanged);
            // Add EventTrigger component, listen to mouse up/down events
            EventTrigger eventTrigger = _currentProgressSlider.gameObject.AddComponent<EventTrigger>();
            // Mouse Down event
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            entry.callback.AddListener(this.OnProgressSliderMouseDown);
            eventTrigger.triggers.Add(entry);
            // Mouse Up event
            entry = new EventTrigger.Entry()
            {
                eventID = EventTriggerType.PointerUp
            };
            entry.callback.AddListener(this.OnProgressSliderMouseUp);
            eventTrigger.triggers.Add(entry);
        }
    }

    private void Update()
    {
        CurrentlyPlayingContext context = GetCurrentContext();
        if (context != null)
        {
            // Update current position to context position when user is not dragging
            if (_currentProgressText != null && !_progressStartDrag)
            {
                _currentProgressText.text = S4UUtility.MsToTimeString(context.ProgressMs);
            }

            // Update Volume slider
            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0;
                _volumeSlider.maxValue = 100;
                _volumeSlider.value = context.Device.VolumePercent.Value;
            }

            FullTrack track = context.Item as FullTrack;
            if (track != null)
            {
                if (_totalProgressText != null)
                {
                    _totalProgressText.text = S4UUtility.MsToTimeString(track.DurationMs);
                }
                if (_currentProgressSlider != null)
                {
                    _currentProgressSlider.minValue = 0;
                    _currentProgressSlider.maxValue = track.DurationMs;

                    // Update position when user is not dragging slider
                    if (!_progressStartDrag)
                        _currentProgressSlider.value = context.ProgressMs;
                }
            }
        }
    }

    private async void PlayingItemChanged(IPlayableItem newPlayingItem)
    {
        if (newPlayingItem == null)
        {
            // No new item playing, timeout
            UpdatePlayerInfo("-", "-", "");
        }
        else
        {
            if (newPlayingItem.Type == ItemType.Track)
            {
                if (newPlayingItem is FullTrack track)
                {
                    string allArtists = S4UUtility.ArtistsToSeparatedString(", ", track.Artists);
                    string firstArtUrl = track.Album.Images.FirstOrDefault()?.Url;
                    UpdatePlayerInfo(track.Name, allArtists, firstArtUrl);

                    // Make request to see if track is part of user's library
                    var client = SpotifyService.Instance.GetSpotifyClient();
                    LibraryCheckTracksRequest request = new LibraryCheckTracksRequest(new List<string>() { track.Id });
                    var result = await client.Library.CheckTracks(request);
                    if (result.Count > 0)
                    {
                        SetLibraryBtnIsLiked(result[0]);
                    }
                }
            }
            else if (newPlayingItem.Type == ItemType.Episode)
            {
                if (newPlayingItem is FullEpisode episode)
                {
                    string creators = episode.Show.Publisher;
                    string firstArtUrl = episode.Images.FirstOrDefault()?.Url;
                    UpdatePlayerInfo(episode.Name, creators, firstArtUrl);
                }
            }
        }
    }

    // Updates the left hand side of the player (Artwork, track name, artists)
    private void UpdatePlayerInfo(string trackName, string artistNames, string artUrl)
    {
        if (_trackName != null)
        {
            _trackName.text = trackName;
        }
        if (_artistsNames != null)
        {
            _artistsNames.text = artistNames;
        }
        if (_trackIcon != null)
        {
            // Disable icon if no url given
            _trackIcon.gameObject.SetActive(!string.IsNullOrEmpty(artUrl));
            // Load sprite from url
            StartCoroutine(S4UUtility.LoadImageFromUrl(artUrl, (loadedSprite) =>
            {
                _trackIcon.sprite = loadedSprite;
            }));
        }
    }
    
    private void OnPlayPauseClicked()
    {
        // Get current context & client, check if null
        CurrentlyPlayingContext context = GetCurrentContext();
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        if (context != null && client != null)
        {
            if (context.IsPlaying)
            {
                client.Player.PausePlayback();
            }
            else
            {
                client.Player.ResumePlayback();
            }
        }
    }

    private void OnPreviousClicked()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        if(client != null)
        {
            client.Player.SkipPrevious();
        }
    }

    private void OnNextClicked()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        if (client != null)
        {
            client.Player.SkipNext();
        }
    }

    private void OnToggleShuffle()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        if (client != null)
        {
            // get current shuffle state
            bool currentShuffleState = GetCurrentContext().ShuffleState;
            // Create request, invert state
            PlayerShuffleRequest request = new PlayerShuffleRequest(!currentShuffleState);

            client.Player.SetShuffle(request);
        }
    }

    private void OnToggleRepeat()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        CurrentlyPlayingContext context = this.GetCurrentContext();
        if(client != null && context != null)
        {
            // Get current shuffle state
            string currentShuffleState = context.RepeatState;

            // Determine next shuffle state
            PlayerSetRepeatRequest.State newState = PlayerSetRepeatRequest.State.Off;
            switch (currentShuffleState)
            {
                case "off":
                    newState = PlayerSetRepeatRequest.State.Track;
                    break;
                case "track":
                    newState = PlayerSetRepeatRequest.State.Context;
                    break;
                case "context":
                    newState = PlayerSetRepeatRequest.State.Off;
                    break;
                default:
                    Debug.LogError($"Unknown Shuffle State '{currentShuffleState}'");
                    break;
            }

            // Build request and send
            PlayerSetRepeatRequest request = new PlayerSetRepeatRequest(newState);
            client.Player.SetRepeat(request);
        }
    }

    private void OnToggleMute()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
        var context = GetCurrentContext();
        if (context != null && client != null)
        {
            int? volume = context.Device.VolumePercent;
            int targetVolume;
            if (volume.HasValue && volume > 0)
            {
                targetVolume = 0;
            }
            else
            {
                // Default volume when un-muting
                targetVolume = 50;
            }

            PlayerVolumeRequest request = new PlayerVolumeRequest(targetVolume);
            client.Player.SetVolume(request);
        }
    }

    private async void OnToggleAddToLibrary()
    {
        SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();

        // Get current context and check any are null
        CurrentlyPlayingContext context = this.GetCurrentContext();
        if (client != null && context != null)
        {
            List<string> ids = new List<string>();
            // Cast Item to correct type, add it's URI add make request
            if (context.Item.Type == ItemType.Track)
            {
                FullTrack track = context.Item as FullTrack;
                ids.Add(track.Id);

                if (_currentItemIsInLibrary)
                {
                    // Is in library, remove
                    LibraryRemoveTracksRequest removeRequest = new LibraryRemoveTracksRequest(ids);
                    await client.Library.RemoveTracks(removeRequest);

                    SetLibraryBtnIsLiked(false);
                }
                else
                {
                    // Not in library, add to user's library
                    LibrarySaveTracksRequest removeRequest = new LibrarySaveTracksRequest(ids);
                    await client.Library.SaveTracks(removeRequest);

                    SetLibraryBtnIsLiked(true);
                }
            }
            else if (context.Item.Type == ItemType.Episode)
            {
                FullEpisode episode = context.Item as FullEpisode;
                ids.Add(episode.Id);

                if (_currentItemIsInLibrary)
                {
                    LibraryRemoveShowsRequest request = new LibraryRemoveShowsRequest(ids);
                    await client.Library.RemoveShows(request);

                    SetLibraryBtnIsLiked(false);
                }
                else
                {
                    LibrarySaveShowsRequest request = new LibrarySaveShowsRequest(ids);
                    await client.Library.SaveShows(request);

                    SetLibraryBtnIsLiked(true);
                }
            }

           
        }
    }

    private void OnProgressSliderMouseDown(BaseEventData arg0)
    {
        _progressStartDrag = true;
    }

    private void OnProgressSliderValueChanged(float newValueMs)
    {
        _progressDragNewValue = newValueMs;

        _currentProgressText.text = S4UUtility.MsToTimeString((int)_progressDragNewValue);
    }

    private void OnProgressSliderMouseUp(BaseEventData arg0)
    {
        if (_progressStartDrag && _progressDragNewValue > 0)
        {
            SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();

            // Build request to set new ms position
            PlayerSeekToRequest request = new PlayerSeekToRequest((long)_progressDragNewValue);
            client.Player.SeekTo(request);

            // Set value in slider
            _currentProgressSlider.value = _progressDragNewValue;

            // Reset variables
            _progressStartDrag = false;
            _progressDragNewValue = -1.0f;
        }
    }

    private void SetLibraryBtnIsLiked(bool isLiked)
    {
        _currentItemIsInLibrary = isLiked;

        if (_addToLibraryButton != null)
        {
            Image img = _addToLibraryButton.GetComponent<Image>();
            img.color = isLiked ? Color.green : Color.white;
        }
    }
}