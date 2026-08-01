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
