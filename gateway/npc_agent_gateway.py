from fastapi import FastAPI
from pydantic import BaseModel
import time

app = FastAPI(title = "LLM NPC Demo Gateway")

class NpcChatRequest(BaseModel):
    npc_id:str
    npc_name: str
    player_message:str
    quest_state: str
    fallback_line: str

class NpcChatResponse(BaseModel):
    reply: str

REPLIES_BY_STATE = {
    "elder":{
        "not_started":"年轻人，我丢了一块旧怀表。若你愿意帮我寻找，我会告诉村子里面真正发生了什么。",
        "accepted_watch_quest": "那块怀表可能在集市附近。你可以先去问问阿贵，他消息很灵通。",
        "watch_found":"你真的找到了怀表！谢谢你，看来你值得知道村子的秘密....",
    },
    "merchant": {
        "not_started": "客人来得正好。你是在找什么东西吗？",
        "accepted_watch_quest": "怀表？我听说有人在集市后面的木箱旁见过类似的东西。",
        "watch_found": "看来你已经找到怀表了。长老这下该放心了。",
    },
    "guard": {
        "not_started": "站住。最近村里不太平，不要到处乱跑。",
        "accepted_watch_quest": "如果你在帮长老找怀表，那我不会拦你。但别惹麻烦。",
        "watch_found": "怀表找到了？很好，至少今天少了一件麻烦事。",
    },
}

@app.get("/health")
def health():
    return{"ok":True}


@app.post("/npc/chat", response_model=NpcChatResponse)
def npc_chat(request:NpcChatRequest):
    # print(
    #     f"npc_id={request.npc_id}, "
    #     f"npc_name={request.npc_name}, "
    #     f"quest_state={request.quest_state}, "
    #     f"fallback_line={request.fallback_line}"
    # )
        
    #time.sleep(1.0)
    npc_replies = REPLIES_BY_STATE.get(request.npc_id,{})
    # print("known npc ids:", list(REPLIES_BY_STATE.keys()))
    # print("known states for npc:", list(REPLIES_BY_STATE.get(request.npc_id, {}).keys()))

    # print("quest_state repr:", repr(request.quest_state), len(request.quest_state))
    # print("state keys repr:", [(repr(key), len(key), key == request.quest_state) for key in npc_replies.keys()])
    reply = npc_replies.get(request.quest_state)

    # print(f"matched_reply={reply}")
    # print("matched value direct:", npc_replies["accepted_watch_quest"])

    if reply is None:
        reply = request.fallback_line or "我现在还不知道该说什么。"

    return NpcChatResponse(reply=reply)