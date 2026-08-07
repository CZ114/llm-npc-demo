using UnityEngine;
using UnityEngine.InputSystem;

public class WatchItem : MonoBehaviour
{
    [SerializeField] private QuestStateManager questStateManager;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private Transform player;
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private string requiredState = "got_river_clue";
    [SerializeField] private string nextState = "watch_found";

    private bool _showingPickupPrompt;

    private void Start()
    {
        if (questStateManager == null)
        {
            questStateManager = FindFirstObjectByType<QuestStateManager>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (questStateManager == null || dialogueUI == null || player == null)
        {
            return;
        }

        if (dialogueUI.IsOpen)
        {
            return;
        }

        bool canPickup = questStateManager.CurrentState == requiredState;
        float distance = Vector3.Distance(transform.position, player.position);
        bool playerInRange = distance <= pickupRange;

        if (canPickup && playerInRange)
        {
            dialogueUI.ShowPromptText("按 E 拾取怀表");
            _showingPickupPrompt = true;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                questStateManager.SetState(nextState);
                Debug.Log("Watch picked up.");
                dialogueUI.HidePrompt();
                gameObject.SetActive(false);
            }

            return;
        }

        if (_showingPickupPrompt)
        {
            dialogueUI.HidePrompt();
            _showingPickupPrompt = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}