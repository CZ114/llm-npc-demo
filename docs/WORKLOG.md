# LLM NPC Demo 工作日志

> 记录规则:每完成一个工作块追加一条,标注**谁做的**(Claude 代劳 / 用户手动 / 结对)。
> 技能档位含义:A = 面试相关、用户必须亲手做;B = 懂概念即可;C = 纯杂务 Claude 全包。
> 完整计划:`E:\简历准备\targets\internships\unity-npc-demo-plan.md`

---

## 2026-07-31

- **[用户手动]** 完成 Unity Learn Roll-a-Ball 教程(旧练手工程 `My project (2)`):场景搭建、PlayerController/CameraController/Rotator 脚本、NavMesh 敌人、Build 出包。第 1 周引擎入门主体完成。
- **[Claude 代劳,C 档]** 调研并选定 CoplayDev MCP for Unity(弃 Unity 官方 pre-release 方案);在练手工程配置 MCP(manifest + .mcp.json),排查连接问题(端口/配置作用域/服务启动方式),最终打通 Claude→Python 服务→Unity 三层链路,远程创建测试方块验证成功。

## 2026-08-01

- **[用户手动]** 用 Unity Hub 创建正式 demo 工程 `llm-npc-demo`(URP 空白模板,E:\unity\llm-npc-demo);Hub 自动建了私有 GitHub 仓库 CZ114/llm-npc-demo 并完成首次提交。
- **[Claude 代劳,C 档]** 新工程接入 MCP(manifest 加包 + .mcp.json 注册),验证插件注册成功。
- **[用户手动]** Asset Store 领取并导入 Starter Assets: Character Controllers | URP(选型依据:Unity 6 时代唯一在维护版,老 ThirdPerson 包已停更)。
- **[结对]** Claude 远程打开 ThirdPerson Playground 场景,用户试玩验证(移动/跳跃/冲刺/相机);拆解 PlayerArmature 组件构成,通读 ThirdPersonController.cs(组件模型、生命周期、Move/JumpAndGravity、动画参数与 Blend Tree 概念)。
- **[结对]** 制定技能分档(A/B/C)与教学分工模式,存入长期记忆;建立本工作日志。
- **[结对]** 用户质疑"手动搭场景是否有必要"→ 重新评估:建场景/摆物体在 Roll-a-Ball 已练过,重复无增量 → 调整分工,场景搭建改由 Claude 代劳,用户直接从交互脚本(真正的 A 档增量)开始。
- **[Claude 代劳,C 档]** 通过 MCP 远程搭建 NPCDemo 场景:Ground(Plane ×4)、三个 NPC 胶囊(NPC_Elder/NPC_Merchant/NPC_Guard)、玩家角色包、平行光,场景已保存。插曲:官方 Tools 部署菜单在新版包目录结构下有 bug("Couldn't find player armature prefab"),改用 NestedParentArmature_Unpack 组合预制体经 execute_code 实例化解决。

- **[用户手动,A 档]** NPCInteractable 数据脚本完成并挂到 3 个 NPC(长老·沈鹤/商人·阿贵/卫兵·铁牛,台词埋了"寻回怀表"任务线钩子)。review 抓到长老/商人数据互换 bug,用户修复。
- **[用户手动,A 档]** PlayerInteractor 检测脚本完成:OverlapSphere 轮询方案(方案 A)+ 擂台法找最近 NPC + Input System 按 E 打印台词。三轮 review 迭代,踩过并修复的坑:字段未声明、类型大小写、IDE 自动补全塞垃圾(NamedPipeClientStream / System.Diagnostics.Debug 歧义)、擂台不更新 bestDist(顺序依赖的隐性 bug)、方法写了没人调用、脚本挂错对象 ×2(Ground → Armature_Mesh → PlayerArmature)。
- **[Claude 代劳,C 档]** 修复玩家出生点 y=-0.93 埋地问题(预制体带来的 Playground 地板高度),移到 (0, 0.1, -2)。
- **验收通过**:近处 E 打印台词 / 远处无反应 / 双 NPC 择近。检测层完成。

## 2026-08-02

- **[Claude 代劳,B/C 档]** DialogueUI.cs:运行时自建 Canvas(对话面板/提示条,legacy Text 走系统字体渲染中文),对外仅暴露 ShowPrompt/HidePrompt/Show/Hide/IsOpen;场景里建了 DialogueUI 载体物体。
- **[用户手动,A 档]** PlayerInteractor 接入 UI:对话状态机(IsOpen 分流 + 提前 return 防同帧按键二次消费)、SetPlayerControl 冻结/解冻(含动画参数清零)、Inspector 拖引用接线。深入理解了:序列化字段"长"出 Inspector 槽位、GetComponent 只搜自身 GameObject、防御性判空会掩盖故障(fail-fast 取舍)。
- **排错记录**:组件实际一直在 Armature_Mesh(上次搬家未成),导致 SetPlayerControl 静默空转(TPC/Animator 在父物体上,GetComponent 返回 null 被判空吞掉)→ 搬到 PlayerArmature 后重接引用,行为验收新增"对话中 WASD 不可移动"一条。
- **✅ 里程碑达成:哑巴 NPC 对话闭环**——提示浮现 → E 开框冻人 → E/Esc 关框解冻,三 NPC 择近对话,台词含"寻回怀表"任务钩子。本次 git 提交为第一个功能提交。

### 下一步(第 2 周主线)

把 DialogueUI.Show 的数据源从 NPCInteractable 硬编码台词换成 HTTP 调用自研 Agent 框架(FastAPI 网关):Unity 侧 UnityWebRequest/async 上报观测 + 拿回复,Python 侧 Persona 注入。先跑通单轮对话,再加流式/记忆。

## 2026-08-05

- **[结对,A 档]** 完成 Unity→FastAPI 单轮 NPC Agent 网关闭环:用户手写/接线 `NpcAgentClient.cs`、`PlayerInteractor.cs` 与 `gateway/npc_agent_gateway.py`,Codex 接手 Claude 记忆后给骨架、逐步 review 和排错。功能扩展为 Unity 侧通过 `UnityWebRequest` POST `/npc/chat`,上报 `npc_id / npc_name / player_message / quest_state / fallback_line`,后端按 `npc_id + quest_state` 返回 `reply`,DialogueUI 从硬编码 `NPCInteractable.dialogueLine` 过渡到网关回复。
- **[用户手动,A 档]** 扩展 NPC 数据协议:`NPCInteractable` 增加稳定 `npcId` 与 `questState`,Inspector 为 elder/merchant/guard 配置状态;`PlayerInteractor` 增加 `agentClient` 引用、"思考中..."等待提示、等待期间冻结玩家控制与 `_waitingForAgentReply` 请求锁,避免快速连按 E 造成多次 POST 或提前关闭对话。
- **排错记录**:先修复 `NpcAgentClient.endpoint` 未使用 warning(补全实际 HTTP 请求后消失);随后在"后端 200 OK 但 Unity 一直显示原 Dialogue Line"场景下,通过打印 `npc_id / quest_state / known states / repr / matched_reply` 定位到 Python 第 56 行误用 `npc_replies.get(request.npc_id)` 查状态字典,导致匹配失败后走 `fallback_line`;改为按 `request.quest_state` 查找后验收通过。
- **剧情校准**:核对 Claude 记忆和聊天记录后确认正式任务线应沿用"长老丢祖传怀表 → 商人提供情报 → 卫兵给出河边线索 → 找回怀表";当前状态分支验证已跑通,下一步清理临时 debug print,把网关台词从临时"集市/木箱"修正回"河边线索",再接 `QuestStateManager` / `update_quest_state` 工具让状态自动推进。
## 2026-08-05

- **[用户手动,A 档]** 完成 `QuestStateManager.cs` 最小任务状态机并接入 `PlayerInteractor`:场景新增 `QuestManager` 对象,全局维护 `currentState`,Unity 对话请求改为读取 `questStateManager.CurrentState` 而不是每个 NPC Inspector 上的静态 `questState`。
- **[结对,A 档]** 打通自动状态推进链路:与长老对话后由 `not_started` 推进到 `accepted_watch_quest`,与卫兵对话后推进到 `got_river_clue`;NPC 后端回复开始由全局任务状态驱动,验证商人/卫兵能根据新状态返回对应怀表线索台词。
- **排错/设计记录**:本阶段先保持本地 C# 状态机推进,不急着接 LLM tool-call,原因是先建立可控 baseline:Unity 负责可靠状态落盘和输入事件,FastAPI 只按 `npc_id + quest_state` 生成回复。下一步再把状态推进迁移为显式 `update_quest_state` 工具协议,用于展示 Agent Tool Use。
- **[结对,A 档]** Agent 框架接入前完成阶段边界确认:本阶段 MVP 定为"真实 Agent 框架驱动的一条短任务闭环",而不是完整长剧情系统。验收目标是 3 NPC / 4 状态怀表任务闭环(`not_started -> accepted_watch_quest -> got_river_clue -> watch_found`),Agent 根据 Persona、世界背景、任务状态与玩家输入生成回复,并至少返回一次 `update_quest_state` 工具调用。
- **设计决策**:`QuestStateManager` 后续从"剧情判断器"降级为 Unity 侧"状态保存/校验/执行器";剧情推进逻辑迁移到 Agent/tool-call 输出。Unity 负责 observation 上报与 action 执行,FastAPI 网关负责适配 D 盘自研 Agent 框架,避免继续把 mock 后端做复杂。
- **下一步**:只读检查 `D:\Imperial\individual\Music!!!\project\agent` 的 Agent/Tool Registry/Memory/FastAPI 入口,再决定 `gateway/npc_agent_gateway.py` 是直接 import 框架还是转发到框架服务。
- **[结对,A 档]** 接入真实 D 盘自研 Agent 框架并跑通 NPC Agent 回复:只读确认旧记忆路径 `D:\Imperial\individual\Music!!!\project\agent` 已不存在,实际框架位于 `D:\Imperial\individual\AgentFramework_build\project`,核心入口为 `Agent / AgentDeploy / create_registry / @registry.tool`。`gateway/npc_agent_gateway.py` 通过 `sys.path` 作为 adapter import 框架,FastAPI `/npc/chat` 从 mock 字典回复升级为优先调用真实 Agent,失败时仍保留 `REPLIES_BY_STATE` fallback,保证 Unity demo 不因 LLM/API 问题中断。
- **[用户手动,A 档]** 启动真实 Agent 网关并完成端到端验证:使用 `uvx --with fastapi --with uvicorn --with openai --with python-dotenv uvicorn npc_agent_gateway:app --host 127.0.0.1 --port 8787`,后端成功输出 `agent_reply=...`,Unity UI 成功显示真实 Agent 生成的 NPC 台词。排错场景:最初后端已打印 `agent_reply` 但 UI 仍显示旧预设台词,定位为 `return NpcChatResponse(...)` 缩进/位置不对,在 `tool_name is None` 时继续落入 fallback;修正 return 位置后通过。
- **[结对,A 档]** 跑通真实 Agent Tool Use 雏形:后端注册 `update_quest_state(next_state)` 工具,通过更明确的 Unity `player_message`("你好，我想了解祖传怀表的事，也愿意帮忙寻找它。") 让 Agent 在长老 `not_started` 场景稳定触发 `agent_tool_call=update_quest_state, next_state=accepted_watch_quest`。同时增加 `clean_agent_reply()` 清理模型返回中的 `<think>...</think>`,避免游戏 UI 暴露思考过程。
- **下一步**:明天继续 Unity 侧执行真实 tool call:扩展 `NpcAgentClient` 解析 `tool_name/tool_args`,给 `QuestStateManager` 增加 `SetState(nextState)` 和合法状态校验,再把 `PlayerInteractor` 从旧的 `AdvanceAfterConversation(...)` 本地硬编码推进切换为执行后端 Agent 返回的 `update_quest_state`。
- **[用户手动,A 档]** 完成真实 Agent Tool Use 的 Unity 执行链路:扩展 `NpcAgentClient` 解析后端返回的 `tool_name/tool_args.next_state`,在 `PlayerInteractor` 成功回调中执行 `update_quest_state`,并把状态推进从旧的 `AdvanceAfterConversation(...)` 本地硬编码改为由真实 Agent 返回工具调用后驱动。
- **[结对,A 档]** 强化 `QuestStateManager` 为 Unity 侧状态裁判:新增 `SetState(nextState)` 与 `CanTransition(fromState,toState)` 合法状态转移校验,允许 `not_started -> accepted_watch_quest -> got_river_clue -> watch_found`,拒绝 `got_river_clue -> accepted_watch_quest` 等回退/跳跃。排错场景:真实 Agent 在已获得河边线索后仍可能再次建议回到 `accepted_watch_quest`;修复原则是 LLM 只给建议,Unity 状态机拥有最终写权限。
- **[结对,A 档]** 跑通完整短剧情 MVP 闭环:后端 prompt 规则扩展到 `watch_found`,Unity 侧新增按 `npcId + questState` 构造玩家输入的 `BuildPlayerMessage(...)`,避免所有 NPC/状态都收到同一句话导致 Agent 重复触发旧工具。验收顺序为长老 `not_started -> accepted_watch_quest`,卫兵 `accepted_watch_quest -> got_river_clue`,再找长老 `got_river_clue -> watch_found`,测试通过。
- **下一步**:把当前仍偏 demo-script 的内容整理为可维护 Agent context:拆出 `personas/world/quest` 配置,或优先按 D 盘框架原生 Memory/Context provider 方式注入;随后再考虑可拾取怀表物体、对话历史/记忆和 20 场景评测 harness。
- **[用户手动,A 档]** 完成 Agent context 配置化第一步:在 `gateway/agent_context/` 下新增 `personas.json`、`world.md`、`quest_watch.json`,把 NPC Persona、世界背景、怀表任务状态说明与合法状态转移从 `npc_agent_gateway.py` 的硬编码 prompt 中拆出为可维护文件。
- **[结对,A 档]** 改造 `npc_agent_gateway.py` 从 context 文件构造 prompt:新增 `load_json(...)`、`format_persona(...)`、`format_allowed_transitions(...)`,让真实 Agent 的回答依据来自角色设定、世界背景、当前 `quest_state`、玩家输入和 allowed transitions。排错记录:修复 `format_persona.get(...)` 把函数当字典、`PERSONAS.gat(...)` 拼写错误、`WATCH_QUUEST` typo、`WATCH_QUEST["states"]` 读取错误、`update_quest_sate` 工具名拼写错误等配置化迁移 bug。
- **排错/设计记录**:在卫兵对话场景发现 tool result 泄漏到玩家 UI:Agent 调用 `update_quest_state` 后,框架把工具结果回填给 LLM,模型在最终台词里说出"任务状态已更新为 got_river_clue"。修复方式:强化 system prompt 输出规则,禁止暴露工具/函数/状态机/quest_state/next_state/任务状态更新;同时把 `update_quest_state` 工具返回值改为简短内部结果,降低模型复述工具结果的概率。
- **剧情质量修复**:强化卫兵 `accepted_watch_quest` 场景的关键剧情约束,要求台词必须自然包含"河边"或"芦苇荡"线索,避免只更新状态但没有把线索明确传达给玩家。验收结果:真实 Agent 仍能触发 `agent_tool_call=update_quest_state, next_state=got_river_clue`,Unity UI 不再显示工具结果,卫兵能给出河边线索。
- **下一步**:整理 `npc_agent_gateway.py` adapter 结构,把 schema、context loading、agent runtime、fallback、reply cleaning/tool capture 分层;之后再进入 `inspect_world_state` 工具和可拾取怀表物体。
- **[用户手动,A档]** 完成可拾取怀表实体闭环: 新增 WatchItem.cs, 场景中放置 LostWatchItem, 仅在 got_river_clue 状态下允许玩家靠近按 E 拾取, 拾取后调用 QuestStateManager.SetState("watch_found") 并隐藏怀表物体。修复场景: 怀表拾取最初没有更新状态, 原因是 FindGameObjectWithTag("player") 大小写不匹配 Unity 默认 Player Tag, 导致脚本找不到玩家对象。
- **[结对,A档]** 修复“未拾取怀表也能完成任务”的剧情漏洞: PlayerInteractor.BuildPlayerMessage(...) 不再在 got_river_clue + elder 时告诉 Agent “已经找到怀表”, 而是改为“问到河边线索但还没找到”; 只有 Unity 状态真正进入 watch_found 后, 才向长老发送“找到了怀表”的玩家输入。该修复明确了游戏事实由 Unity 状态机控制, Agent 只能基于当前事实生成对话和建议工具调用。
- **验收通过**: 测试流程为接长老任务 -> 找卫兵获得河边线索 -> 不捡怀表回长老不会完成任务 -> 河边按 E 拾取怀表后状态变为 watch_found -> 回长老进入完成对话。当前短任务从纯 NPC 对话推进为“对话线索 + 场景物品 + 状态门控”的可玩闭环。
- **下一步**: 做最小任务反馈 polish: 增加 QuestDebugUI 或临时状态显示/拾取提示, 让玩家明确知道当前任务状态和靠近怀表时可以按 E 拾取; 随后再进入 inspect_world_state 工具, 让 Agent 主动读取 Unity 事实而不是只依赖传入的 quest_state。