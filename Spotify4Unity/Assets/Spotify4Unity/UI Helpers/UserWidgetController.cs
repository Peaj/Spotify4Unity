using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class UserWidgetController : UIListener
{
    [Tooltip("User Id of the spotify user you wish to display in this widget. If none supplied, will display the current signed in user. Can accept username's like 'wizzler' or URI id's like '1121645175. Hold Ctrl or Cmd and click '...' on a user's page toget their URI")]
    public string UserId;

    [SerializeField]
    private UnityEngine.UI.Image _icon;

    [SerializeField]
    private Button _followBtn;

    [SerializeField]
    private Text _nameText, _followersText, _countryText, _productText, _typeText, _uriText, _idText;

    // Current signed in private user info
    private PrivateUser _privateUserInfo = null;
    // Public user information available to everyone from user id
    private PublicUser _publicUserInfo = null;

    protected override async void OnSpotifyConnected(SpotifyClient client)
    {
        base.OnSpotifyConnected(client);

        if (string.IsNullOrEmpty(UserId))
        {
            _privateUserInfo = await client.UserProfile.Current();
        }
        else
        {
            _publicUserInfo = await client.UserProfile.Get(UserId);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_privateUserInfo != null)
        {
            DownloadUpdateSprite(_icon, _privateUserInfo.Images);

            UpdateTextElement(_nameText, $"Name: {_privateUserInfo.DisplayName}");
            UpdateTextElement(_followersText, $"Followers: {_privateUserInfo.Followers.Total.ToString()}");
            UpdateTextElement(_typeText, $"Type: {_privateUserInfo.Type}");
            UpdateTextElement(_uriText, $"URI: {_privateUserInfo.Uri}");
            UpdateTextElement(_idText, $"Id: {_privateUserInfo.Id}");

            // PrivateProfile specific properties
            UpdateTextElement(_countryText, $"Country: {_privateUserInfo.Country}");
            UpdateTextElement(_productText, $"Product: {_privateUserInfo.Product}");

            // Disable follow, cant follow self
            if (_followBtn)
                _followBtn.gameObject.SetActive(false);
        }
        else if (_publicUserInfo != null)
        {
            DownloadUpdateSprite(_icon, _publicUserInfo.Images);

            UpdateTextElement(_nameText, $"Name: {_publicUserInfo.DisplayName}");
            UpdateTextElement(_followersText, $"Followers: {_publicUserInfo.Followers.Total.ToString()}");
            UpdateTextElement(_typeText, $"Type: {_publicUserInfo.Type}");
            UpdateTextElement(_uriText, $"URI: {_publicUserInfo.Uri}");
            UpdateTextElement(_idText, $"Id: {_publicUserInfo.Id}");

            if (_productText)
                _productText.gameObject.SetActive(false);
            if (_countryText)
                _countryText.gameObject.SetActive(false);
            

            if (_followBtn != null)
            {
                _followBtn.onClick.AddListener(() =>
                {
                    SpotifyClient client = SpotifyService.Instance.GetSpotifyClient();
                    if (client != null)
                    {
                        List<string> allArtistIdsList = new List<string>() { UserId };
                        FollowRequest followRequest = new FollowRequest(FollowRequest.Type.Artist, allArtistIdsList);
                        client.Follow.Follow(followRequest);
                    }
                });
            }
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
