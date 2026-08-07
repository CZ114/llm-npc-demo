using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    private NPCInteractable _nearest;
    private bool _waitingForAgentReply;
    private bool _acceptQuestRequested;

    public float interactRange = 2.5f;
    public DialogueUI dialogueUI;
    public NpcAgentClient agentClient;
    public QuestStateManager questStateManager;

    void Start()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>();
        }

        if (agentClient == null)
        {
            agentClient = FindFirstObjectByType<NpcAgentClient>();
        }

        if (questStateManager == null)
        {
            questStateManager = FindFirstObjectByType<QuestStateManager>();
        }
    }

    void Update()
    {
        if (dialogueUI != null && dialogueUI.IsOpen)
        {
            HandleDialogueInput();
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

            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance <= bestDist)
            {
                _nearest = npc;
                bestDist = distance;
            }
        }
    }

    string BuildPlayerMessage(string npcId, string questState)
    {
        if (questState == "not_started" && npcId == "elder")
        {
            return "您好，听说您最近有烦心事？";
        }

        if (questState == "accepted_watch_quest" && npcId == "guard")
        {
            return "长老让我寻找祖传怀表。你最近在河边有没有发现异常？";
        }

        if (questState == "got_river_clue" && npcId == "elder")
        {
            return "我问到了河边的线索，但还没有找到怀表。";
        }

        if (questState == "watch_found" && npcId == "elder")
        {
            return "我顺着河边线索找到了这块怀表，应该是您的。";
        }

        return "你好，我想和你聊聊。";
    }

    string BuildAcceptQuestMessage(string npcId, string questState)
    {
        if (questState == "not_started" && npcId == "elder")
        {
            return "我愿意帮您寻找怀表。";
        }

        return "";
    }

    bool CanAcceptCurrentQuest()
    {
        if (_nearest == null || _acceptQuestRequested) return false;

        string currentQuestState = GetCurrentQuestState();
        return _nearest.npcId == "elder" && currentQuestState == "not_started";
    }

    string GetCurrentQuestState()
    {
        return questStateManager != null ? questStateManager.CurrentState : _nearest.questState;
    }

    void HandleDialogueInput()
    {
        if (_waitingForAgentReply || Keyboard.current == null) return;

        if (CanAcceptCurrentQuest() && Keyboard.current.fKey.wasPressedThisFrame)
        {
            _acceptQuestRequested = true;
            string currentQuestState = GetCurrentQuestState();
            string playerMessage = BuildAcceptQuestMessage(_nearest.npcId, currentQuestState);
            SendNpcMessage(_nearest, playerMessage, true);
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            dialogueUI.Hide();
            SetPlayerControl(true);
        }
    }

    void HandleInteractInput()
    {
        if (_waitingForAgentReply) return;

        if (_nearest != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            string currentQuestState = GetCurrentQuestState();
            string playerMessage = BuildPlayerMessage(_nearest.npcId, currentQuestState);
            SendNpcMessage(_nearest, playerMessage, false);
        }
    }

    void SendNpcMessage(NPCInteractable npc, string playerMessage, bool isAcceptAction)
    {
        if (npc == null || _waitingForAgentReply) return;

        dialogueUI.Show(npc.npcName, "思考中...");
        SetPlayerControl(false);
        _waitingForAgentReply = true;

        if (agentClient == null)
        {
            dialogueUI.Show(npc.npcName, AddDialogueHint(npc.dialogueLine, npc.npcId, GetCurrentQuestState(), isAcceptAction));
            _waitingForAgentReply = false;
            return;
        }

        string currentQuestState = questStateManager != null ? questStateManager.CurrentState : npc.questState;

        StartCoroutine(agentClient.RequestReply(
            npc.npcId,
            npc.npcName,
            playerMessage,
            currentQuestState,
            npc.dialogueLine,
            currentQuestState == "watch_found",
            "",
            (reply, toolName, nextState) =>
            {
                if (questStateManager != null && toolName == "update_quest_state" && !string.IsNullOrWhiteSpace(nextState))
                {
                    if (ShouldApplyToolCall(npc.npcId, currentQuestState, nextState, isAcceptAction))
                    {
                        questStateManager.SetState(nextState);
                    }
                    else
                    {
                        Debug.LogWarning($"Quest state update ignored until explicit accept: {currentQuestState}->{nextState}");
                    }
                }

                if (questStateManager != null &&
                    npc.npcId == "elder" &&
                    currentQuestState == "watch_found" &&
                    string.IsNullOrWhiteSpace(nextState))
                {
                    questStateManager.SetState("quest_completed");
                }

                dialogueUI.Show(npc.npcName, AddDialogueHint(reply, npc.npcId, currentQuestState, isAcceptAction));
                _waitingForAgentReply = false;
            },
            error =>
            {
                Debug.LogWarning($"NPC agent request failed: {error}");
                dialogueUI.Show(npc.npcName, AddDialogueHint(npc.dialogueLine, npc.npcId, currentQuestState, isAcceptAction));
                _waitingForAgentReply = false;
            }
        ));
    }

    bool ShouldApplyToolCall(string npcId, string currentQuestState, string nextState, bool isAcceptAction)
    {
        if (npcId == "elder" && currentQuestState == "not_started" && nextState == "accepted_watch_quest")
        {
            return isAcceptAction;
        }

        return true;
    }

    string AddDialogueHint(string reply, string npcId, string currentQuestState, bool isAcceptAction)
    {
        if (!isAcceptAction && npcId == "elder" && currentQuestState == "not_started")
        {
            return "[F] 接受任务    [E/Esc] 关闭\n\n" + reply;
        }

        return reply;
    }

    void SetPlayerControl(bool enabled)
    {
        var controller = GetComponent<StarterAssets.ThirdPersonController>();
        if (controller != null) controller.enabled = enabled;

        var anim = GetComponent<Animator>();
        if (anim != null && !enabled)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("MotionSpeed", 0f);
        }
    }
}