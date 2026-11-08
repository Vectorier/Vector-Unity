using Core._Common;

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loads moddable sprites, textures, text and audio from StreamingAssets (with Resources as fallback).</summary>
public static class ResourcesLoader
{
    public static IEnumerator LoadItemSpriteAsync(string trickIconId, System.Action<Sprite> onloaded)
    {
        Sprite sprite = null;

        yield return LoadSpriteAsync(VectorPaths.Icons + "/shop/" + trickIconId, loaded =>
        {
            sprite = loaded;
        }, new Vector2(0.5f, 0.5f), 100);;
        
        onloaded?.Invoke(sprite);
    }

    public static IEnumerator LoadLocationSpriteAsync(string locationIconId, System.Action<Sprite> onloaded)
    {
        Sprite sprite = null;

        yield return LoadSpriteAsync(VectorPaths.Icons + "/locations/" + locationIconId, loaded =>
        {
            sprite = loaded;

        }, new Vector2(0.5f, 0.5f), 100); ;

        onloaded?.Invoke(sprite);
    }

    public static IEnumerator LoadStoriesSpriteAsync(string locationIconId, System.Action<Sprite> onloaded)
    {
        Sprite sprite = null;

        yield return LoadSpriteAsync(VectorPaths.Icons + "/stories/" + locationIconId, loaded =>
        {
            sprite = loaded;

        }, new Vector2(0.5f, 0.5f), 100); ;

        onloaded?.Invoke(sprite);
    }

    public static IEnumerator LoadSpriteAsync(string path, System.Action<Sprite> onLoaded, Vector2 pivot = default, float ppu = 1)
    {
        if (string.IsNullOrEmpty(path))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        if (path.StartsWith(Application.streamingAssetsPath))
        {
            yield return ResourceManager.LoadSpriteFromExternalAsync(
                path,
                pivot,
                ppu,
                tex => onLoaded?.Invoke(tex)
            );

            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<Sprite>(path);

        yield return request;

        onLoaded?.Invoke(request.asset as Sprite);
    }

    public static Texture2D LoadTexture2D(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        if (path.StartsWith(Application.streamingAssetsPath))
        {
            return ResourceManager.LoadTextureFromExternal(path);
        }
        return Resources.Load<Texture2D>(path);
    }

    public static string LoadText(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        if (path.StartsWith(Application.streamingAssetsPath))
        {
            return File.ReadAllText(path);
        }
        return Resources.Load<TextAsset>(path).text;
    }

    public static IEnumerator LoadAudioClipAsync(string audioClip, System.Action<AudioClip> onLoaded)
    {
        var path = VectorPaths.Sounds + "/" + audioClip;

        if (string.IsNullOrEmpty(path))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        if (path.StartsWith(Application.streamingAssetsPath))
        {
            yield return ResourceManager.GetAudioClipFromExternalAsync(
                path,
                tex => onLoaded?.Invoke(tex)
            );

            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<AudioClip>(path);

        yield return request;

        onLoaded?.Invoke(request.asset as AudioClip);
    }

    public static IEnumerator LoadMusicClipAsync(string musicClip, System.Action<AudioClip> onLoaded)
    {
        var path = VectorPaths.Music + "/" + musicClip;

        if (string.IsNullOrEmpty(path))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        if (path.StartsWith(Application.streamingAssetsPath))
        {
            yield return ResourceManager.GetAudioClipFromExternalAsync(
                path,
                tex => onLoaded?.Invoke(tex), true
            );

            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<AudioClip>(path);

        yield return request;

        onLoaded?.Invoke(request.asset as AudioClip);
    }

    public static Image LoadImage(string id)
    {
        return Resources.Load<Image>(id);
    }

    public static T Load<T>(string id) where T : Object
    {
        return Resources.Load<T>(id);
    }
}
