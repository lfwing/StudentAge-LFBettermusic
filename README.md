# 一、简介
LFBettermusic，一款适用于游戏《StudentAge》，基于 BepInEx、Harmony 和 Unity 的游戏音乐演出插件。

插件面向Modder和玩家，采取类似原版的Data-Driven的形式，自定义 `1163` EFFECT，为剧情提供独立于原版 BGM 的音乐播放渠道，并支持单 Talk 音乐、跨 Talk 背景音乐、唱歌演出、浮动歌词、双语歌词、指定句段播放、暂停恢复和长按跳过等功能。

插件音乐和原版音乐使用两个不同的播放渠道：
- 插件音乐播放时拥有更高优先级；
- 插件音乐加载期间不会提前压制原版音乐；
- 插件音乐暂停、停止、中断、播放失败或自然结束后，会归还原版音乐渠道；
- 插件音乐不会永久占用或破坏原版 BGM 的正常播放。

BetterMusic 同时兼容正常游戏剧情和剧情编辑器 Preview。

*<font color="#4bacc6">此模组实际代码编写工作由AI完成，本人仅负责构思及debug</font>*

# 二、功能特性
- ✅ **独立音乐渠道**：插件音乐与原版 BGM 分离，避免直接占用原版播放通道
- ✅ **两种适用场景和两种使用模式自由组合**：针对单talk/背景的背景型音乐/唱歌型音乐，自由组合，且支持多项参数的自定义，不同组合对自动模式、手动推进和快进有着不同的适配
- ✅ **扩展 LRC**：扩展.lrc歌词文件，支持 `idN` 演唱槽、双语歌词和歌词句段编号
- ✅ **句段播放**：通过 `u/v` 从指定歌词开始，或播放到指定歌词结束
- ✅ **暂停与恢复**：暂停时记录位置并清理歌词，恢复时从原位置继续同
- ✅ **音频预加载**：进入 Talk 时提前加载播放型 1163 对应音频，不提前暂停原版 BGM
- ✅ **平滑音乐接续**：合并同路径加载请求，缓存命中时同帧换轨，并减少相邻 Talk 之间的空档
- ✅ **支持双语歌词**：原文和译文两行显示，并保持悬挂式左对齐
- ✅ **歌词交互框**：点击歌词显示半透明灰框、二级菜单和恢复按钮，无操作后自动隐藏。可拖动歌词，并自动限制在屏幕可见区域内，且玩家可在五档可见字号和十三种颜色中实时选择字号颜色
- ✅ **创意工坊友好**：可直接读取其他 Mod 的按规范使用和导入的音乐、歌词和 JSON
- ✅ **支持原版音乐扩展歌词**：无需替换原版音频，也可为原版音乐配置.lrc歌词文件
- ✅ **正常游戏与编辑器均适用**：正常游戏和编辑器中均可以使用，便于检验修改
- ✅ **内置校验音乐**：保留音乐 ID `1163001`，显示名称固定为“校验音乐”

# 三、安装方法
## 3.1 前置需求
- 《学生时代》游戏本体
- 与游戏当前 Mod 环境匹配的 BepInEx 5.x
- （推荐）启用 BepInEx 控制台或日志文件，便于查看启动自检结果
## 3.2 安装插件
1. 安装并确认 BepInEx 可以正常加载。
2. 获取编译后的 `LFBetterMusicPlugin.dll`。
3. 将 DLL 放入：
```text
<游戏目录>/BepInEx/plugins/
```
4. 启动游戏。
5. （推荐）检查 BepInEx 控制台或日志。

正常启动时应看到类似内容：
```text
[启动自检成功] BetterMusic x.x.x 全流程自检通过；音乐条目=...，资源包=...。
```

若出现：
```text
[启动自检失败] BetterMusic x.x.x 全流程自检未通过：...
```
请根据后面的具体错误检查引用、资源包、关键补丁或运行环境。

## 3.3 Mod 作者模板
插件启动后会在以下位置维护一份作者模板：
```text
<游戏目录>/BepInEx/plugins/Bettermusic/
```
该目录仅供复制和参考，**不会被当作正式音乐资源包注册**。

正式发布时，应把整个 `Bettermusic` 文件夹放入自己的 Mod 根目录：
```text
<你的 Mod 根目录>/
└─ Bettermusic/
   ├─ Music/
   ├─ Lyrics/
   └─ Bettermusic.json
```

上传到 Steam 创意工坊后，BetterMusic 会直接扫描并读取对应 Workshop Item，无需要求玩家手动复制音乐和歌词到 BepInEx。

# 四、音乐资源包配置
## 4.1 标准目录
```text
Bettermusic/
├─ Bettermusic.json
├─ Music/
│  ├─ example.mp3
│  └─ ...
└─ Lyrics/
   ├─ example.lrc
   └─ ...
```
文件夹和配置文件名称不区分大小写，但建议统一使用上述写法。

## 4.2 `Bettermusic.json`
示例：
```text
{
  "musics": [
    {
      "id": 90001,
      "name": "自定义音乐示例",
      "musicPath": "Music/example.mp3",
      "lrcPath": "Lyrics/example.lrc",
      "volume": 0.85
    },
    {
      "id": 12345,
      "name": "原版音乐外挂歌词示例",
      "musicPath": "",
      "lrcPath": "Lyrics/original_12345.lrc",
      "volume": 1.0
    }
  ]
}
```
字段说明：

| 字段          | 是否必填 | 说明                          |
| ----------- | ---: | --------------------------- |
| `id`        |    是 | 音乐 ID，必须大于 0，并避免与其他 Mod 冲突  |
| `name`      |   建议 | 控制台成功日志中显示的音乐名称             |
| `musicPath` |  视情况 | 自定义音乐相对路径；留空时尝试使用同 ID 的原版音乐 |
| `lrcPath`   |    否 | LRC 相对路径；可为自定义音乐或原版音乐外挂歌词   |
| `volume`    |    否 | 播放音量，示例使用 `0.85` 或 `1.0`    |

相对路径以当前 Mod 自己的 `Bettermusic` 文件夹为基准。

## 4.3 音乐 ID 规则
音乐解析优先级为：
1. 内置保留音乐 `1163001`
2. 创意工坊 BetterMusic JSON 中的音乐
3. 编辑器 Preview 当前音频配置
4. 正常游戏原版 `AudioCfg`

注意：
- `1163001` 是代码级保留 ID，外部 JSON 不能覆盖；
- 不同 Mod 使用相同 ID 时会发生冲突；
- 冲突时保留先注册的条目，并在启动自检失败信息中报告；
- 单个资源包 JSON 损坏时只忽略该包，不应拖垮整个插件；
- 自定义音乐支持格式取决于游戏当前 Unity 音频加载能力。

# 五、1163 EFFECT 总览

| 指令                                    | 功能               |
| ------------------------------------- | ---------------- |
| `1163,0`                              | 停止并刷新插件音乐通道      |
| `1163,1,x,y,z,m[,u[,v]]`              | 针对单一 Talk 的背景型音乐 |
| `1163,2,x,y,z,id1,id2...,-1[,u[,v]]`  | 针对单一 Talk 的唱歌型音乐 |
| `1163,10,x,y,z,m[,u[,v]]`             | 针对背景的背景型音乐       |
| `1163,20,x,y,z,id1,id2...,-1[,u[,v]]` | 针对背景的唱歌型音乐       |
| `1163,99,1`                           | 暂停当前插件音乐         |
| `1163,99,2`                           | 从暂停点恢复插件音乐       |

## 5.1 效果的执行时机
为减少文本速度对音乐开始时间的影响，只把当前 Talk 中的 1163 效果单独提前到文字开始前，不影响原版效果。
```text
PlayScreenEffect
→ RefreshTalkingRole
→ 原版 PlayAudio
→ 原版 PlayRoleEffect
→ 提前执行当前 Talk 中的 1163
→ 原版 DoText
→ 原版 DoTextEnd
→ 执行剩余原版 EFFECT
```

## 5.2 新播放指令的会话替换
再次执行 `1163,1 / 2 / 10 / 20` 时：
- 新指令通过参数、音乐资源和 LRC 校验后，才替换旧会话；
- 采用新指令的字号、颜色、演唱者、`u/v` 和播放类型；
- 清除上一会话的运行时字号、颜色和拖动位置；
- 按新 EFFECT 的初始设置和默认位置正常重显；
- 无效的新播放指令不会提前破坏仍有效的旧背景会话。

# 六、停止、暂停与恢复
## 6.1 停止并刷新
```text
1163,0
```

作用：
- 停止当前插件音乐；
- 清理浮动歌词；
- 清理长按跳过提示；
- 释放插件音乐会话；
- 归还原版音乐播放渠道；
- 即使当前没有插件音乐，也可以使用，相当于刷新一次插件播放渠道。

## 6.2 暂停
```text
1163,99,1
```
只有当前存在正在播放的插件音乐时才有效。
暂停后：
- 记录当前播放位置；
- 暂停插件独立 AudioSource；
- 清理当前歌词；
- 归还原版 BGM 渠道。

## 6.3 恢复
```text
1163,99,2
```
只有当前存在已暂停的插件音乐时才有效。
恢复后：
- 从记录的暂停点继续播放；
- 重新取得插件音乐优先级；
- 根据当前播放时间重新同步歌词。

# 七、背景型音乐
## 7.1 单 Talk 背景型音乐
格式：
```text
1163,1,x,y,z,m[,u[,v]]
```
参数：

| 参数 | 说明 |
|---|---|
| `x` | 音乐 ID |
| `y` | 播放类型，只允许 `1`、`2`、`3` |
| `z` | 歌词字号：`-1`、`1`、`2`、`3`、`4` |
| `m` | 歌词颜色：`0～12` |
| `u` | 可选，起始歌词句号 |
| `v` | 可选，结束歌词句号 |

`y` 的含义：

| `y` | 行为 |
|---:|---|
| `1` | 音乐播放一次；400ms 后允许玩家手动进入下一 Talk；音乐完成前自动模式不推进；快进不生效 |
| `2` | 音乐循环；玩家手动推进后停止；自动模式不推进；快进不生效 |
| `3` | 音乐必须完整播放后才能正常推进；可长按鼠标左键跳过；自动和快进均不生效 |

## 7.2 背景持续型背景音乐
格式：
```text
1163,10,x,y,z,m[,u[,v]]
```
规则：
- `y` 无论填写什么，均强制按 `2` 处理；
- 音乐循环播放；
- 进入下一 Talk 后仍保持；
- 不阻止自动模式；
- 不阻止快进；
- **只有执行 `1163,0`、`1163,99,x` 或播放新插件音乐时才改变当前状态**。

# 八、唱歌型音乐
## 8.1 单 Talk 唱歌型音乐
格式：
```text
1163,2,x,y,z,id1,id2,id3...,-1[,u[,v]]
```
规则：
- `x` 为音乐 ID；
- `y` 强制为 `3`；
- `z` 只允许 `1～4`，非法值按 `1` 处理；
- `id1、id2...` 是演唱角色的实际 roleid；
- `-1` 表示演唱角色列表结束；
- 音乐必须播放完成，或由玩家长按鼠标左键跳过；
- 自动模式和快进均不生效；
- 只对当前 Talk 生效。

## 8.2 背景持续型唱歌音乐

格式：
```text
1163,20,x,y,z,id1,id2,id3...,-1[,u[,v]]
```
规则：
- `y` 强制为 `1`；
- 音乐单次播放；
- 进入下一 Talk 后仍保持；
- 音乐完成前阻止自动推进；
- 玩家手动推进仍然有效；
- 快进仍然有效。
## 8.3 演唱角色槽
LRC 中的 `id1`、`id2` 并不是直接 roleid，而是 EFFECT 角色列表的位置。
例如：
```text
1163,2,90001,3,1,103,0,-1
```
表示：
- `id1` 对应 roleid `103`
- `id2` 对应 roleid `0`
- roleid `0` 代表玩家
如果 LRC 出现 `id3`，但 EFFECT 只提供了两个角色，则该行按“合唱”处理。
如果歌词行没有任何 `idN` 前缀，也按“合唱”处理。

# 九、歌词字号与颜色
## 9.1 字号

| `z` | 显示效果 |
|---:|---|
| `-1` | 不显示歌词，仅背景型音乐可用 |
| `1` | 默认字号 |
| `2` | 1.2 倍字号 |
| `3` | 1.5 倍字号 |
| `4` | 1.8 倍字号 |

背景型音乐中，非法字号会归一化为 `-1`。
唱歌型音乐中，非法字号会归一化为 `1`。

## 9.2 背景型歌词颜色

|  `m` | 颜色  |
| ---: | --- |
|  `0` | 白色  |
|  `1` | 红色  |
|  `2` | 橙红色 |
|  `3` | 橙色  |
|  `4` | 橙黄色 |
|  `5` | 黄色  |
|  `6` | 黄绿色 |
|  `7` | 绿色  |
|  `8` | 蓝绿色 |
|  `9` | 蓝色  |
| `10` | 蓝紫色 |
| `11` | 紫色  |
| `12` | 紫红色 |

此颜色序列为十二色相环的排列。非法颜色值按 `0` 处理。

## 9.3 唱歌型歌词颜色
唱歌模式不使用 `m` 参数。

| 演唱者 | 歌词颜色 |
|---|---|
| 男性 | 天蓝色 |
| 女性 | 粉色 |
| 合唱、未匹配或无 `idN` | 紫色 |

roleid `0` 会读取玩家当前姓名和性别。无法读取玩家姓名时，使用“白雨”作为后备名称。

# 十、`u/v` 句段播放
`u/v` 按 LRC 中有效时间歌词的顺序计算，无时间标签的元数据不计入句数。
## 10.1 只填写 `u`
```text
1163,1,90001,1,1,0,2
```
表示从第 2 条有效歌词的时间点开始，一直播放到结束。
## 10.2 同时填写 `u/v`
```text
1163,1,90001,1,1,0,2,5
```
表示从第 2 句开始，播放到第 5 句结束。
规则：
- `u=0`：从音频开头播放；
- `u>=1`：从第 `u` 条有效歌词的时间点开始；
- `v` 是包含式终点；
- 停止点取第 `v` 句之后下一条时间更晚的有效歌词；
- 循环模式只在所选句段内循环；
- `v` 必须大于等于 `u`；
- `v=0` 无效；
- `u` 或 `v` 超出有效歌词范围时，新指令拒绝执行；
- 新指令校验失败时，当前正在播放的有效音乐不会被提前中断。

唱歌型音乐的 `u/v` 位于角色列表终止符 `-1` 之后：
```text
1163,2,90001,3,1,103,0,-1,2,5
```

# 十一、扩展 LRC 格式
## 11.1 标准歌词举例
```text
[00:31.65]第一句歌词
[00:35.47]第二句歌词
```
## 11.2 演唱角色槽扩展
```text
id1[00:31.65]第一位角色演唱
id2[00:35.47]第二位角色演唱
```
`idN` 对应 EFFECT 演唱角色列表中的第 N 个位置。
## 11.3 双语歌词扩展
使用第一个半角竖线 `|` 分隔原文和译文：
```text
id1[00:31.65]ふわりふわり揺れる二人の距離が|摇摇晃晃的两人之间的距离
```

游戏内显示为：
```text
角色名：ふわりふわり揺れる二人の距離が
        摇摇晃晃的两人之间的距离
```
译文会悬挂在“角色名：”之后，并与歌词正文左对齐。
## 11.4 元数据
以下这类没有 LRC 时间标签的 JSON/NDJSON 行会被自动跳过，不计入 `u/v` 句数：
```text
{"t":0,"c":[{"tx":"作词: "},{"tx":"示例作者"}]}
```
## 11.5 全局时间偏移
支持标准 LRC 偏移标签：
```text
[offset:500]
```
单位为毫秒。

# 十二、浮动歌词显示
BetterMusic 使用 TextMeshPro 浮动歌词层。
## 12.1 基础显示效果
- 显示在 Talk 顶部区域；
- 角色名前缀与歌词正文分离；
- 支持单语和双语；
- 歌词淡入并轻微向上浮动；
- 使用更细的 TextMeshPro 描边；
- 使用随字号倍率同步变化的柔和半透明黑色阴影；
- 自动根据局部背景亮度切换黑色或白色描边。
## 12.2 拖动与边缘限制
- 点击并拖动歌词可改变其屏幕位置；
- 边缘限制会同时考虑歌词、半透明灰框、顶部工具栏和展开菜单的实际尺寸；
- 字号变大或菜单展开后会重新校正位置，避免歌词或按钮跑出屏幕；
- 普通歌词换行、淡入动画和背景会话跨 Talk 重建时，不会把玩家拖好的位置强制重置；
- 新的播放型 1163 会创建新会话，并回到默认位置。
## 12.3 灰框、图标与菜单
- 点击歌词后显示半透明灰色选框；
- 选框中间正上方显示齿轮按钮和恢复按钮；
- 齿轮按钮展开“字号”和“颜色”二级菜单，再进入对应三级菜单；
- 齿轮和恢复图标由代码运行时生成透明纹理并转换为 Sprite，通过 Unity `Image` 显示，不依赖 TMP 字体是否包含 Unicode 图标字形；
- 恢复按钮恢复当前 EFFECT 的初始字号与颜色规则，保留拖动位置。
## 12.4 自动隐藏
灰框显示后，以 `Time.unscaledTime` 计算约 `4.5` 秒无操作倒计时。
以下操作会重新开始倒计时：
- 点击歌词；
- 点击齿轮或恢复按钮；
- 展开二级菜单或三级菜单；
- 选择字号或颜色；
- 拖动歌词或结束拖动。
规则：
- 鼠标只停留在菜单上但没有实际操作，不会无限延长显示时间；
- 拖动过程中不会自动隐藏；
- 倒计时结束后，灰框、工具栏、二级菜单和三级菜单一起隐藏；
- 灰框和菜单隐藏不影响歌词本身继续显示。

# 十三、控制台日志
插件只保留以下四类底层日志出口：
```text
[启动自检成功]
[启动自检失败]
[1163执行成功]
[1163错误]
```
## 13.1 启动自检
自检覆盖：
- 正常游戏单条和批次 `GenEffector`
- 编辑器普通文本 Talk
- 编辑器空文本 Talk
- 内置校验音乐 `1163001`
- Mod 作者模板
- 创意工坊资源包扫描
- 播放控制器
- 浮动歌词自动描边组件
- Harmony 外围补丁
- 关键 EFFECT 入口复检
## 13.2 成功日志
背景型音乐示例：
```text
[1163执行成功] 类型=针对单一talk的背景型音乐；音乐=校验音乐（ID=1163001）；字号=2（1.2倍）；歌词颜色=9（蓝色）；u=2，v=5。
```

唱歌型音乐示例：
```text
[1163执行成功] 类型=针对单一talk的唱歌型音乐；音乐=示例歌曲（ID=90001）；字号=1（默认）；唱歌人=id1=薛诗蕾（roleId=103，女性），id2=白雨（roleId=0，男性）；u=2，播放至结束。
```
## 13.3 错误日志
参数错误、资源缺失、LRC 错误、句段越界、角色解析问题、音频加载失败、暂停恢复状态无效等统一输出：
```text
[1163错误] 具体错误内容
```
参数解析错误会附带原始 1163 指令。

---
# 十四、技术细节
## 14.1 核心实现
BetterMusic 使用 BepInEx 和 Harmony 对游戏进行运行时扩展。

| 模块        | 核心文件                                       | 作用                        |
| --------- | ------------------------------------------ | ------------------------- |
| 插件入口      | `Plugin.cs`                                | 初始化、自检、补丁注册、生命周期保护和关键入口保活 |
| EFFECT 入口 | `Patches/GenEffector1163Patch.cs`          | 将正常游戏中的 1163 转交给插件        |
| 编辑器桥接     | `Preview/BetterMusicPreviewBridge.cs`      | 让剧情编辑器 Preview 执行 1163    |
| 指令解析      | `Effects/BetterMusicEffectEncoding.cs`     | 解析和校验 1163 参数             |
| EFFECT 适配 | `Effects/EffectorBetterMusic.cs`           | 接入原版 Effector 链           |
| 播放状态机     | `Runtime/BetterMusicController.cs`         | 音频、歌词、Talk 推进、暂停恢复和双通道控制  |
| 会话模型      | `Runtime/BetterMusicSession.cs`            | 保存当前播放请求和运行状态             |
| 音乐解析      | `Audio/MusicResolver.cs`                   | 解析内置、自定义和原版音乐             |
| 资源包发现     | `Discovery/BetterMusicPackageDiscovery.cs` | 扫描 Steam 创意工坊资源包          |
| JSON 合并   | `Config/BetterMusicConfigStore.cs`         | 合并音乐配置并处理 ID 冲突           |
| LRC 解析    | `Lyrics/LrcParser.cs`                      | 解析时间、演唱槽、双语和句号            |
| 浮动歌词      | `Lyrics/FloatingLyricsOverlay.cs`          | 创建和更新歌词 UI                |
| 自动描边      | `Lyrics/LyricsContrastProbe.cs`            | 根据局部背景亮度调整描边              |
| 角色映射      | `Runtime/SingingRoleInfo.cs`               | 解析角色姓名、roleid 和性别         |
| Talk 控制   | `Patches/TalkAdvancePatches.cs`            | 控制手动、自动和快进                |
| 生命周期      | `Patches/TalkLifecyclePatches.cs`          | 处理 Talk 切换、关闭和空文本         |
| 长按跳过      | `Patches/HoldToSkipPatches.cs`             | 监听长按跳过行为                  |
| 跳过 UI     | `UI/HoldToSkipOverlay.cs`                  | 显示长按跳过提示和进度               |
| 内置校验      | `Assets/BuiltInValidationAssets.cs`        | 注册并生成 `1163001` 校验资源      |

## 14.2 双音频通道
- 插件音乐：独立 `lf-BetterMusicAudioSource`
- 原版音乐：原版 `AudioMgr channel=1`
只有插件音乐实际开始播放时，才暂停原版 BGM。
插件音乐在以下情况归还原版渠道：
- `1163,0`
- `1163,99,1`
- 单 Talk 被手动或系统推进
- 长按跳过
- Talk 关闭或失效
- 单次音乐自然结束
- 资源加载或播放失败
- AudioSource 失效
- 插件退出
## 14.3 EFFECT 复核
游戏启动过程中可能销毁 BepInEx 所挂载的插件组件。BetterMusic 不会在这种非退出型销毁中撤销关键补丁。
持久控制器会维护：
- 正常游戏 `CommonEvtMgr.GenEffector`
- 编辑器 `PreviewTalkView.DoTextEnd`
- 编辑器空文本 `PreviewTalkView.RefreshTalk`
避免启动后出现 `Effect Not Found`。

---
# 十五、兼容性
## 15.1 正常兼容方向
- 剧情扩展 Mod
- 使用 `1163` EFFECT 的创意工坊 Mod
- 不修改 Talk 推进底层的内容 Mod
- 不占用相同音乐 ID 的 BetterMusic 资源包
- 仅修改其他系统的常规 Harmony Mod
## 15.2 可能发生冲突的情况
- 其他 Mod 同样注册或拦截 EFFECT `1163`
- 其他 Mod 替换 `CommonEvtMgr.GenEffector`
- 其他 Mod 修改 `NewTalkView.DoText`、`PreviewTalkView.DoText` 或 `RefreshTalk`，改变 1163 提前入口顺序
- 其他 Mod 深度修改 `NewTalkView`、`PreviewTalkView`、`OnClickNext`、`NextTalk` 或 Talk 推进流程
- 其他 Mod 修改 `TimerMgr.Delay` 并依赖特定委托对象
- 其他 Mod 直接控制原版 `AudioMgr channel=1`
- 其他 Mod 销毁或替换插件创建的独立 AudioSource
- 其他 Mod 全局修改 `Time.timeScale`，并与单 Talk 快进阻断规则冲突
- 多个 BetterMusic 资源包使用相同音乐 ID
- 游戏版本更新导致目标方法签名变化
## 15.3 存档影响
BetterMusic 不设计新的存档字段，也不主动改写角色、剧情或游戏进度数据。
不过任何非官方运行时 Mod 都存在兼容风险。建议：
- 首次使用前备份存档；
- 更新游戏或插件前备份存档；
- 测试新 EFFECT 时使用独立测试存档。

# 十六、构建方法
## 16.1 环境需求
- Visual Studio 或支持 SDK 风格项目的 MSBuild
- .NET Framework 4.8 开发包
- 游戏本体及其 Managed DLL
- BepInEx 和 Harmony 相关 DLL
项目目标框架：
```text
net48
```
程序集名称：
```text
LFBetterMusicPlugin
```
## 16.2 引用文件
项目默认从以下目录寻找引用：
```text
<项目目录>/bin/Debug/net48/
```
也可以通过 MSBuild 属性覆盖：
```text
/p:ReferenceDir=<引用 DLL 所在目录>
```

主要引用包括：
- `0Harmony.dll
- `Assembly-CSharp.dll`
- `BepInEx.dll`
- `BepInEx.Harmony.dll`
- `Newtonsoft.Json.dll`
- UnityEngine 相关 DLL
- TextMeshPro DLL

## 16.3 使用游戏目录构建
项目支持通过环境变量指定游戏目录：
```text
STUDENTAGE_GAME_DIR
```
也可在构建时传入：
```bash
dotnet build BetterMusicPlugin.csproj -c Release /p:GameDir="你的游戏目录"
```
或者准备引用目录后：
```bash
dotnet build BetterMusicPlugin.csproj -c Release /p:ReferenceDir="你的引用目录"
```
构建输出通常位于：
```text
bin/Release/net48/LFBetterMusicPlugin.dll
```
项目配置还可在构建后把 DLL 复制到：
```text
<游戏目录>/BepInEx/plugins/
```
## 16.4 可选内置资源
授权构建者可以放入：
```text
BundledAssets/ValidationMusic.mp3
BundledAssets/ValidationLyrics.lrc
```
项目会将其嵌入 DLL
若未提供，插件会在运行时生成后备校验音和占位歌词，`1163001` 仍然可以注册。

# 十七、项目结构
```text
BetterMusicPlugin/
├─ Plugin.cs                              # BepInEx 入口、自检和补丁复核
├─ BetterMusicPlugin.csproj              # net48 项目与引用配置
├─ BetterMusicPlugin.slnx                # Visual Studio 解决方案
│
├─ Assets/
│  └─ BuiltInValidationAssets.cs         # 内置校验音乐 1163001
│
├─ Audio/
│  ├─ MusicResolver.cs                   # 音乐 ID 与资源解析
│  └─ ResolvedMusic.cs                   # 已解析音乐模型
│
├─ Config/
│  ├─ BetterMusicConfig.cs               # Bettermusic.json 数据模型
│  └─ BetterMusicConfigStore.cs          # 多资源包合并与 ID 查询
│
├─ Discovery/
│  ├─ BetterMusicPackage.cs              # 资源包描述模型
│  └─ BetterMusicPackageDiscovery.cs     # Steam 创意工坊资源发现
│
├─ Effects/
│  ├─ BetterMusicEffectEncoding.cs       # 1163 参数解析与校验
│  ├─ EffectorBetterMusic.cs             # 原版 Effector 链适配
│  └─ Early1163Execution.cs              # 1163 预加载、文字开始前执行与一次性对象标记
│
├─ Lyrics/
│  ├─ FloatingLyricsOverlay.cs           # 歌词显示、拖动、灰框、图标和多级菜单
│  ├─ FloatingLyricsRuntimeState.cs      # 会话内字号、颜色与位置状态
│  ├─ LrcLine.cs                         # 单条歌词模型
│  ├─ LrcParser.cs                       # 扩展 LRC 解析
│  └─ LyricsContrastProbe.cs             # 背景亮度与自动描边
│
├─ Patches/
│  ├─ GenEffector1163Patch.cs            # 正常游戏 1163 工厂入口与提前对象消费
│  ├─ TalkAdvancePatches.cs              # 条件式推进来源识别和最终阻断
│  ├─ TalkLifecyclePatches.cs            # 预加载、Talk 切换、音乐交接和关闭
│  ├─ HoldToSkipPatches.cs               # 长按跳过输入
│  └─ PreviewEffectTriggerPatches.cs     # Preview 文字开始与空文本触发入口
│
├─ Preview/
│  └─ PreviewTalkAccess.cs               # 编辑器 Talk、Audio 和 Person 数据访问
│
├─ Runtime/
│  ├─ BetterMusicController.cs           # 播放状态机、缓存、换轨、歌词和推进控制
│  ├─ BetterMusicSession.cs              # 当前播放会话
│  ├─ MusicResolveContext.cs             # 音乐解析上下文
│  ├─ RuntimeTalkAccess.cs               # 正常游戏 Talk 与 lastEffectCfgId 访问
│  ├─ TalkChannel.cs                     # Talk 渠道枚举
│  └─ SingingRoleInfo.cs                 # 演唱者姓名与性别解析
│
├─ Templates/
│  └─ ModAuthorTemplateInstaller.cs      # 作者模板自动安装
│
├─ UI/
│  └─ HoldToSkipOverlay.cs               # 长按跳过提示 UI
│
├─ ModAuthorTemplate/                    # 发布给 Mod 作者的模板
│  ├─ README.txt
│  └─ Bettermusic/
│     ├─ Bettermusic.json
│     ├─ Music/
│     └─ Lyrics/
│
└─ BundledAssets/                        # 可选的编译期嵌入资源
```
# 十八、常见问题
## Q：控制台出现 `Effect Not Found` 怎么办？
A：正常情况下插件的复核机制会防止该问题。请先查看启动日志是否为 `[启动自检成功]`。若自检失败，应根据错误检查游戏版本、Harmony 补丁目标和插件引用。
## Q：为什么插件音乐停止后原版 BGM 没有恢复？
A：插件处理双通道让权。请确认使用的是新版本，并查看是否出现“原版音乐通道恢复失败”的 `[1163错误]`。也要检查是否有其他 Mod 同时控制原版 BGM。
## Q：为什么填写了 `u/v` 后指令没有执行？
A：请检查：
- ID是否正确，以及LRC是否存在有效时间标签；
- `u` 是否为非负数；
- `v` 是否大于等于 `u`；
- `v` 是否大于 0；
- 句数是否超出有效歌词数量。
## Q：为什么音乐接续有明显的卡顿？
A：这是游戏原版设计就存在的问题，因为游戏原版设计在自动模式推进talk时天然存在间隔，若想尽可能减少卡顿影响，个人建议将文本显示速度和自动播放间隔都调为“快”，至少要是“正常”，不建议设置为“慢”。
## Q：为什么使用了阻碍自动推进的effect，但是自动模式依然会很快速的推进？
A：这是游戏原版设计就存在的问题，只要频繁改动文本显示速度和自动播放间隔，就会导致原版游戏残留大量未清理数据，引起逻辑错误，建议重启游戏。
## Q：为什么唱歌角色显示成“合唱”？
A：可能原因包括：
- LRC 没有 `idN`；
- `idN` 超过 EFFECT 中提供的角色数量；
- roleid 无法解析；
- 对应角色不存在。
这些情况按设计回退为“合唱”。
## Q：为什么背景型音乐没有歌词？
A：检查：
- `z` 是否为 `-1`；
- JSON 是否配置 `lrcPath`；
- LRC 文件是否存在；
- LRC 是否包含有效时间标签；
- `u/v` 是否越界。
## Q：能否只给原版音乐增加歌词？
A：可以。在 JSON 中使用原版音乐 ID，把 `musicPath` 留空，只填写 `lrcPath`。
## Q：编辑器 Preview 中能否测试？
A：可以。插件为普通文本 Talk 和空文本 Talk 分别提供了 Preview 桥接。但编辑器版本或底层方法签名变化仍可能影响兼容性。
## Q：插件会修改存档吗？
A：项目没有设计新的存档字段，也不主动改写游戏进度。出于非官方 Mod 的通用风险，仍建议保留备份。

# 十九、进行二次修改时优先定位的文件

| 需要修改的功能 | 优先查看文件 |
|---|---|
| 新增或改变 `1163` 参数格式 | `Effects/BetterMusicEffectEncoding.cs` |
| 改变播放、暂停、循环、Talk 拦截规则 | `Runtime/BetterMusicController.cs` |
| 给当前播放增加状态字段 | `Runtime/BetterMusicSession.cs` |
| 修改音乐 ID 查找优先级 | `Audio/MusicResolver.cs` |
| 修改 JSON 字段 | `Config/BetterMusicConfig.cs`、`Config/BetterMusicConfigStore.cs` |
| 修改创意工坊扫描方式 | `Discovery/BetterMusicPackageDiscovery.cs` |
| 修改 LRC 语法 | `Lyrics/LrcParser.cs`、`Lyrics/LrcLine.cs` |
| 修改歌词样式和颜色 | `Lyrics/FloatingLyricsOverlay.cs` |
| 修改唱歌人姓名/性别逻辑 | `Runtime/SingingRoleInfo.cs` |
| 修改长按跳过样式 | `UI/HoldToSkipOverlay.cs` |
| 修改正常游戏 EFFECT 接入 | `Patches/GenEffector1163Patch.cs` |
| 修改编辑器 EFFECT 接入 | `Preview/BetterMusicPreviewBridge.cs`、`Patches/PreviewEffectTriggerPatches.cs` |
| 修复启动时 EFFECT 丢失 | `Plugin.cs` |
| 修改编译引用或输出目录 | `BetterMusicPlugin.csproj` |
| 修改作者示例 | `ModAuthorTemplate/`、`Templates/ModAuthorTemplateInstaller.cs` |

# 二十、维护原则
1. **不要删除 `Plugin.cs` 中的关键 EFFECT 保活和非退出销毁保护。** 这是避免启动阶段出现 `Effect Not Found` 的核心机制。
2. **不要把插件 AudioSource 和原版 BGM AudioSource 合并。** 双渠道是原版音乐在插件中断后能够正常恢复的基础。
3. **指令格式只在 `BetterMusicEffectEncoding` 解析。** 不应在多个 Patch 中分别读取参数。
4. **播放状态统一进入 `BetterMusicSession` 和 `BetterMusicController`。** 避免另建相互冲突的临时状态变量。
5. **正常游戏和 Preview 是两个 EFFECT 入口，但共用同一控制器。** 修改时必须同时回归两种环境。
6. **单 Talk 与背景音乐的生命周期必须保持分离。** 单 Talk 跟随 Talk 结束，背景会话跨 Talk 保留。
7. **作者模板不是运行时资源包。** `BepInEx/plugins/Bettermusic` 仅用于示例，正式内容从各 Mod 的创意工坊目录读取。

# 二十一、更新日志
## v1.6.2
对浮动歌词相关内容进行大修改，且微调了音乐接续逻辑，尽可能使其不被原版影响。
## v1.5.1
公开的第一个stable版本，涵盖基础功能

---
# 二十二、贡献与反馈
提交问题时，建议同时提供：
- BetterMusic 版本
- 游戏版本
- BepInEx 版本
- 完整的 1163 指令
- 对应 `Bettermusic.json`
- 对应 LRC 文件
- 启动自检结果
- `[1163错误]` 的完整内容
- 是否在正常游戏或编辑器 Preview 中触发
- 是否安装了其他修改 Talk、EFFECT 或音乐系统的 Mod
若项目发布到 GitHub、Gitee 或其他代码托管平台，可在此补充 Issue 和 Pull Request 地址。

作者QQ邮箱2124436512@qq.com，如有进一步探讨指导的地方，欢迎联系。

# 二十三、许可证与免责声明
参考：
https://github.com/white12666/StudentAgeEditorPlus
在此提出感谢。

本 mod 基于 AGPL-3.0 协议开源，这是一份强 Copyleft（传染性）协议。通俗概括如下：
你可以自由地： 使用、修改本 mod，以及基于本 mod 的代码进行二次开发。
但你必须遵守：
若你复制、修改本 mod 的代码，或将其代码用于你的项目，在分发你的作品时，必须同样以 AGPL-3.0 协议开源，并提供完整源代码；
即使不公开分发文件，若你将修改后的版本部署在服务器上供玩家使用，也必须向这些玩家提供修改后的源代码；
保留本 mod 的版权声明与协议文本。
关于游戏本体： 本 mod 未包含、修改或分发游戏本体的任何代码与文件，仅通过 Harmony 运行时补丁与反射调用同游戏交互。
以上为通俗概括，具体权利义务以 [AGPL-3.0 协议原文] 为准。

---
附加许可（Additional Permission，基于 AGPL-3.0 第 7 条）
作为本项目的创作者，本人在 AGPL-3.0 协议之外，额外授予白雨工作室及其工作人员（仅限用于该工作室的开发与运营工作）一份免费、非独占、不可撤销的许可：
允许其以任何形式（包括但不限于闭源、并入游戏本体、商业用途）复制、修改、引用本项目中由本人创作的代码，不受 AGPL-3.0 各项义务（包括开源与源代码提供义务）的约束。
范围限定：
工作人员以个人名义、非为该工作室工作目的使用本项目代码时，不适用本附加许可，仍受 AGPL-3.0 约束；
依据 AGPL-3.0 第 7 条，任何再分发者可以选择移除本附加许可文本，但这不影响白雨工作室已获得的权利。
当前源码包中未声明明确的开源许可证。正式公开发布前，应由项目作者补充实际采用的许可证文本。

---
免责声明：
1. 本项目是基于 BepInEx 和 Harmony 的非官方同人 Mod，与游戏官方无关。
2. 插件通过运行时补丁扩展游戏行为，游戏更新后可能暂时失效。
3. 音乐、歌词和其他资源的发布者应确保自己拥有合法使用和分发权限。
4. 不建议把无授权的商业音乐直接打包上传至公开创意工坊。
5. 使用第三方 Mod 存在一般性的兼容和稳定性风险，建议提前备份存档及相关文件。

# 二十四、致谢
- BepInEx：Unity 游戏插件框架
- Harmony：运行时方法补丁框架
- TextMeshPro：浮动歌词文字显示
- Newtonsoft.Json：音乐资源包 JSON 读取
- 所有参与测试、编写剧情 EFFECT、制作歌词和反馈问题的 Mod 作者与玩家
