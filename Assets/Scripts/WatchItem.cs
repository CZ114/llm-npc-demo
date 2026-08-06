
using UnityEngine;
using UnityEngine.InputSystem;

public class WatchItem : MonoBehaviour
{
    [SerializeField] private QuestStateManager questStateManager;
    [SerializeField] private Transform player;
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private string requiredState = "got_river_clue";
    [SerializeField] private string nextState = "watch_found";

    
    
    private void Start()
    {
        if (questStateManager == null)
        {
            questStateManager = FindFirstObjectByType<QuestStateManager>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
        
    }

    private void Update()
    {
        if (questStateManager == null || player == null)
        {
            return;
        }

        if (questStateManager.CurrentState != requiredState)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > pickupRange)
        {
            return;
        }

        questStateManager.SetState(nextState);
        Debug.Log("Watch picked up.");
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }

}
