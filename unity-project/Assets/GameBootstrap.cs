using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TowerOfSaviors;

/// <summary>
/// The only MonoBehaviour in the project. Bridges Unity lifecycle to the
/// pure C# TosGame. Loads JSON data files, then starts the game.
/// On WebGL, uses UnityWebRequest for StreamingAssets (File.ReadAllText unsupported).
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private TosGame _game;
    private bool _started;

    private static readonly string[] DataTypes = {
        "monsters", "skills", "leader_skills", "stages", "gacha_pools",
        "team_skills", "gem_modifiers", "element_chart", "loot_tables",
        "monster_exchange", "event_shops", "player_levels"
    };

    void Awake()
    {
        _game = new TosGame();
        StartCoroutine(LoadAllData());
    }

    void Update()
    {
        if (_started)
            _game.Update();
    }

    void OnApplicationQuit()
    {
        if (_started)
            _game.OnQuit();
    }

    public TosGame Game => _game;

    private IEnumerator LoadAllData()
    {
        int loaded = 0;
        foreach (string type in DataTypes)
        {
            string fileName = type + ".json";
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "GameData", fileName);

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: must use UnityWebRequest for StreamingAssets
            using (var request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ParseAndLoad(type, request.downloadHandler.text);
                    loaded++;
                }
                else
                {
                    Debug.LogWarning($"[GameBootstrap] Failed to load {fileName}: {request.error}");
                }
            }
#else
            // Editor/Standalone: direct file read
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                ParseAndLoad(type, json);
                loaded++;
            }
            else
            {
                Debug.LogWarning($"[GameBootstrap] File not found: {path}");
            }
            yield return null; // Spread across frames
#endif
        }

        Debug.Log($"[GameBootstrap] Loaded {loaded}/{DataTypes.Length} data files");
        _game.Start();
        _started = true;
        Debug.Log($"[GameBootstrap] Game started — screen: {_game.Router.CurrentScreenId}");
    }

    private void ParseAndLoad(string type, string json)
    {
        try
        {
            var token = JToken.Parse(json);

            if (token is JArray rootArray)
            {
                var entries = ConvertJArray(rootArray);
                if (entries != null && entries.Count > 0)
                {
                    _game.LoadData(type, entries);
                    Debug.Log($"[GameBootstrap] Loaded {type}: {entries.Count} entries");
                }
                return;
            }

            if (token is JObject rootObj)
            {
                // Root is an object — find the first array property with "id" fields
                foreach (var prop in rootObj.Properties())
                {
                    if (prop.Value is JArray arr && arr.Count > 0 && arr[0] is JObject first && first.ContainsKey("id"))
                    {
                        var entries = ConvertJArray(arr);
                        if (entries != null && entries.Count > 0)
                        {
                            _game.LoadData(type, entries);
                            Debug.Log($"[GameBootstrap] Loaded {type}: {entries.Count} entries (from '{prop.Name}')");
                        }
                        return;
                    }
                }

                // No array with ids found — wrap entire object as single entry with synthetic id
                var dict = ConvertJObject(rootObj);
                if (dict != null)
                {
                    if (!dict.ContainsKey("id"))
                        dict["id"] = 0;
                    _game.LoadData(type, new List<Dictionary<string, object>> { dict });
                    Debug.Log($"[GameBootstrap] Loaded {type}: 1 entry (structured data)");
                }
                return;
            }

            Debug.LogWarning($"[GameBootstrap] Unexpected JSON root type for {type}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameBootstrap] Parse error for {type}: {e.Message}");
        }
    }

    /// <summary>
    /// Recursively convert JToken to native CLR types so downstream code
    /// doesn't need to handle JObject/JArray/JValue.
    /// </summary>
    private static object ConvertJToken(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                return ConvertJObject((JObject)token);
            case JTokenType.Array:
            {
                var list = new List<object>();
                foreach (var item in (JArray)token)
                    list.Add(ConvertJToken(item));
                return list;
            }
            case JTokenType.Integer:
                return token.Value<long>() is long l && l >= int.MinValue && l <= int.MaxValue
                    ? (object)(int)l : l;
            case JTokenType.Float:
                return token.Value<double>();
            case JTokenType.Boolean:
                return token.Value<bool>();
            case JTokenType.String:
                return token.Value<string>();
            case JTokenType.Null:
            case JTokenType.Undefined:
                return null;
            default:
                return token.ToString();
        }
    }

    private static Dictionary<string, object> ConvertJObject(JObject obj)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in obj.Properties())
            dict[prop.Name] = ConvertJToken(prop.Value);
        return dict;
    }

    private static List<Dictionary<string, object>> ConvertJArray(JArray arr)
    {
        var list = new List<Dictionary<string, object>>();
        foreach (var item in arr)
        {
            if (item is JObject jObj)
                list.Add(ConvertJObject(jObj));
        }
        return list;
    }
}
