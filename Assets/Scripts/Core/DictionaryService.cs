using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DictionaryService : MonoBehaviour
{
    private static DictionaryService _instance;
    public static DictionaryService Instance => _instance;

    private Dictionary<string, DictionaryResult> cache
        = new Dictionary<string, DictionaryResult>(StringComparer.OrdinalIgnoreCase);

    private const string API_URL = "https://api.dictionaryapi.dev/api/v2/entries/en/";
    private const string WIKTIONARY_URL = "https://en.wiktionary.org/api/rest_v1/page/definition/";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    // ─── Data Model ───

    [Serializable]
    public class DictionaryResult
    {
        public string word;
        public string phonetic;
        public List<Meaning> meanings = new List<Meaning>();
        public bool isError;
        public string errorMessage;
    }

    [Serializable]
    public class Meaning
    {
        public string partOfSpeech;
        public List<DefinitionEntry> definitions = new List<DefinitionEntry>();
    }

    [Serializable]
    public class DefinitionEntry
    {
        public string definition;
        public string example;
    }

    // ─── Public API ───

    public void FetchDefinition(string word, Action<DictionaryResult> onComplete)
    {
        string key = word.ToLower();
        if (cache.TryGetValue(key, out DictionaryResult cached))
        {
            onComplete?.Invoke(cached);
            return;
        }
        StartCoroutine(FetchCoroutine(key, onComplete));
    }

    // ─── Internals ───

    private IEnumerator FetchCoroutine(string word, Action<DictionaryResult> onComplete)
    {
        // Try Free Dictionary API first
        string url = API_URL + UnityWebRequest.EscapeURL(word);
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                DictionaryResult result = ParseResponse(word, json);
                cache[word] = result;
                onComplete?.Invoke(result);
                yield break;
            }

            // If not a 404, it's a network error — don't try fallback
            if (request.responseCode != 404)
            {
                var errorResult = new DictionaryResult
                {
                    word = word,
                    isError = true,
                    errorMessage = "Could not connect. Check your internet connection."
                };
                cache[word] = errorResult;
                onComplete?.Invoke(errorResult);
                yield break;
            }
        }

        // Fallback: try Wiktionary API
        yield return FetchWiktionaryCoroutine(word, onComplete);
    }

    private IEnumerator FetchWiktionaryCoroutine(string word, Action<DictionaryResult> onComplete)
    {
        string url = WIKTIONARY_URL + UnityWebRequest.EscapeURL(word);
        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("User-Agent", "WordyWordsGame/1.0");
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                var errorResult = new DictionaryResult
                {
                    word = word,
                    isError = true,
                    errorMessage = "This is a valid word, but no dictionary definition is available."
                };
                cache[word] = errorResult;
                onComplete?.Invoke(errorResult);
                yield break;
            }

            string json = request.downloadHandler.text;
            DictionaryResult result = ParseWiktionaryResponse(word, json);
            cache[word] = result;
            onComplete?.Invoke(result);
        }
    }

    private DictionaryResult ParseResponse(string word, string json)
    {
        // The API returns a root JSON array: [{...}]
        // JsonUtility can't parse root arrays, so use manual parsing
        try
        {
            var result = new DictionaryResult { word = word };

            // Extract phonetic
            int phoneticIdx = json.IndexOf("\"phonetic\"");
            if (phoneticIdx >= 0)
            {
                string phonetic = ExtractStringValue(json, phoneticIdx);
                if (phonetic != null)
                    result.phonetic = phonetic;
            }

            // Extract meanings array
            int meaningsIdx = json.IndexOf("\"meanings\"");
            if (meaningsIdx < 0)
                return result;

            int arrStart = json.IndexOf('[', meaningsIdx);
            if (arrStart < 0) return result;

            int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
            if (arrEnd < 0) return result;

            string meaningsJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

            // Parse each meaning object
            int searchFrom = 0;
            while (searchFrom < meaningsJson.Length)
            {
                int objStart = meaningsJson.IndexOf('{', searchFrom);
                if (objStart < 0) break;

                int objEnd = FindMatchingBracket(meaningsJson, objStart, '{', '}');
                if (objEnd < 0) break;

                string meaningObj = meaningsJson.Substring(objStart, objEnd - objStart + 1);
                var meaning = ParseMeaning(meaningObj);
                if (meaning != null)
                    result.meanings.Add(meaning);

                searchFrom = objEnd + 1;
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DictionaryService] Parse error: {e.Message}");
            return new DictionaryResult
            {
                word = word,
                isError = true,
                errorMessage = "Failed to parse definition."
            };
        }
    }

    private Meaning ParseMeaning(string json)
    {
        var meaning = new Meaning();

        // Extract partOfSpeech
        int posIdx = json.IndexOf("\"partOfSpeech\"");
        if (posIdx >= 0)
            meaning.partOfSpeech = ExtractStringValue(json, posIdx);

        // Extract definitions array
        int defsIdx = json.IndexOf("\"definitions\"");
        if (defsIdx < 0) return meaning;

        int arrStart = json.IndexOf('[', defsIdx);
        if (arrStart < 0) return meaning;

        int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
        if (arrEnd < 0) return meaning;

        string defsJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

        // Parse each definition object
        int searchFrom = 0;
        while (searchFrom < defsJson.Length)
        {
            int objStart = defsJson.IndexOf('{', searchFrom);
            if (objStart < 0) break;

            int objEnd = FindMatchingBracket(defsJson, objStart, '{', '}');
            if (objEnd < 0) break;

            string defObj = defsJson.Substring(objStart, objEnd - objStart + 1);
            var entry = new DefinitionEntry();

            int defIdx = defObj.IndexOf("\"definition\"");
            if (defIdx >= 0)
                entry.definition = ExtractStringValue(defObj, defIdx);

            int exIdx = defObj.IndexOf("\"example\"");
            if (exIdx >= 0)
                entry.example = ExtractStringValue(defObj, exIdx);

            if (!string.IsNullOrEmpty(entry.definition))
                meaning.definitions.Add(entry);

            searchFrom = objEnd + 1;
        }

        return meaning;
    }

    private static string ExtractStringValue(string json, int keyIndex)
    {
        int colonIdx = json.IndexOf(':', keyIndex);
        if (colonIdx < 0) return null;

        int quoteStart = json.IndexOf('"', colonIdx + 1);
        if (quoteStart < 0) return null;

        int quoteEnd = quoteStart + 1;
        while (quoteEnd < json.Length)
        {
            if (json[quoteEnd] == '"' && json[quoteEnd - 1] != '\\')
                break;
            quoteEnd++;
        }
        if (quoteEnd >= json.Length) return null;

        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1)
            .Replace("\\\"", "\"")
            .Replace("\\n", "\n")
            .Replace("\\\\", "\\");
    }

    private static int FindMatchingBracket(string json, int openIdx, char open, char close)
    {
        int depth = 0;
        bool inString = false;
        for (int i = openIdx; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\'))
            {
                inString = !inString;
                continue;
            }
            if (inString) continue;

            if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    // ─── Wiktionary Parsing ───

    private DictionaryResult ParseWiktionaryResponse(string word, string json)
    {
        try
        {
            var result = new DictionaryResult { word = word };

            // Wiktionary returns: {"en": [{"partOfSpeech": "Noun", "definitions": [{"definition": "<html>..."}]}]}
            // Find the "en" array
            int enIdx = json.IndexOf("\"en\"");
            if (enIdx < 0)
                return new DictionaryResult { word = word, isError = true, errorMessage = "No English definition found." };

            int arrStart = json.IndexOf('[', enIdx);
            if (arrStart < 0) return result;

            int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
            if (arrEnd < 0) return result;

            string enJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

            // Parse each part-of-speech object
            int searchFrom = 0;
            while (searchFrom < enJson.Length)
            {
                int objStart = enJson.IndexOf('{', searchFrom);
                if (objStart < 0) break;

                int objEnd = FindMatchingBracket(enJson, objStart, '{', '}');
                if (objEnd < 0) break;

                string posObj = enJson.Substring(objStart, objEnd - objStart + 1);
                var meaning = ParseWiktionaryMeaning(posObj);
                if (meaning != null && meaning.definitions.Count > 0)
                    result.meanings.Add(meaning);

                searchFrom = objEnd + 1;
            }

            if (result.meanings.Count == 0)
            {
                result.isError = true;
                result.errorMessage = "This is a valid word, but no dictionary definition is available.";
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DictionaryService] Wiktionary parse error: {e.Message}");
            return new DictionaryResult
            {
                word = word,
                isError = true,
                errorMessage = "Failed to parse definition."
            };
        }
    }

    private Meaning ParseWiktionaryMeaning(string json)
    {
        var meaning = new Meaning();

        int posIdx = json.IndexOf("\"partOfSpeech\"");
        if (posIdx >= 0)
            meaning.partOfSpeech = ExtractStringValue(json, posIdx);

        int defsIdx = json.IndexOf("\"definitions\"");
        if (defsIdx < 0) return meaning;

        int arrStart = json.IndexOf('[', defsIdx);
        if (arrStart < 0) return meaning;

        int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
        if (arrEnd < 0) return meaning;

        string defsJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

        int searchFrom = 0;
        while (searchFrom < defsJson.Length)
        {
            int objStart = defsJson.IndexOf('{', searchFrom);
            if (objStart < 0) break;

            int objEnd = FindMatchingBracket(defsJson, objStart, '{', '}');
            if (objEnd < 0) break;

            string defObj = defsJson.Substring(objStart, objEnd - objStart + 1);
            var entry = new DefinitionEntry();

            int defIdx = defObj.IndexOf("\"definition\"");
            if (defIdx >= 0)
            {
                string raw = ExtractStringValue(defObj, defIdx);
                if (raw != null)
                    entry.definition = StripHtml(raw);
            }

            if (!string.IsNullOrEmpty(entry.definition))
                meaning.definitions.Add(entry);

            searchFrom = objEnd + 1;
        }

        return meaning;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;

        // Remove HTML tags
        var sb = new System.Text.StringBuilder(html.Length);
        bool inTag = false;
        for (int i = 0; i < html.Length; i++)
        {
            char c = html[i];
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(c);
        }

        // Decode common HTML entities
        string text = sb.ToString();
        text = text.Replace("&amp;", "&");
        text = text.Replace("&lt;", "<");
        text = text.Replace("&gt;", ">");
        text = text.Replace("&quot;", "\"");
        text = text.Replace("&#39;", "'");
        text = text.Replace("&nbsp;", " ");

        return text.Trim();
    }
}