from fastapi import FastAPI
from pydantic import BaseModel
import time
import json
import os
import sys
from pathlib import Path
from typing import Optional
import re

AGENT_FRAMEWORK_ROOT = os.environ.get("AGENT_FRAMEWORK_ROOT")
if AGENT_FRAMEWORK_ROOT:
    sys.path.insert(0, str(Path(AGENT_FRAMEWORK_ROOT).expanduser()))

try:
    from agent import Agent, AgentDeploy, create_registry
except ImportError:
    Agent = AgentDeploy = create_registry = None

app = FastAPI(title = "LLM NPC Demo Gateway")

class NpcChatRequest(BaseModel):
    npc_id:str
    npc_name: str
    player_message:str
    quest_state: str
    fallback_line: str
    has_watch: bool = False
    nearby_item: Optional[str] = None

class ToolArgs(BaseModel):
    next_state: str

class NpcChatResponse(BaseModel):
    reply: str
    tool_name:Optional[str] = None
    tool_args:Optional[ToolArgs] = None

CONTEXT_DIR = Path(__file__).parent / "agent_context"

def load_json(filename:str):
    with open(CONTEXT_DIR/filename, "r", encoding="utf-8") as file:
        return json.load(file)

PERSONAS = load_json("personas.json")
WORLD_CONTEXT = (CONTEXT_DIR / "world.md").read_text(encoding="utf-8")
WATCH_QUEST = load_json("quest_watch.json")

def format_persona(npc_id: str, fallback_name:str) -> str:
    persona = PERSONAS.get(npc_id)

    if persona is None:
        return fallback_name

    traits = "、".join(persona.get("traits",[]))
    return(
        f"{persona.get('name', fallback_name)}："
        f"特性={traits}；"
        f"说话风格={persona.get('speaking_style', '')}；"
        f"任务角色={persona.get('role_in_quest', '')}"  
    )

def format_allowed_transitions() -> str:
    lines = []

    for transition in WATCH_QUEST.get("allowed_transitions",[]):
        lines.append(
            f"- 如果当前状态是 {transition['from']}，并且{transition['condition']}，"
            f"你应该调用 {transition['tool']}(next_state=\"{transition['to']}\")."      
        )

    lines.append("- 不要让任务状态回退。不要调用不在 allowed_transitions 里的状态变化。")
    return "\n".join(lines)
    

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

def run_agent_reply(request: NpcChatRequest):
    if create_registry is None:
        raise RuntimeError("Agent framework not installed; using scripted demo fallback")
    captured_tool_name = None
    captured_tool_args = None

    registry = create_registry()

    @registry.tool(
        description = "Request Unity to update the current quest state. Use this only when the dialogue clearly advances the watch quest."
    )
    def update_quest_state(next_state: str) ->str:
        """update the watch quest state in Unity.
        
        Args:
        next_state: One of not_started, accepted_watch_quest, got_river_clue, watch_found, quest_completed.
        """
        nonlocal captured_tool_name,captured_tool_args
        captured_tool_name = "update_quest_state"
        captured_tool_args = ToolArgs(next_state=next_state)
        return f"ok" #Queued Unity quest state update: {next_state}

    npc_id = request.npc_id.strip()
    quest_state = request.quest_state.strip()

    system_prompt = f"""
你是Unity游戏里的 NPC Agent。你必须扮演当前NPC，并根据任务状态回答玩家。

当前NPC：
{format_persona(npc_id, request.npc_name)}

世界背景：
{WORLD_CONTEXT}

当前任务状态：
{quest_state}: {WATCH_QUEST["states"].get(quest_state, "未知状态")}

工具规则：
{format_allowed_transitions()}

Unity 当前事实:
- 玩家是否已经拥有怀表: {request.has_watch}
- 玩家附近物品: {request.nearby_item or "无"}

关键剧情约束:
- 当 npc_id 是 guard 且当前状态是 accepted_watch_quest 时，卫兵必须明确告诉玩家：昨晚河边芦苇荡有异常，有人影或翻找声，怀表可能在那里。
- 当 npc_id 是 elder 且当前状态是 got_river_clue，并且玩家说已经找到怀表时，长老应该感谢玩家并收下怀表。
- NPC 可以自然表演动作，但不要让动作描写盖过关键信息。

输出规则:
- 最终回答只能是当前 NPC 对玩家说的话。
- 不要提到工具、函数、状态机、quest_state、next_state、任务状态已更新。
- 不要用括号解释系统行为或工具结果。
- 如果你调用了工具，也不要在最终回答里说明工具调用结果。
- 台词必须自然地包含当前 NPC 应该给玩家的信息。
"""
    agent = Agent(
        client=AgentDeploy(temperature=0.4),
        system_prompt=system_prompt,
        registry=registry,
        max_rounds=3
    )

    user_text = f"玩家说: {request.player_message}"
    reply = agent.send(user_text)

    return reply,captured_tool_name,captured_tool_args


@app.get("/health")
def health():
    return{"ok":True}


@app.post("/npc/chat", response_model=NpcChatResponse)
def npc_chat(request: NpcChatRequest):
    npc_id = request.npc_id.strip()
    quest_state = request.quest_state.strip()

    print(
        f"world_facts: has_watch={request.has_watch},"
        f"nearby_item = {request.nearby_item}"
    )

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

    if npc_id == "elder" and quest_state == "watch_found":
        return "update_quest_state", ToolArgs(next_state="quest_completed")

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
