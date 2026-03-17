using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
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
        "team_skills", "gem_modifiers", "element_chart", "loot_tables"
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
            var entries = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            if (entries != null)
            {
                _game.LoadData(type, entries);
                Debug.Log($"[GameBootstrap] Loaded {type}: {entries.Count} entries");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameBootstrap] Parse error for {type}: {e.Message}");
        }
    }
}
