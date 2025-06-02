using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using ChatGPTWrapper;
public class ImageGenerator : MonoBehaviour
{
    [SerializeField] private string openAIApiKey = "";
    private const string DALLE_API_URL = "https://api.openai.com/v1/images/generations";

    [System.Serializable]
    public class ImageGenerationRequest
    {
        public string model = "dall-e-3";
        public string prompt;
        public int n = 1;
        public string size = "1024x1024";
        public string quality = "standard";
        public string style = "vivid";
    }

    [System.Serializable]
    public class ImageGenerationResponse
    {
        public int created;
        public ImageData[] data;
    }

    [System.Serializable]
    public class ImageData
    {
        public string url;
        public string revised_prompt;
    }

    public delegate void ImageGeneratedCallback(Texture2D frontSprite, Texture2D backSprite, bool success, string error = "");

    public void GeneratePokemonSprites(string pokemonName, string description, ImageGeneratedCallback callback)
    {
        StartCoroutine(GenerateSpritesCoroutine(pokemonName, description, callback));
    }

    private IEnumerator GenerateSpritesCoroutine(string pokemonName, string description, ImageGeneratedCallback callback)
    {
        // First generate the front sprite
        string frontPrompt = CreatePokemonSpritePrompt(pokemonName, description, "front view");
        yield return StartCoroutine(GenerateSingleImage(frontPrompt, (frontTexture, success, error) => 
        {
            if (success && frontTexture != null)
            {
                // Generate back sprite
                string backPrompt = CreatePokemonSpritePrompt(pokemonName, description, "back view");
                StartCoroutine(GenerateSingleImage(backPrompt, (backTexture, backSuccess, backError) => 
                {
                    if (backSuccess && backTexture != null)
                    {
                        callback?.Invoke(frontTexture, backTexture, true);
                    }
                    else
                    {
                        // If back sprite fails, create a darker version of front sprite as back
                        Texture2D generatedBack = CreateBackSpriteFromFront(frontTexture);
                        callback?.Invoke(frontTexture, generatedBack, true);
                    }
                }));
            }
            else
            {
                callback?.Invoke(null, null, false, error);
            }
        }));
    }

    private string CreatePokemonSpritePrompt(string pokemonName, string description, string viewType)
    {
        return $"Create a pixel art style Pokemon sprite of {pokemonName} in {viewType}. " +
               $"Description: {description}. " +
               "Style: 16-bit pixel art, clean lines, bright colors, transparent background, " +
               "similar to classic Pokemon game sprites. Size should be approximately 96x96 pixels when scaled. " +
               "The Pokemon should be centered and clearly visible against a transparent or white background.";
    }

    private IEnumerator GenerateSingleImage(string prompt, System.Action<Texture2D, bool, string> callback)
    {
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("OpenAI API key is not set!");
            callback?.Invoke(null, false, "API key not set");
            yield break;
        }

        // Create the request
        ImageGenerationRequest request = new ImageGenerationRequest
        {
            prompt = prompt,
            size = "1024x1024", // DALL-E 3 requirement
            quality = "standard"
        };

        string jsonData = JsonUtility.ToJson(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        // Create web request
        UnityWebRequest webRequest = new UnityWebRequest(DALLE_API_URL, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", $"Bearer {openAIApiKey}");

        yield return webRequest.SendWebRequest();

        string imageUrl = null;
        bool parseSuccess = false;

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string responseText = webRequest.downloadHandler.text;
                ImageGenerationResponse response = JsonUtility.FromJson<ImageGenerationResponse>(responseText);

                if (response.data != null && response.data.Length > 0)
                {
                    imageUrl = response.data[0].url;
                    parseSuccess = true;
                }
                else
                {
                    callback?.Invoke(null, false, "No image data in response");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing DALL-E response: {e.Message}");
                callback?.Invoke(null, false, $"Error parsing response: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"DALL-E API request failed: {webRequest.error}");
            Debug.LogError($"Response: {webRequest.downloadHandler.text}");
            callback?.Invoke(null, false, $"API request failed: {webRequest.error}");
        }

        webRequest.Dispose();

        // Download image if parsing was successful
        if (parseSuccess && !string.IsNullOrEmpty(imageUrl))
        {
            yield return StartCoroutine(DownloadImageFromUrl(imageUrl, callback));
        }
    }

    private IEnumerator DownloadImageFromUrl(string url, System.Action<Texture2D, bool, string> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                // Resize to appropriate sprite size (96x96)
                Texture2D resizedTexture = ResizeTexture(texture, 96, 96);
                callback?.Invoke(resizedTexture, true, "");
            }
            else
            {
                Debug.LogError($"Failed to download image: {webRequest.error}");
                callback?.Invoke(null, false, $"Failed to download image: {webRequest.error}");
            }
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    private Texture2D CreateBackSpriteFromFront(Texture2D frontTexture)
    {
        // Create a back sprite by darkening and possibly flipping the front sprite
        Texture2D backTexture = new Texture2D(frontTexture.width, frontTexture.height);
        
        for (int x = 0; x < frontTexture.width; x++)
        {
            for (int y = 0; y < frontTexture.height; y++)
            {
                Color pixel = frontTexture.GetPixel(x, y);
                // Darken the pixel for back sprite
                pixel.r *= 0.7f;
                pixel.g *= 0.7f;
                pixel.b *= 0.7f;
                backTexture.SetPixel(x, y, pixel);
            }
        }
        
        backTexture.Apply();
        return backTexture;
    }

    public void SetApiKey(string apiKey)
    {
        openAIApiKey = apiKey;
    }
} 