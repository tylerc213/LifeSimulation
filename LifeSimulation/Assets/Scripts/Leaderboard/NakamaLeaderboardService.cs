// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Nakama Leaderboard Service
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Nakama REST client for this project’s arcade leaderboards: authenticate
//    (device id; optional disposable id per submit), POST scores with
//    metadata JSON {"name":"..."} for display names, and GET top records.
//    Used by ScoreSummary (writes) and Leaderboard scene (reads); ranks use
//    numeric score only—names are not first-class leaderboard fields.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary> Nakama HTTP: auth, leaderboard write (flat REST body), list records. </summary>
public class NakamaLeaderboardService : MonoBehaviour
{
    public static NakamaLeaderboardService Instance;

    [Header("Nakama Configuration")]
    public string scheme = "http";
    public string host = "172.200.210.208";
    public int port = 7350;
    public string serverKey = "defaultkey";
    public int requestTimeoutSeconds = 10;
    public bool useDisposableIdentityPerSubmit = true;

    private string authToken;
    private bool isAuthenticating;
    private const string InsecureHttpHint = "Insecure HTTP blocked. Use HTTPS for Nakama or enable 'Allow downloads over HTTP' in Player Settings.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        var serviceObject = new GameObject("NakamaLeaderboardService");
        serviceObject.AddComponent<NakamaLeaderboardService>();
    }

    [Serializable]
    private class DeviceAuthRequest
    {
        public string id;
    }

    [Serializable]
    private class DeviceAuthResponse
    {
        public string token;
    }

    /// <summary>
    /// REST POST body for /v2/leaderboard/{id} is this object only — not wrapped in "record"
    /// (see Nakama apigrpc.swagger.json: body schema LeaderboardRecordWrite).
    /// </summary>
    [Serializable]
    private class LeaderboardRecordWriteBody
    {
        public string score;
        public string subscore;
        public string metadata;
    }

    /// <summary> Matches common Nakama metadata: {"name":"PlayerName"}. </summary>
    [Serializable]
    private class LeaderboardNameMetadata
    {
        public string name;
    }

    [Serializable]
    private class LeaderboardListResponse
    {
        public LeaderboardRecord[] records;
    }

    [Serializable]
    public class LeaderboardRecord
    {
        public string owner_id;
        public string score;
        public string username;
        public string metadata;
        public long rank;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        scheme = NormalizeScheme(scheme);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary> Ensures a device session token exists before leaderboard calls. </summary>
    public IEnumerator EnsureAuthenticated(Action<bool> onComplete)
    {
        if (!string.IsNullOrEmpty(authToken))
        {
            onComplete?.Invoke(true);
            yield break;
        }

        if (isAuthenticating)
        {
            yield return new WaitUntil(() => !isAuthenticating);
            onComplete?.Invoke(!string.IsNullOrEmpty(authToken));
            yield break;
        }

        isAuthenticating = true;
        string deviceId;
        if (useDisposableIdentityPerSubmit)
        {
            // Arcade mode: each submission run can use a disposable identity.
            deviceId = Guid.NewGuid().ToString();
        }
        else
        {
            deviceId = PlayerPrefs.GetString("nakama_device_id", SystemInfo.deviceUniqueIdentifier);
            if (deviceId == SystemInfo.unsupportedIdentifier || string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
            }
            PlayerPrefs.SetString("nakama_device_id", deviceId);
        }

        var request = new DeviceAuthRequest { id = deviceId };
        string body = JsonUtility.ToJson(request);
        string url = $"{scheme}://{host}:{port}/v2/account/authenticate/device?create=true";

        using (UnityWebRequest webRequest = BuildJsonRequest(url, "POST", body))
        {
            webRequest.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(serverKey + ":")));
            UnityWebRequestAsyncOperation asyncOperation;
            try
            {
                asyncOperation = webRequest.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                isAuthenticating = false;
                Debug.LogError($"{InsecureHttpHint} Exception: {ex.Message}");
                onComplete?.Invoke(false);
                yield break;
            }
            yield return asyncOperation;

            isAuthenticating = false;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Nakama device auth failed: " + webRequest.error + " | " + webRequest.downloadHandler.text);
                onComplete?.Invoke(false);
                yield break;
            }

            DeviceAuthResponse response = JsonUtility.FromJson<DeviceAuthResponse>(webRequest.downloadHandler.text);
            authToken = response != null ? response.token : null;
            onComplete?.Invoke(!string.IsNullOrEmpty(authToken));
        }
    }

    /// <summary> Writes a single leaderboard record with display name metadata. </summary>
    public IEnumerator SubmitScore(string boardId, long score, string displayName, Action<bool> onComplete)
    {
        bool isReady = false;
        yield return EnsureAuthenticated(ok => isReady = ok);
        if (!isReady)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string safeName = string.IsNullOrWhiteSpace(displayName) ? "Guest" : displayName.Trim();
        string metadataJson = JsonUtility.ToJson(new LeaderboardNameMetadata { name = safeName });
        var bodyObj = new LeaderboardRecordWriteBody
        {
            score = score.ToString(),
            subscore = "0",
            metadata = metadataJson
        };

        string url = $"{scheme}://{host}:{port}/v2/leaderboard/{boardId}";
        using (UnityWebRequest webRequest = BuildJsonRequest(url, "POST", JsonUtility.ToJson(bodyObj)))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);
            UnityWebRequestAsyncOperation asyncOperation;
            try
            {
                asyncOperation = webRequest.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"{InsecureHttpHint} Exception: {ex.Message}");
                onComplete?.Invoke(false);
                yield break;
            }
            yield return asyncOperation;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Nakama leaderboard submit failed: " + webRequest.error + " | " + webRequest.downloadHandler.text);
                onComplete?.Invoke(false);
                yield break;
            }

            onComplete?.Invoke(true);
        }
    }

    /// <summary> Lists top records for a leaderboard board id. </summary>
    public IEnumerator FetchTopRecords(string boardId, int limit, Action<List<LeaderboardRecord>> onComplete)
    {
        bool isReady = false;
        yield return EnsureAuthenticated(ok => isReady = ok);
        if (!isReady)
        {
            onComplete?.Invoke(new List<LeaderboardRecord>());
            yield break;
        }

        string url = $"{scheme}://{host}:{port}/v2/leaderboard/{boardId}?limit={Mathf.Clamp(limit, 1, 100)}";
        using (UnityWebRequest webRequest = BuildJsonRequest(url, "GET", null))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);
            UnityWebRequestAsyncOperation asyncOperation;
            try
            {
                asyncOperation = webRequest.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"{InsecureHttpHint} Exception: {ex.Message}");
                onComplete?.Invoke(new List<LeaderboardRecord>());
                yield break;
            }
            yield return asyncOperation;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Nakama leaderboard fetch failed: " + webRequest.error + " | " + webRequest.downloadHandler.text);
                onComplete?.Invoke(new List<LeaderboardRecord>());
                yield break;
            }

            LeaderboardListResponse response = JsonUtility.FromJson<LeaderboardListResponse>(webRequest.downloadHandler.text);
            int recordCount = response != null && response.records != null ? response.records.Length : 0;
            Debug.Log($"Nakama leaderboard fetch success for '{boardId}'. Records: {recordCount}");
            onComplete?.Invoke(response != null && response.records != null
                ? new List<LeaderboardRecord>(response.records)
                : new List<LeaderboardRecord>());
        }
    }

    private UnityWebRequest BuildJsonRequest(string url, string method, string body)
    {
        UnityWebRequest request = new UnityWebRequest(url, method)
        {
            timeout = requestTimeoutSeconds
        };

        byte[] bodyBytes = string.IsNullOrEmpty(body) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        return request;
    }

    public void BeginNewSubmissionSession()
    {
        authToken = null;
        isAuthenticating = false;
    }

    private string NormalizeScheme(string rawScheme)
    {
        if (string.IsNullOrWhiteSpace(rawScheme))
        {
            return "http";
        }

        string normalized = rawScheme.Trim().ToLowerInvariant();
        if (normalized.EndsWith("://", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0, normalized.Length - 3);
        }

        if (normalized == "http" || normalized == "https")
        {
            return normalized;
        }

        return "http";
    }

    public static string ResolveDisplayName(LeaderboardRecord record)
    {
        if (record == null)
        {
            return "Guest";
        }

        if (!string.IsNullOrWhiteSpace(record.metadata))
        {
            try
            {
                string normalized = record.metadata.Trim();
                if (normalized.StartsWith("\"") && normalized.EndsWith("\""))
                {
                    normalized = normalized.Substring(1, normalized.Length - 2).Replace("\\\"", "\"");
                }

                LeaderboardNameMetadata byName = JsonUtility.FromJson<LeaderboardNameMetadata>(normalized);
                if (!string.IsNullOrWhiteSpace(byName.name))
                {
                    return byName.name;
                }
            }
            catch
            {
                // Keep fallback behavior.
            }
        }

        return "Guest";
    }

    public static string ResolveScore(LeaderboardRecord record)
    {
        if (record == null)
        {
            return "0";
        }

        if (long.TryParse(record.score, out long parsed))
        {
            return parsed.ToString();
        }

        return string.IsNullOrWhiteSpace(record.score) ? "0" : record.score;
    }
}
