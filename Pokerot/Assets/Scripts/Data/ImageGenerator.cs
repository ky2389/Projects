using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class ImageGenerator : MonoBehaviour
{
    [SerializeField] private string poeApiKey = "";
    [SerializeField] private string poeModel = "Nano-Banana-2";
    [SerializeField] private string poeChatCompletionsApiUrl = "https://api.poe.com/v1/chat/completions";
    [SerializeField] private int spriteSize = 96;
    [SerializeField] private bool logPrompts = true;
    [SerializeField] private int rateLimitRetryCount = 3;
    [SerializeField] private float rateLimitBaseDelaySeconds = 5f;
    [SerializeField] private float rateLimitMaxDelaySeconds = 45f;

    private string currentPokemonName;
    private string currentViewName;

    [System.Serializable]
    public class ImageGenerationRequest
    {
        public string model;
        public ImageChatMessage[] messages;
        public bool stream = false;
    }

    [System.Serializable]
    public class ImageChatMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class ChatCompletionResponse
    {
        public ChatChoice[] choices;
    }

    [System.Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    public delegate void ImageGeneratedCallback(Texture2D frontSprite, Texture2D backSprite, bool success, string error = "");

    public void GeneratePokemonSprites(string pokemonName, string description, ImageGeneratedCallback callback)
    {
        GeneratePokemonSprites(pokemonName, description, "", PokemonType.None, PokemonType.None, callback);
    }

    public void GeneratePokemonSprites(string pokemonName, string description, string animalName, PokemonType type1, PokemonType type2, ImageGeneratedCallback callback)
    {
        StartCoroutine(GenerateSpritesCoroutine(pokemonName, description, animalName, type1, type2, callback));
    }

    private IEnumerator GenerateSpritesCoroutine(string pokemonName, string description, string animalName, PokemonType type1, PokemonType type2, ImageGeneratedCallback callback)
    {
        string sheetPrompt = CreatePokemonSpriteSheetPrompt(pokemonName, description, animalName, type1, type2);
        currentPokemonName = pokemonName;
        currentViewName = "sheet";

        yield return StartCoroutine(GenerateSingleRawImage(sheetPrompt, (sheetTexture, success, error) =>
        {
            if (success && sheetTexture != null)
            {
                Texture2D frontSprite = ExtractSpriteFromSheet(sheetTexture, true);
                Texture2D backSprite = ExtractSpriteFromSheet(sheetTexture, false);
                callback?.Invoke(frontSprite, backSprite, true);
            }
            else
            {
                callback?.Invoke(null, null, false, error);
            }
        }));
    }

    private string CreatePokemonSpriteSheetPrompt(string pokemonName, string description, string animalName, PokemonType type1, PokemonType type2)
    {
        AIPromptConfig promptConfig = AIPromptConfig.Load();
        string typeText = type2 == PokemonType.None ? type1.ToString() : $"{type1} & {type2}";
        string animalText = string.IsNullOrWhiteSpace(animalName) ? "" : $"{animalName} featured, ";

        return AIPromptConfig.FillTemplate(
            promptConfig.imageSpriteSheetPromptTemplate,
            ("pokemonName", pokemonName),
            ("description", description),
            ("animalName", animalName),
            ("animalText", animalText),
            ("typeText", typeText),
            ("type1", type1.ToString()),
            ("type2", type2.ToString()));
    }

    private IEnumerator GenerateSingleRawImage(string prompt, System.Action<Texture2D, bool, string> callback)
    {
        string apiKey = ApiSecretConfig.GetPoeApiKey(poeApiKey);
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Poe API key is not set. Add POE_API_KEY as an environment variable or create LocalApiSecrets.json.");
            callback?.Invoke(null, false, "API key not set");
            yield break;
        }

        if (logPrompts)
        {
            Debug.Log($"Poe image prompt ({currentViewName}): {prompt}");
        }

        // Create the request
        ImageGenerationRequest request = new ImageGenerationRequest
        {
            model = poeModel,
            messages = new[]
            {
                new ImageChatMessage
                {
                    role = "user",
                    content = prompt
                }
            },
            stream = false
        };

        string jsonData = JsonUtility.ToJson(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        string imageUrl = null;
        bool parseSuccess = false;
        string finalError = "";

        for (int attempt = 0; attempt <= rateLimitRetryCount; attempt++)
        {
            UnityWebRequest webRequest = new UnityWebRequest(poeChatCompletionsApiUrl, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = webRequest.downloadHandler.text;
                    Debug.Log($"Poe image response ({currentViewName}): {responseText}");
                    ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);

                    if (response.choices != null && response.choices.Length > 0 && response.choices[0].message != null)
                    {
                        imageUrl = ExtractImageUrl(response.choices[0].message.content);
                        parseSuccess = !string.IsNullOrEmpty(imageUrl);

                        if (!parseSuccess)
                        {
                            finalError = "Chat completion response did not include an image URL";
                        }
                    }
                    else
                    {
                        finalError = "No chat choices in image response";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing Poe image response: {e.Message}");
                    finalError = $"Error parsing response: {e.Message}";
                }

                webRequest.Dispose();
                break;
            }

            finalError = $"API request failed: {webRequest.error}";
            bool shouldRetryRateLimit = webRequest.responseCode == 429 && attempt < rateLimitRetryCount;
            Debug.LogError($"Poe chat completions image request failed: {webRequest.error}");
            Debug.LogError($"Response: {webRequest.downloadHandler.text}");

            if (shouldRetryRateLimit)
            {
                float retryDelay = GetRateLimitRetryDelay(webRequest, attempt);
                Debug.LogWarning($"Poe image bot is rate limited. Retrying in {retryDelay:0.0}s ({attempt + 1}/{rateLimitRetryCount}).");
                webRequest.Dispose();
                yield return new WaitForSeconds(retryDelay);
                continue;
            }

            webRequest.Dispose();
            break;
        }

        // Download image if parsing was successful
        if (parseSuccess && !string.IsNullOrEmpty(imageUrl))
        {
            yield return StartCoroutine(DownloadRawImageFromUrl(imageUrl, callback));
        }
        else
        {
            callback?.Invoke(null, false, finalError);
        }
    }

    private float GetRateLimitRetryDelay(UnityWebRequest webRequest, int attempt)
    {
        string retryAfterHeader = webRequest.GetResponseHeader("Retry-After");
        if (float.TryParse(retryAfterHeader, out float retryAfterSeconds) && retryAfterSeconds > 0f)
        {
            return Mathf.Min(retryAfterSeconds, rateLimitMaxDelaySeconds);
        }

        float exponentialDelay = rateLimitBaseDelaySeconds * Mathf.Pow(2f, attempt);
        float jitter = UnityEngine.Random.Range(0f, 2f);
        return Mathf.Min(exponentialDelay + jitter, rateLimitMaxDelaySeconds);
    }

    private string ExtractImageUrl(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        Match markdownImage = Regex.Match(content, @"!\[[^\]]*\]\((https?://[^)\s]+)\)");
        if (markdownImage.Success)
        {
            return markdownImage.Groups[1].Value;
        }

        Match anyImageUrl = Regex.Match(content, @"https?://\S+\.(?:png|jpg|jpeg|webp)(?:\?\S*)?", RegexOptions.IgnoreCase);
        if (anyImageUrl.Success)
        {
            return anyImageUrl.Value.TrimEnd(')', ']', '"', '\'');
        }

        Match anyPoeCdnUrl = Regex.Match(content, @"https?://\S*poe\S*", RegexOptions.IgnoreCase);
        if (anyPoeCdnUrl.Success)
        {
            return anyPoeCdnUrl.Value.TrimEnd(')', ']', '"', '\'');
        }

        Debug.LogWarning($"Could not find image URL in Poe response content: {content}");
        return null;
    }

    private IEnumerator DownloadRawImageFromUrl(string url, System.Action<Texture2D, bool, string> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                callback?.Invoke(texture, true, "");
            }
            else
            {
                Debug.LogError($"Failed to download image: {webRequest.error}");
                callback?.Invoke(null, false, $"Failed to download image: {webRequest.error}");
            }
        }
    }

    private Texture2D TextureFromBase64(string base64Image)
    {
        string payload = base64Image;
        int commaIndex = payload.IndexOf(',');
        if (commaIndex >= 0)
        {
            payload = payload.Substring(commaIndex + 1);
        }

        try
        {
            byte[] imageBytes = Convert.FromBase64String(payload);
            Texture2D texture = new Texture2D(2, 2);
            return texture.LoadImage(imageBytes) ? texture : null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to decode base64 image: {e.Message}");
            return null;
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        result.filterMode = FilterMode.Point;
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    private Texture2D ResizeToSprite(Texture2D source)
    {
        return ResizeTexture(source, spriteSize, spriteSize);
    }

    private Texture2D ExtractSpriteFromSheet(Texture2D sheetTexture, bool frontSprite)
    {
        int halfWidth = sheetTexture.width / 2;
        int sourceX = frontSprite ? 0 : halfWidth;
        int sourceWidth = frontSprite ? halfWidth : sheetTexture.width - halfWidth;

        Texture2D halfTexture = new Texture2D(sourceWidth, sheetTexture.height, TextureFormat.RGBA32, false);
        for (int x = 0; x < sourceWidth; x++)
        {
            for (int y = 0; y < sheetTexture.height; y++)
            {
                halfTexture.SetPixel(x, y, sheetTexture.GetPixel(sourceX + x, y));
            }
        }
        halfTexture.Apply();

        RemoveEdgeBackground(halfTexture);
        Texture2D croppedSprite = CropToAlphaBounds(halfTexture);
        Texture2D resizedSprite = ResizeToSprite(croppedSprite);
        RemoveEdgeBackground(resizedSprite);
        resizedSprite.filterMode = FilterMode.Point;
        resizedSprite.Apply();
        return resizedSprite;
    }

    private void RemoveEdgeBackground(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        void EnqueueIfBackground(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y])
                return;

            if (!IsBackgroundPixel(texture.GetPixel(x, y)))
                return;

            visited[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        for (int x = 0; x < width; x++)
        {
            EnqueueIfBackground(x, 0);
            EnqueueIfBackground(x, height - 1);
        }

        for (int y = 0; y < height; y++)
        {
            EnqueueIfBackground(0, y);
            EnqueueIfBackground(width - 1, y);
        }

        while (queue.Count > 0)
        {
            Vector2Int pixel = queue.Dequeue();
            Color color = texture.GetPixel(pixel.x, pixel.y);
            color.a = 0f;
            texture.SetPixel(pixel.x, pixel.y, color);

            EnqueueIfBackground(pixel.x + 1, pixel.y);
            EnqueueIfBackground(pixel.x - 1, pixel.y);
            EnqueueIfBackground(pixel.x, pixel.y + 1);
            EnqueueIfBackground(pixel.x, pixel.y - 1);
        }

        texture.Apply();
    }

    private bool IsBackgroundPixel(Color color)
    {
        if (color.a <= 0.1f)
            return true;

        float maxChannel = Mathf.Max(color.r, color.g, color.b);
        float minChannel = Mathf.Min(color.r, color.g, color.b);
        float average = (color.r + color.g + color.b) / 3f;
        return average > 0.86f && (maxChannel - minChannel) < 0.18f;
    }

    private Texture2D CropToAlphaBounds(Texture2D texture)
    {
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                if (texture.GetPixel(x, y).a <= 0.05f)
                    continue;

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return ResizeToSprite(texture);
        }

        int padding = Mathf.RoundToInt(Mathf.Max(maxX - minX + 1, maxY - minY + 1) * 0.12f);
        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(texture.width - 1, maxX + padding);
        maxY = Mathf.Min(texture.height - 1, maxY + padding);

        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        int squareSize = Mathf.Max(contentWidth, contentHeight);

        Texture2D cropped = new Texture2D(squareSize, squareSize, TextureFormat.RGBA32, false);
        Color transparent = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < squareSize; x++)
        {
            for (int y = 0; y < squareSize; y++)
            {
                cropped.SetPixel(x, y, transparent);
            }
        }

        int offsetX = (squareSize - contentWidth) / 2;
        int offsetY = (squareSize - contentHeight) / 2;
        for (int x = 0; x < contentWidth; x++)
        {
            for (int y = 0; y < contentHeight; y++)
            {
                cropped.SetPixel(offsetX + x, offsetY + y, texture.GetPixel(minX + x, minY + y));
            }
        }

        cropped.Apply();
        cropped.filterMode = FilterMode.Point;
        return cropped;
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
        poeApiKey = apiKey;
    }
}

[Serializable]
public class AIPromptConfig
{
    private const string ResourcePath = "AIPromptConfig";
    private static AIPromptConfig cachedConfig;

    public string ollamaInitialPrompt = "You are a helpful assistant.";
    public string chatGPTInitialPrompt = "You are ChatGPT, a large language model trained by OpenAI.";
    public string pokemonTypeAutoInstruction = "Choose one or two Pokemon types that fit the animal. If it should only have one type, set type2 to \"None\".";
    public string pokemonTypeLockedInstructionTemplate = "The player selected {selectedTypeDescription}. You must use exactly these type fields: type1=\"{type1}\" and type2=\"{type2}\". Do not choose or change the Pokemon's types.";
    public string pokemonDataPromptTemplate = "Create a Pokemon-like monster based on this animal: {animalName}. {typeInstruction} Return only one JSON object with these fields: name, type1, type2, description, moves. The moves array should contain 4 moves, and each move should have: name, type, power, accuracy, description, alwaysHits, pp, priority, category, target. Make the name, description, and moves fit the animal and the chosen typing. {moveDesignInstruction}Valid type values are: Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison, Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy, None. For one-type Pokemon, use type2=\"None\". Use category values Physical, Special, or Status. Use target values Foe or Self.";
    public string moveDesignInstruction = "Use one or two well-known official Pokemon moves from the original games when they naturally fit the creature and its typing, using their exact official names. Create custom moves for the remaining slots. Keep all moves balanced for a playable early-game Pokemon: most damaging moves should have 30-80 power, stronger moves should have lower accuracy, high-accuracy moves should usually stay at 60 power or below, status moves should usually have 0 power, PP should usually be 10-35, priority should usually be 0 and only rarely 1, and alwaysHits should only be true for weak or utility moves.";
    public string imageSpriteSheetPromptTemplate = "Create a two-view sprite sheet for one single collectible monster battle-game creature. The left half must show the front view. The right half must show the back view of the exact same creature design. Both views must have the same body plan, limbs, colors, markings, silhouette, proportions, and accessories. Do not redesign the creature between views. Do not add labels, text, panel borders, UI, or multiple creatures. Pixel sprite, Pokemon sprite style, Pokemon generation V style, cel shading, clean dark outline, limited palette, readable silhouette. Cute but battle-ready creature character, large head/body silhouette, simple iconic shapes, game-sprite proportions. Full body visible in both views, standard idle pose, eyes open if visible, mouth closed, {animalText}elemental type: {typeText}. Creature name: {pokemonName}. Design notes: {description}. Each view should fill most of its half of the canvas and be centered in its half. Transparent background or pure white background only, for easy masking. No environment, no landscape, no realistic animal photo, no human, no trainer, no shadow, no logo, no frame.";

    public static AIPromptConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        TextAsset configAsset = Resources.Load<TextAsset>(ResourcePath);
        if (configAsset == null || string.IsNullOrWhiteSpace(configAsset.text))
        {
            cachedConfig = new AIPromptConfig();
            Debug.LogWarning($"No prompt config found at Resources/{ResourcePath}.json. Using built-in prompt defaults.");
            return cachedConfig;
        }

        try
        {
            cachedConfig = JsonUtility.FromJson<AIPromptConfig>(configAsset.text);
            if (cachedConfig == null)
            {
                cachedConfig = new AIPromptConfig();
                Debug.LogWarning("Prompt config parsed as null. Using built-in prompt defaults.");
            }
        }
        catch (Exception e)
        {
            cachedConfig = new AIPromptConfig();
            Debug.LogWarning($"Failed to parse prompt config: {e.Message}. Using built-in prompt defaults.");
        }

        return cachedConfig;
    }

    public static string FillTemplate(string template, params (string key, string value)[] replacements)
    {
        string result = template ?? "";
        foreach (var replacement in replacements)
        {
            result = result.Replace("{" + replacement.key + "}", replacement.value ?? "");
        }

        return result;
    }
}

[Serializable]
public class ApiSecretConfig
{
    private const string LocalSecretsFileName = "LocalApiSecrets.json";
    private static ApiSecretConfig cachedConfig;

    public string poeApiKey = "";
    public string openAIApiKey = "";

    public static string GetPoeApiKey(string fallback = "")
    {
        string envKey = Environment.GetEnvironmentVariable("POE_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey.Trim();
        }

        string configKey = Load().poeApiKey;
        if (!string.IsNullOrWhiteSpace(configKey))
        {
            return configKey.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? "" : fallback.Trim();
    }

    public static string GetOpenAIApiKey(string fallback = "")
    {
        string envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey.Trim();
        }

        string configKey = Load().openAIApiKey;
        if (!string.IsNullOrWhiteSpace(configKey))
        {
            return configKey.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? "" : fallback.Trim();
    }

    private static ApiSecretConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        foreach (string secretsPath in GetLocalSecretsPaths())
        {
            if (string.IsNullOrEmpty(secretsPath) || !System.IO.File.Exists(secretsPath))
            {
                continue;
            }

            try
            {
                cachedConfig = JsonUtility.FromJson<ApiSecretConfig>(System.IO.File.ReadAllText(secretsPath));
                if (cachedConfig == null)
                {
                    cachedConfig = new ApiSecretConfig();
                }

                return cachedConfig;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read local API secrets from {secretsPath}: {e.Message}");
            }
        }

        cachedConfig = new ApiSecretConfig();
        return cachedConfig;
    }

    private static string[] GetLocalSecretsPaths()
    {
        #if UNITY_EDITOR
        return new[]
        {
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), LocalSecretsFileName),
            System.IO.Path.Combine(Application.persistentDataPath, LocalSecretsFileName)
        };
        #else
        string buildFolderPath = System.IO.Directory.GetParent(Application.dataPath)?.FullName;
        return new[]
        {
            string.IsNullOrEmpty(buildFolderPath) ? "" : System.IO.Path.Combine(buildFolderPath, LocalSecretsFileName),
            System.IO.Path.Combine(Application.persistentDataPath, LocalSecretsFileName)
        };
        #endif
    }
}