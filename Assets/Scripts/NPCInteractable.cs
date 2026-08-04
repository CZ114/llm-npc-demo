using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public string npcId = "npc";
    public string npcName;

    [TextArea]
    public string dialogueLine;
    public string questState = "not_started";

    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
