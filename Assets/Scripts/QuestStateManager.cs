
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
}
