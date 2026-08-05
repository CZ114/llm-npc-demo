using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NpcAgentClient : MonoBehaviour
{
    [SerializeField] private string endpoint = "http://127.0.0.1:8787/npc/chat";

    [Serializable]
    private class NpcChatRequest
    {
        public string npc_id;
        public string npc_name;
        public string player_message;
        public string quest_state;
        public string fallback_line;
    }
    [Serializable]
    private class ToolArgs
    {
        public string next_state;
    }

    [Serializable]
    private class NpcChatResponse
    {
        public string reply;
        public string tool_name;
        public ToolArgs tool_args;
    }

    public IEnumerator RequestReply(
        string npcId,
        string npcName,
        string playerMessage,
        string questState,
        string npcLine,
        Action<string,string,string> onSuccess,
        Action<string> onError
    )
    {
        var requestBody = new NpcChatRequest
        {
            npc_id = npcId,
            npc_name = npcName,
            player_message = playerMessage,
            quest_state = questState,
            fallback_line = npcLine
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        var response = JsonUtility.FromJson<NpcChatResponse>(responseText);

        if (response == null || string.IsNullOrWhiteSpace(response.reply))
        {
            onError?.Invoke("Empty reply from NPC agent.");
            yield break;
        }

        onSuccess?.Invoke(
            response.reply,
            response.tool_name,
            response.tool_args != null ? response.tool_args.next_state:null
        );
    }

}
