using SpotifyAPI.Web;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Image = UnityEngine.UI.Image;

public class ArtistWidgetController : UIListener
{
    [Tooltip("Id of the artist to display in thw widget. ")]
    public string ArtistId;

    [SerializeField]
    private UnityEngine.UI.Image _icon;

    [SerializeField]
    private Button _followBtn;

    [SerializeField]
    private Text _nameText, _idText, _uriText, _followersText, _genresText, _popularityText, _typeText;

    private FullArtist _artistInfo;

    protected override async void OnSpotifyConnected(SpotifyClient client)
    {
        base.OnSpotifyConnected(client);

        _artistInfo = await client.Artists.Get(ArtistId);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_artistInfo != null)
        {
            DownloadUpdateSprite(_icon, _artistInfo.Images);

            UpdateTextElement(_nameText, $"Name: {_artistInfo.Name}");
            UpdateTextElement(_idText, $"Id: {_artistInfo.Id}");
            UpdateTextElement(_uriText, $"URI: {_artistInfo.Uri}");
            UpdateTextElement(_followersText, $"Followers: {_artistInfo.Followers.Total.ToString()}");
            UpdateTextElement(_genresText, $"Genres: {string.Join(", ", _artistInfo.Genres.ToArray())}");
            UpdateTextElement(_popularityText, $"Popularity: {_artistInfo.Popularity}");
            UpdateTextElement(_typeText, $"Type: {_artistInfo.Type}");
        }

        // If follow btn set, add listener to on click to follow the current artist
        if (_followBtn != null)
        {
            _followBtn.onClick.AddListener(() =>
            {
                SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
                if (client != null)
                {
                    List<string> allArtistIdsList = new List<string>() { ArtistId };
                    FollowRequest followRequest = new FollowRequest(FollowRequest.Type.Artist, allArtistIdsList);
                    client.Follow.Follow(followRequest);
                }
            });
        }
    }

    private void UpdateTextElement(Text element, string content)
    {
        if (element != null && !string.IsNullOrEmpty(content))
        {
            element.text = content;
        }
    }

    private void DownloadUpdateSprite(Image img, List<SpotifyAPI.Web.Image> images)
    {
        if (img != null && img.sprite == null)
        {
            string iconUrl = images.FirstOrDefault()?.Url;
            if (!string.IsNullOrEmpty(iconUrl))
            {
                StartCoroutine(S4UUtility.LoadImageFromUrl(iconUrl, (loadedSprite) =>
                {
                    _icon.sprite = loadedSprite;
                }));
            }
        }
    }
}
