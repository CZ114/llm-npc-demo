
using UnityEngine;

public class QuestStateManager : MonoBehaviour
{
    [SerializeField] private string currentState = "not_started";

    public string CurrentState => currentState;

    public void AdvanceAfterConversation(string npcId)
    {
        if (currentState == "not_started" && npcId == "elder")
        {
            currentState = "accepted_watch_quest";
            Debug.Log("Quest advanced: accepted_watch_quest");
            return;
        }

        if (currentState == "accepted_watch_quest" && npcId == "guard")
        {
            currentState = "got_river_clue";
            Debug.Log("Quest advanced: got_river_clue");
            return;
        }
    }

    public void SetState(string nextState)
    {
        if (string.IsNullOrWhiteSpace(nextState))
        {
            Debug.LogWarning("Quest state update ignored: empty next state.");
            return;
        }

        if (nextState == currentState)
        {
            Debug.Log($"Quest state unchanged:{currentState}");
            return;
        }

        if (!CanTransition(currentState,nextState))
        {
            Debug.LogWarning($"Quest state update rejected:{currentState}");
            return;
        }

        Debug.Log($"Quest state changed: {currentState}->{nextState}");
        currentState = nextState;
    }

    private bool CanTransition(string fromState, string toState)
    {
        if (fromState == "not_started" && toState == "accepted_watch_quest")
        {
            return true;
        }

        if (fromState == "accepted_watch_quest" && toState == "watch_found")
        {
            return true;
        }

        return false;
    }
}
