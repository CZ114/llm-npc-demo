
using System.IO.Pipes;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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
            // 对话进行中:只听关闭键,其他全停(提前返回,避免对话时还在检测/开新对话)
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

            StartCoroutine(agentClient.RequestReply(
                _nearest.npcId,
                _nearest.npcName,
                "你好，我想了解祖传怀表的事，也愿意帮忙寻找它。",
                questStateManager != null ? questStateManager.CurrentState : _nearest.questState,
                _nearest.dialogueLine,
                reply =>
                {
                    dialogueUI.Show(_nearest.npcName, reply);
                    if (questStateManager != null)
                    {
                        questStateManager.AdvanceAfterConversation(_nearest.npcId);
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
            anim.SetFloat("Speed", 0f);       // 不清零的话,对话中角色会原地跑步
            anim.SetFloat("MotionSpeed", 0f);
        }
    }
}
