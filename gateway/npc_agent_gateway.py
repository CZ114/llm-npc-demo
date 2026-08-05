from fastapi import FastAPI
from pydantic import BaseModel
import time
import json
import sys
from pathlib import Path
from typing import Optional
import re

AGENT_FRAMEWORK_ROOT = Path(r"D:\Imperial\individual\AgentFramework_build\project")
sys.path.insert(0, str(AGENT_FRAMEWORK_ROOT))

from agent import Agent, AgentDeploy, create_registry

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

NPC_PERSONAS = {
    "elder": "长老·沈鹤：慈祥、年迈、记性变差，但知道村子的旧秘密。说话温和，常把怀表称为祖传旧物。",
    "merchant": "商人·阿贵：精明、市侩、消息灵通。喜欢把情报当成商品，但不是真正坏人。",
    "guard": "卫兵·铁牛：嘴硬心软、守规矩、警惕河边异常。说话直接，表面严厉但会保护村民。",
}

WORLD_CONTEXT = """
场景是一个小村子。长老丢失了一块祖传怀表。
商人阿贵掌握村里的消息。
卫兵铁牛最近在河边巡逻，因为那里夜里不太平。
短任务线：长老丢怀表 -> 商人提示铁牛/河边 -> 卫兵给出河边线索 -> 玩家找回怀表。
"""

QUEST_STATE_DESCRIPTIONS = {
    "not_started": "玩家还没有正式接下寻找怀表的任务。",
    "accepted_watch_quest": "玩家已经答应长老寻找祖传怀表。",
    "got_river_clue": "玩家已经从卫兵处获得河边线索。",
    "watch_found": "玩家已经找回怀表。",
}

def run_agent_reply(request: NpcChatRequest):
    captured_tool_name = None
    captured_tool_args = None

    registry = create_registry()

    @registry.tool(
        description = "Request Unity to update the current quest state. Use this only when the dialogue clearly advances the watch quest."
    )
    def update_quest_sate(next_state: str) ->str:
        """update the watch quest state in Unity.
        
        Args:
        next_state: One of not_started, accepted_watch_quest, got_river_clue, watch_found.
        """
        nonlocal captured_tool_name,captured_tool_args
        captured_tool_name = "update_quest_state"
        captured_tool_args = ToolArgs(next_state=next_state)
        return f"Queued Unity quest state update: {next_state}"

    npc_id = request.npc_id.strip()
    quest_state = request.quest_state.strip()

    system_prompt = f"""
你是Unity游戏里的 NPC Agent。你必须扮演当前NPC，并根据任务状态回答玩家。

当前NPC：
{NPC_PERSONAS.get(npc_id, request.npc_name)}

世界背景：
{WORLD_CONTEXT}

当前任务状态：
{quest_state}: {QUEST_STATE_DESCRIPTIONS.get(quest_state, "未知状态")}

工具规则：
- 如果如果玩家第一次和长老对话，并且当前状态是 not_started，你应该调用 update_quest_state(next_state="accepted_watch_quest")。
- 如果玩家在 accepted_watch_quest 状态下和卫兵对话，你应该调用 update_quest_state(next_state="got_river_clue")。
- 不要随意跳到 watch_found，除非明确收到玩家已经找到怀表的输入。
- 最终回答必须是 NPC 对玩家说的一两句中文台词。
"""
    agent = Agent(
        client=AgentDeploy(temperature=0.4),
        system_prompt=system_prompt,
        registry=registry,
        max_rounds=3
    )

    user_text = f"玩家说{request.player_message}"
    reply = agent.send(user_text)

    return reply,captured_tool_name,captured_tool_args


@app.get("/health")
def health():
    return{"ok":True}


@app.post("/npc/chat", response_model=NpcChatResponse)
def npc_chat(request: NpcChatRequest):
    npc_id = request.npc_id.strip()
    quest_state = request.quest_state.strip()

    try:
        reply, tool_name, tool_args = run_agent_reply(request)
        reply = clean_agent_reply(reply)
        print(f"agent_reply={reply}")

        if tool_name is not None:
            print(f"agent_tool_call={tool_name}, next_state={tool_args.next_state}")

        return NpcChatResponse(
            reply=reply,
            tool_name=tool_name,
            tool_args=tool_args,
        )

    except Exception as error:
        print(f"agent_error={type(error).__name__}: {error}")

    npc_replies = REPLIES_BY_STATE.get(npc_id,{})
    reply = npc_replies.get(quest_state)

    if reply is None:
        reply = request.fallback_line or "我现在还不知道说什么。"
    tool_name,tool_args = decide_tool_call(npc_id,quest_state)
    return NpcChatResponse(
        reply=reply,
        tool_name=tool_name,
        tool_args=tool_args,
    )

def decide_tool_call(npc_id:str, quest_state:str):
    if npc_id == "elder" and quest_state == "not_started":
        return "update_quest_state", ToolArgs(next_state="accepted_watch_quest")

    if npc_id == "guard" and quest_state == "accepted_watch_quest":
        return "update_quest_state", ToolArgs(next_state="got_river_clue")

    return None, None

def clean_agent_reply(reply: str) -> str:
    cleaned = re.sub(r"<think>.*?</think>", "", reply, flags=re.DOTALL)
    return cleaned.strip()

# 旧版实现，保留作参考，不再启用。
# def old_npc_chat(request:NpcChatRequest):
#     npc_id = request.npc_id.strip()
#     quest_state = request.quest_state.strip()
#     # print(
#     #     f"npc_id={request.npc_id}, "
#     #     f"npc_name={request.npc_name}, "
#     #     f"quest_state={request.quest_state}, "
#     #     f"fallback_line={request.fallback_line}"
#     # )
#
#     #time.sleep(1.0)
#     npc_replies = REPLIES_BY_STATE.get(request.npc_id,{})
#     # print("known npc ids:", list(REPLIES_BY_STATE.keys()))
#     # print("known states for npc:", list(REPLIES_BY_STATE.get(request.npc_id, {}).keys()))
#
#     # print("quest_state repr:", repr(request.quest_state), len(request.quest_state))
#     # print("state keys repr:", [(repr(key), len(key), key == request.quest_state) for key in npc_replies.keys()])
#     reply = npc_replies.get(request.quest_state)
#
#     # print(f"matched_reply={reply}")
#     # print("matched value direct:", npc_replies["accepted_watch_quest"])
#
#     if reply is None:
#         reply = request.fallback_line or "我现在还不知道该说什么。"
#
#     tool_name, tool_args = decide_tool_call(npc_id, quest_state)
#     if tool_name is not None:
#         print(f"tool_call={tool_name}, next_state={tool_args.next_state}")
#
#     return NpcChatResponse(
#         reply=reply,
#         tool_name=tool_name,
#         tool_args=tool_args
#     )
#
#     return NpcChatResponse(reply=reply)