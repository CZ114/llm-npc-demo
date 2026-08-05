
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private NPCInteractable _nearest;
    private bool _waitingForAgentReply;
    public float interactRange = 2.5f;

    public DialogueUI dialogueUI;
    public NpcAgentClient agentClient;

    public QuestStateManager questStateManager;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
            // 瀵硅瘽杩涜涓?鍙惉鍏抽棴閿?鍏朵粬鍏ㄥ仠(鎻愬墠杩斿洖,閬垮厤瀵硅瘽鏃惰繕鍦ㄦ娴?寮€鏂板璇?
    if (dialogueUI != null && dialogueUI.IsOpen)
    {
        if (!_waitingForAgentReply &&
            Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            dialogueUI.Hide();
            SetPlayerControl(true);
        }
        return;
    }
        FindNearestNpc();

        if (_nearest != null) dialogueUI.ShowPrompt(_nearest.npcName);
        else dialogueUI.HidePrompt();

        HandleInteractInput();
    }

    void FindNearestNpc()
    {
        _nearest = null;
        float bestDist = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
        foreach (Collider hit in hits)
        {
            NPCInteractable npc = hit.GetComponent<NPCInteractable>();
            if (npc == null) continue;

            float d = Vector3.Distance(transform.position, npc.transform.position);

            if (d <= bestDist)
            {
                _nearest = npc;
                bestDist = d;
            }
        }
        
    }

    string BuildPlayerMessage(string npcId, string questState)
    {
        if (questState == "not_started" && npcId == "elder")
        {
            return "你好，我想了解祖传怀表的事情，也愿意帮忙寻找它。";
        }

        if (questState == "accepted_watch_quest" && npcId == "guard")
        {
            return "闀胯€佽鎴戝鎵剧浼犳€€琛ㄣ€備綘鏈€杩戝湪娌宠竟鏈夋病鏈夊彂鐜板紓甯革紵";
        }

        if (questState == "got_river_clue" && npcId == "elder")
        {
            return "我顺着河边线索找到了这块怀表，应该是您的。";
        }

        return "你好，我想和你聊聊。";
    }

    void HandleInteractInput()
    {
        if (_waitingForAgentReply) return;

        if (_nearest && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            dialogueUI.Show(_nearest.npcName, "思考中...");
            SetPlayerControl(false);
            _waitingForAgentReply = true;

            if (agentClient == null)
            {
                dialogueUI.Show(_nearest.npcName, _nearest.dialogueLine);
                _waitingForAgentReply = false;
                return;
            }

            string currentQuestState = questStateManager != null ? questStateManager.CurrentState : _nearest.questState;
            string playerMessage = BuildPlayerMessage(_nearest.npcId,currentQuestState);

            StartCoroutine(agentClient.RequestReply(
                _nearest.npcId,
                _nearest.npcName,
                playerMessage,
                currentQuestState,
                _nearest.dialogueLine,
                (reply,toolName,nextState) =>
                {
                    dialogueUI.Show(_nearest.npcName, reply);
                    if (questStateManager != null && !string.IsNullOrWhiteSpace(nextState))
                    {
                        // questStateManager.AdvanceAfterConversation(_nearest.npcId);
                        questStateManager.SetState(nextState);
                    }
                    _waitingForAgentReply = false;
                },
                error =>
                {
                    Debug.LogWarning($"NPC agent request failed: {error}");
                    dialogueUI.Show(_nearest.npcName, _nearest.dialogueLine);
                    _waitingForAgentReply = false;
                }
            ));
        }
    }

    void SetPlayerControl(bool enabled)
    {
        var controller = GetComponent<StarterAssets.ThirdPersonController>();
        if (controller != null) controller.enabled = enabled;

        var anim = GetComponent<Animator>();
        if (anim != null && !enabled)
        {
            anim.SetFloat("Speed", 0f);       // 涓嶆竻闆剁殑璇?瀵硅瘽涓鑹蹭細鍘熷湴璺戞
            anim.SetFloat("MotionSpeed", 0f);
        }
    }
}
