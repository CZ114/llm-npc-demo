from fastapi import FastAPI
from pydantic import BaseModel
import time

from typing import Optional

app = FastAPI(title = "LLM NPC Demo Gateway")

class NpcChatRequest(BaseModel):
    npc_id:str
    npc_name: str
    player_message:str
    quest_state: str
    fallback_line: str

class ToolArgs(BaseModel):
    next_state: str

class NpcChatResponse(BaseModel):
    reply: str
    tool_name:Optional[str] = None
    tool_args:Optional[ToolArgs] = None



REPLIES_BY_STATE = {
    "elder": {
        "not_started": "唉，年纪大了，记性也跟着走了……我那块祖传的怀表，不知又落在哪儿了。年轻人，你要是碰见，可得告诉我。",
        "accepted_watch_quest": "谢谢你愿意帮忙。你可以先去问问阿贵，他在村里消息最灵通。",
        "got_river_clue": "河边……原来如此。那块怀表也许真的掉在那里了。",
        "watch_found": "你真的找到了怀表。谢谢你，年轻人，现在我可以告诉你村子里真正发生的事了。",
    },
    "merchant": {
        "not_started": "客官随便看！小本生意，童叟无欺。不过嘛……在这村里，消息也是货。",
        "accepted_watch_quest": "怀表？这消息可不便宜。不过我听说铁牛最近总在河边巡逻，也许他知道些什么。",
        "got_river_clue": "看来铁牛已经告诉你河边的事了。那你最好快去看看。",
        "watch_found": "怀表找回来了？长老这下该放心了。你这趟没白跑。",
    },
    "guard": {
        "not_started": "站住，例行盘查。最近河边不太平，夜里少往那边去。",
        "accepted_watch_quest": "你在帮长老找怀表？那我直说了，昨晚河边确实有人影，我正想去查。",
        "got_river_clue": "河边的线索别声张。村里不是每个人都希望怀表被找回来。",
        "watch_found": "怀表找到了？很好。看来你比我想的可靠。",
    },
}

@app.get("/health")
def health():
    return{"ok":True}


@app.post("/npc/chat", response_model=NpcChatResponse)
def npc_chat(request:NpcChatRequest):
    npc_id = request.npc_id.strip()
    quest_state = request.quest_state.strip()
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

    tool_name, tool_args = decide_tool_call(npc_id, quest_state)
    if tool_name is not None:
        print(f"tool_call={tool_name}, next_state={tool_args.next_state}")

    return NpcChatResponse(
        reply=reply,
        tool_name=tool_name,
        tool_args=tool_args
    )

    return NpcChatResponse(reply=reply)

def decide_tool_call(npc_id:str, quest_state:str):
    if npc_id == "elder" and quest_state == "not_started":
        return "update_quest_state", ToolArgs(next_state="accepted_watch_quest")

    if npc_id == "guard" and quest_state == "accepted_watch_quest":
        return "update_quest_state", ToolArgs(next_state="got_river_clue")

    return None, None