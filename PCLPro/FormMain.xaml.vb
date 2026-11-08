Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Interop
Imports System.Windows.Media.Effects

Public Class FormMain

#Region "基础"

    Private Sub ShowUpdateLog(LastVersion As Integer)
        Dim FeatureCount As Integer = 0, BugCount As Integer = 0
        Dim FeatureList As New List(Of KeyValuePair(Of Integer, String))

        If BuildType = BuildTypes.Release Then
            If LastVersion < 436 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(๑•̀ㅂ•́)و✧ 新增 联机页首次打开时可选择「陶瓦联机」或「MCTier」两种联机方式 页脚可随时切换"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(｡•̀ᴗ-)✧ 新增 MCTier 联机支持 可直接在启动器内创建或加入大厅 游戏端口自动广播到朋友的大厅列表"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "( •̀ ω •́ )✧ 新增 MCTier 按需下载 首次选择时自动下载并校验 约141MB 支持多镜像回退 之后可离线使用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(´･ω･`) 优化 MCTier 内置的 EasyTier 已从源码重建 去掉了对 Npcap 的依赖 免除额外安装驱动"))
            End If
            If LastVersion < 435 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(｡•̀ᴗ-)✧ 修复 从毛玻璃模式切回简洁版时部分界面不立即生效 现在所有卡片与仍然打开的弹窗都会马上恢复平面样式"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(๑•̀ㅂ•́)و✧ 新增 界面文字颜色可自定义 可随手挑颜色 也可以一键跟随主题 深色壁纸配浅色文字更清楚"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(´･ω･`) 新增 界面文字大小可调 范围限制在 10 到 18 之间 不怕一不小心调得太大改不回来"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(｡•̀ᴗ-)✧ 修复 视频壁纸后面那层关不掉的白色蒙版 磨砂强度设为0时卡片与侧栏完全透明"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(๑•̀ㅂ•́)و✧ 修复 视频壁纸下「UI 模糊」调了没反应 现在卡片与侧栏的白霜会跟随 UI 模糊变化 视频画面本身保持清晰不模糊"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(✿◡‿◡) 新增 界面字体可自定义 字体列表显示中文名称 还能导入自己的 ttf otf ttc 字体文件 背景全透明时换一款字重清晰的字体就能看清文字了"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(´･ω･`) 新增 音乐支持更多格式 ogg opus ape m4a aac wma 等 系统解码不了的会自动提示下载 FFmpeg 并转换后播放"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(๑˃̵ᴗ˂̵)و 新增 视频背景支持 mkv webm flv ts 等格式 首次使用会按需下载一次 FFmpeg 转换器 转换结果本地缓存 之后打开直接播放"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "( •̀ ω •́ )✧ 优化 FFmpeg 解码器走镜像源按需下载 只下载一次 约90MB 不用时完全不占空间"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "( •̀ ω •́ )✧ 修复 关闭启动动画后本次运行中修改的设置不会被保存 重新打开动画后也无法记住"))
            End If
            If LastVersion < 434 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(｡•̀ᴗ-)✧ 修复 版本列表图标加载失败 升级后旧程序集名图标路径已自动归一化"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(๑•̀ㅂ•́)و✧ 修复 高分屏250%缩放时启动与关闭动画只推进1到2帧 关闭卡顿约1秒"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "(´･ω･`) 修复 设置自定义背景图片或视频壁纸后启动与点击明显卡顿 背景模糊改为一次性预渲染 视频背景铺满内容区 切换静态壁纸时自动关闭视频 不再出现上下两截画面"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "(๑˃̵ᴗ˂̵)و 新增 毛玻璃界面可调玻璃模糊 圆角与卡片磨砂强度 玻璃只模糊卡片 侧栏与弹窗背后透出的壁纸 与背景模糊互不影响"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "( •̀ ω •́ )✧ 优化 毛玻璃底图重写为一张缓存的模糊位图 去掉折射 色散 镜面高光与鼠标柔光等逐帧重绘装饰 滚动与拖动窗口不再掉帧"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ 新增 可关闭启动器启动时的窗口展开动画"))
            End If
            If LastVersion < 433 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "修复：空闲时每隔几分钟会自动弹出用户账户控制（UAC）提示，已取消后台自动内存优化"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "修复：高分屏（250% 缩放）下启动后只剩任务栏图标、主界面显示不出来"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "调整：更新源迁移到新的 PCL-PE 仓库"))
            End If
            If LastVersion < 432 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "更名：启动器应遵循PCL开源协议更名为 PCL Professional Edition 简称 PCL PE"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "图标：换用新的蓝色六边形图标 窗口 任务栏与关于页头像统一"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "合规：关于页首位署名原作者龙腾猫跃 并加入赞助原作者入口"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "协议：继续沿用 PCL 存储库合理使用指南 不再自称 AGPL-3.0"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "联机：陶瓦联机模块已内嵌进启动器 不用 再额外下载了"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "更新：检查更新时先测速 由你选择最快的下载源"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "整合包：导入整合包自动创建实例 不再需要手动新建文件夹"))
            End If
            If LastVersion < 431 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "修复：版本选择页无法显示版本卡片、导入压缩包时报「无法创建未知类型 MyCard」"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "修复：更新后启动时窗口变暗且无法点击，更新日志弹窗不再卡住界面"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "联机：修正房间码解析方向，加入房间可能已恢复，需要双方都更新到 1.3.2"))
            End If
            If LastVersion < 429 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "修复：更新下载进度窗口无法最小化，现在可最小化到任务栏并在后台继续下载"))
            End If
            If LastVersion < 428 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "调整：内存优化改为实验性功能，执行前会二次确认，并提示可能造成的读盘变慢"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：内存优化会同时清理系统备用内存与文件缓存，释放效果更明显"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "修复：启动器放在中文、空格或含特殊符号的目录时，自动更新会提示「Windows 找不到文件」"))
            End If
            If LastVersion < 427 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：更新流程加入下载进度条，实时显示下载源、速度与已完成大小"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：首次启动时可选择简洁版或毛玻璃版界面，之后可在 设置 → 个性化 中随时切换"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：版本设置 → 内容管理，显示截图、资源包、光影包、投影原理图的数量与大小，并可一键打开文件夹"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：投影原理图预览，可读取 litematic / schematic / schem / nbt 的尺寸、名称与作者"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：更新并自动重启后，启动器窗口可能不显示（仅任务栏存在条目）的问题"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：陶瓦联机创建房间前检查游戏是否已启动"))
            End If
            If LastVersion < 419 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "调整：为保持便捷开箱即用（无需安装），移除了内置动态壁纸，可前往作者群下载"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：视频壁纸选择改用启动器同款界面，并补上打开与关闭动画"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：设置 → 启动 中的存档同步，可自动检测官方版 PCL2 的存档并复制到当前游戏文件夹（同名存档不会被覆盖）"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：设置 → 界面 中的动画削减开关（默认关闭），开启后过渡动画加速、背景模糊减弱，低配设备更流畅"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：视频壁纸播放前自动压缩到 970p / 45 帧并缓存，降低高清壁纸的显卡占用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：卡片与弹窗统一为单层毛玻璃材质，减少图层叠加，滚动与切页更跟手"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：窗口裁剪与控件边缘按整像素对齐，修掉窗口边缘发虚、缝隙与残影"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：游戏支持库与下载源加入真实速度测速，优先走更快的镜像，避免官方源抽风时龟速下载"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：启动加载与页面刷新减少重复重绘，提升流畅度与兼容性"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：陶瓦联机加入房间失败（房间码未补全 U/ 前缀、未复位到 Waiting 状态、单实例残留导致状态卡死）"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：陶瓦内置的公共节点已失效导致客人始终连不上房主，改为显式指定当前可用节点"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：启动器占用 UDP 4445 抢走陶瓦的局域网广播，导致房主房间建好却没有端口、客人必然连接失败"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：视频壁纸选择窗口与启动器风格不一致、关闭时没有过渡动画"))
            End If
            If LastVersion < 415 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：恢复 MC 版本下载、整合包安装与联机入口"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：解锁全部主题配色与自定义主题"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：为设置更新添加部分黑名单"))
            End If
            If LastVersion < 413 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：为遵守相关法律法规，PCL 不再提供 MC 下载功能，非常抱歉 qwq……"))
            End If
            If LastVersion < 411 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：允许修改、添加、移除、更新已安装版本的各个组件，可从版本设置中进入"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：启动游戏时允许按 Esc 取消"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：允许复制主页的启动日志中的内容"))
                FeatureCount += 19
                BugCount += 26
            End If
            If LastVersion < 408 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：版本设置中，添加打开截图文件夹的快捷方式"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：导出整合包时，可以勾选导出光影设置文件"))
                FeatureCount += 23
                BugCount += 28
            End If
            If LastVersion < 406 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：重做 Java 管理与相关设置，允许调整 Java 优先级、指定 Java 版本范围等"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：导出整合包时允许自动导出版本文件夹中的 Java"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：重做弹出提示的样式以及动画，以更符合现代 UI 审美"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法安装 OptiFine 26.1.2"))
                FeatureCount += 35
                BugCount += 17
            End If
            If LastVersion < 404 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：Windows 7 无法正常联网"))
                FeatureCount += 1
                BugCount += 2
            End If
            If LastVersion < 402 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：升级框架到 .NET Framework 4.8"))
                FeatureCount += 35
                BugCount += 21
            End If
            If LastVersion < 398 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：下载游戏、整合包的稳定性"))
                FeatureCount += 1
                BugCount += 5
            End If
            If LastVersion < 391 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：会为部分 Forge / NeoForge 选择不支持的 Java"))
                BugCount += 4
            End If
            If LastVersion < 389 Then
                If LastVersion = 387 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法使用 Java 25+ 启动 Forge"))
                FeatureCount += 4
                BugCount += 3
            End If
            If LastVersion < 387 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：降低 Minecraft 的内存占用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：Minecraft 使用 Java 25+ 时的一个性能问题"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法下载 NeoForge 26.1"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：老版本 Windows 无法启动游戏"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：连接 Mojang 的服务可能失败，提示错误码 421"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法访问爱发电"))
                FeatureCount += 24
                BugCount += 19
            End If
            If LastVersion < 383 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能完全卡住，或是下载进度反复回退"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "新增：内存管理设置，可以选择 G1GC、ZGC 或分代 ZGC"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：来自 Modrinth 的文件下载速度"))
                FeatureCount += 15
                BugCount += 16
            End If
            If LastVersion < 381 Then
                FeatureCount += 3
                BugCount += 13
            End If
            If LastVersion < 379 Then
                If LastVersion >= 376 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：暂时隐藏联机入口……不过只是暂时关闭，它还会回来的！"))
                End If
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：适配新的 Minecraft 版本号系统与 Unobfuscated 版本"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：巨幅优化各个下载页面和 Mod 管理页面的性能"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：更换披风时会显示当前使用的披风"))
                FeatureCount += 33
                BugCount += 29
            End If
            If LastVersion < 376 Then

                FeatureCount += 32
                BugCount += 21
            End If
        Else

            If LastVersion < 416 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：恢复 MC 版本下载、整合包安装与联机入口"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：解锁全部主题配色与自定义主题"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：为设置更新添加部分黑名单"))
            End If
            If LastVersion < 414 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：为遵守相关法律法规，PCL 不再提供 MC 下载功能，非常抱歉 qwq……"))
            End If
            If LastVersion < 412 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：允许修改、添加、移除、更新已安装版本的各个组件，可从版本设置中进入"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：启动游戏时允许按 Esc 取消"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：允许复制主页的启动日志中的内容"))
                FeatureCount += 19
                BugCount += 26
            End If
            If LastVersion < 410 Then
                If LastVersion = 409 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：更新 Mod 时可能更新成另一个 Mod 加载器的版本"))
                FeatureCount += 2
                BugCount += 6
            End If
            If LastVersion < 409 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：版本设置中，添加打开截图文件夹的快捷方式"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：导出整合包时，可以勾选导出光影设置文件"))
                FeatureCount += 21
                BugCount += 22
            End If
            If LastVersion < 407 Then
                If LastVersion = 405 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：使用部分主页预设时崩溃"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法删除部分文件夹"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：版本设置中指定的 Java 无法被保存"))
                    FeatureCount += 1
                    BugCount += 3
                End If
                FeatureCount += 2
            End If
            If LastVersion < 405 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(4, "新增：重做 Java 管理与相关设置，允许调整 Java 优先级、指定 Java 版本范围等"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：导出整合包时允许自动导出版本文件夹中的 Java"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：重做弹出提示的样式以及动画，以更符合现代 UI 审美"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法安装 OptiFine 26.1.2"))
                FeatureCount += 35
                BugCount += 17
            End If
            If LastVersion < 403 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：Windows 7 无法正常联网"))
                FeatureCount += 1
                BugCount += 2
            End If
            If LastVersion < 401 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：可能无法更换披风"))
                FeatureCount += 8
                BugCount += 6
            End If
            If LastVersion < 399 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：升级框架到 .NET Framework 4.8"))
                FeatureCount += 28
                BugCount += 19
            End If
            If LastVersion < 397 Then
                If LastVersion >= 393 AndAlso LastVersion < 397 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法使用自定义隐藏主题"))
            End If
            If LastVersion < 395 Then
                If LastVersion = 393 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分个性化功能失效"))
            End If
            If LastVersion < 393 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：允许永久关闭启动页面的快照版提示"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：下载游戏、整合包的稳定性"))
                FeatureCount += 1
                BugCount += 5
            End If
            If LastVersion < 390 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：会为部分 Forge / NeoForge 选择不支持的 Java"))
                BugCount += 4
            End If
            If LastVersion < 388 Then
                If LastVersion = 386 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法使用 Java 25+ 启动 Forge"))
                FeatureCount += 4
                BugCount += 3
            End If
            If LastVersion < 386 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：Minecraft 使用 Java 25+ 时的一个性能问题"))
                If LastVersion = 384 OrElse LastVersion = 385 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法正常重命名游戏版本"))
                If LastVersion = 385 Then FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法显示 Modrinth 整合包的版本列表"))
                BugCount += 1
            End If
            If LastVersion < 385 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法下载 NeoForge 26.1"))
                If LastVersion = 384 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能失败，提示任务管理刷新线程出错"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：无法保存选择的 Minecraft 文件夹"))
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：使用中文搜索 Mod 时，部分结果会忽略筛选条件"))
                End If
                FeatureCount += 3
                BugCount += 6
            End If
            If LastVersion < 384 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "优化：降低 Minecraft 的内存占用"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：部分关键词搜不到资源，特别是中文 Mod 搜索经常没有结果"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：老版本 Windows 无法启动游戏"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：连接 Mojang 的服务可能失败，提示错误码 421"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：无法访问爱发电"))
                FeatureCount += 21
                BugCount += 14
            End If
            If LastVersion < 382 Then
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "修复：下载可能完全卡住，或是下载进度反复回退"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "新增：内存管理设置，可以选择 G1GC、ZGC 或分代 ZGC"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：来自 Modrinth 的文件下载速度"))
                FeatureCount += 15
                BugCount += 16
            End If
            If LastVersion < 380 Then
                FeatureCount += 3
                BugCount += 13
            End If
            If LastVersion < 378 Then
                If LastVersion >= 377 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "修复：在版本列表中存在 OptiFine 时可能无法加载版本列表"))
                    BugCount += 2
                End If
            End If
            If LastVersion < 377 Then
                If LastVersion >= 373 Then
                    FeatureList.Add(New KeyValuePair(Of Integer, String)(5, "删除：暂时隐藏联机入口……不过只是暂时关闭，它还会回来的！"))
                End If
                FeatureList.Add(New KeyValuePair(Of Integer, String)(3, "新增：适配新的 Minecraft 版本号系统与 Unobfuscated 版本"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(2, "优化：巨幅优化各个下载页面和 Mod 管理页面的性能"))
                FeatureList.Add(New KeyValuePair(Of Integer, String)(1, "优化：更换披风时会显示当前使用的披风"))
                FeatureCount += 33
                BugCount += 29
            End If
            If LastVersion < 375 Then

                FeatureCount += 16
                BugCount += 4
            End If
            If LastVersion < 374 Then

                FeatureCount += 9
                BugCount += 7
            End If
            If LastVersion < 373 Then

                FeatureCount += 7
                BugCount += 10
            End If
        End If

        Dim ContentList As New List(Of String)
        Dim SortedFeatures = FeatureList.OrderByDescending(Function(f) f.Key).ToList
        If Not SortedFeatures.Any() AndAlso FeatureCount = 0 AndAlso BugCount = 0 Then ContentList.Add("没有更新日志……")
        For i = 0 To Math.Min(14, SortedFeatures.Count - 1)
            ContentList.Add(CleanUpdateLine(SortedFeatures(i).Value))
        Next
        If SortedFeatures.Count > 15 Then FeatureCount += SortedFeatures.Count - 15
        If FeatureCount > 0 OrElse BugCount > 0 Then
            ContentList.Add(CleanUpdateLine(
                If(FeatureCount > 0, FeatureCount & " 项小调整与修改 ", "") &
                If(BugCount > 0, "修复了 " & BugCount & " 个 Bug ", "") &
                "详见完整更新日志"))
        End If
        Dim Content As String = "· " & ContentList.Join(vbCrLf & "· ")

        Content &= vbCrLf & vbCrLf & "注意：PCL PE 是第三方基于 PCL2 开发的非官方分支 并非 PCL2 官方发布 请注意辨别"

        Dim ShowLog As Action =
            Sub()
                If MyMsgBox(Content, "PCL PE 已更新至 " & VersionBaseName, "确定", "完整更新日志") = 2 Then
                    OpenWebsite("https://meloong.com/afd/a/LTCat?tab=feed")
                End If
            End Sub
        RunInUi(
        Sub()
            If IsWindowLoadFinished Then
                ShowLog()
            Else
                AddHandler ContentRendered, Sub(s, e) ShowLog()
            End If
        End Sub)
    End Sub

    Private Shared Function CleanUpdateLine(Text As String) As String
        Dim Result As String = Text.Replace("，", " ").Replace(",", " ").Replace("。", "").Replace("；", " ").Replace("：", " ")
        While Result.Contains("  ")
            Result = Result.Replace("  ", " ")
        End While
        Return Result.Trim()
    End Function

    Private IsShowAnimationStarted As Boolean = False
    Private Sub FormMain_ContentRendered(sender As Object, e As EventArgs)
        RemoveHandler ContentRendered, AddressOf FormMain_ContentRendered

        Dispatcher.BeginInvoke(New Action(AddressOf ShowWindowAnimation), Windows.Threading.DispatcherPriority.Background)
    End Sub
    Private Sub ShowWindowAnimation()
        If IsShowAnimationStarted Then Return
        IsShowAnimationStarted = True

        If Not Settings.Get(Of Boolean)("UiStartAnimation") Then
            Dim TargetOpacity As Double = Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4
            TransformPos.Y = 0
            TransformRotate.Angle = 0
            Opacity = TargetOpacity
            PanBack.RenderTransform = Nothing
            FinishWindowLoad()
            AniControlEnabled -= 1
            Logger.Info($"已关闭启动动画，DPI：{DPI}，窗口 {Math.Round(ActualWidth)}x{Math.Round(ActualHeight)} DIP")
            Return
        End If
        RunShowAnimation()
    End Sub
    Private IsShowAnimationRunning As Boolean = False
    Private Sub RunShowAnimation()
        If IsShowAnimationRunning Then Return
        IsShowAnimationRunning = True
        Dim TargetOpacity As Double = Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4
        Dim StartTick As Long = GetTimeMs()
        AniStart({
            AaCode(Sub() AniControlEnabled -= 1, 50),
            AaOpacity(Me, TargetOpacity, 250, 100),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(
            Sub()
                PanBack.RenderTransform = Nothing
                FinishWindowLoad()
                Logger.Info($"DPI：{DPI}，窗口 {Math.Round(ActualWidth)}x{Math.Round(ActualHeight)} DIP（{Math.Round(ActualWidth * DPI / 96)}x{Math.Round(ActualHeight * DPI / 96)} 像素），工作区 {SystemParameters.WorkArea}，系统版本：{Environment.OSVersion.VersionString}，PCL 位置：{PathExe}")
                Logger.Info($"启动动画用时：{GetTimeMs() - StartTick} ms（设计 700 ms）")
            End Sub, , True)
        }, "Form Show")

        RunInNewThread(
        Sub()
            Thread.Sleep(1500)
            RunInUi(
            Sub()
                If Opacity <= 0.05 Then
                    Logger.Warn("启动动画未能正常完成，已强制显示主窗口")
                    Opacity = TargetOpacity
                    PanBack.RenderTransform = Nothing
                    FinishWindowLoad()
                End If
            End Sub)
        End Sub, "启动动画监视")
    End Sub

    Private IsWindowLoadFinished As Boolean = False

    Private Sub FinishWindowLoad()
        If IsWindowLoadFinished Then Return
        IsWindowLoadFinished = True
        ModGlass.GlassReady = True
        ModGlass.GlassInvalidate()

        ModGlass.GlassWatchWallpaper(Sub() RefreshUiStyle(True))
        RefreshUiStyle()
        ScheduleBackgroundBlurCache()
    End Sub
    Public Sub New()
        ApplicationStartTick = GetTimeMs()

        FrmMain = Me
        FrmLaunchLeft = New PageLaunchLeft
        FrmLaunchRight = New PageLaunchRight

        Dim LastVersion As Integer = Settings.Get(Of Integer)("SystemLastVersionReg")
        If LastVersion < VersionCode Then

            UpgradeSub(LastVersion)
        ElseIf LastVersion > VersionCode Then

            DowngradeSub(LastVersion)
        End If

        If Not Settings.HasSaved("LaunchArgumentIndieV2") Then
            If Settings.HasSaved("LaunchArgumentIndie") Then
                Logger.Info("从老 PCL 迁移版本隔离")
                Settings.Set("LaunchArgumentIndieV2", Settings.Get(Of Integer)("LaunchArgumentIndie"))
            ElseIf HasIniKey("Setup", "LaunchVersionSelect") Then
                Logger.Info("从老 PCL 升级，但此前未调整版本隔离，使用老的版本隔离默认值")
                Settings.Set("LaunchArgumentIndieV2", Settings.GetDefault("LaunchArgumentIndie"))
            Else
                Logger.Info("全新的 PCL PE，使用新的版本隔离默认值")
                Settings.Set("LaunchArgumentIndieV2", Settings.GetDefault("LaunchArgumentIndieV2"))
            End If
        End If

        ThemeCheckAll(False)
        ThemeRefresh(Settings.Get(Of Integer)("UiLauncherTheme"))

        [AddHandler](DragDrop.DragEnterEvent, New DragEventHandler(AddressOf HandleDrag), handledEventsToo:=True)
        [AddHandler](DragDrop.DragOverEvent, New DragEventHandler(AddressOf HandleDrag), handledEventsToo:=True)

        InitializeComponent()
        Opacity = 0

        Try
            Height = Settings.Get(Of Integer)("WindowHeight")
            Width = Settings.Get(Of Integer)("WindowWidth")
        Catch ex As Exception
            Logger.Error(ex, "读取窗口默认大小失败", LogBehavior.Toast)
            Height = MinHeight + 100
            Width = MinWidth + 100
        End Try

        If WindowsUtils.HasAdminRole() Then
            Static Helper As New DragHelper
            AddHandler SourceInitialized,
            Sub()
                Dim WpfHelper As New WindowInteropHelper(Me)
                Helper.HwndIntPtrSource = HwndSource.FromHwnd(WpfHelper.Handle)
                Helper.AddHook()
            End Sub
            AddHandler Closing, Sub() Helper.RemoveDragHook()
            AddHandler Helper.DragDrop, Sub() FileDrag(Helper.DropFilePaths)
        End If

        If Not IsNothing(FrmLaunchLeft.Parent) Then FrmLaunchLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If Not IsNothing(FrmLaunchRight.Parent) Then FrmLaunchRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PanMainLeft.Child = FrmLaunchLeft
        PageLeft = FrmLaunchLeft
        PanMainRight.Child = FrmLaunchRight
        PageRight = FrmLaunchRight
        FrmLaunchRight.PageState = MyPageRight.PageStates.ContentStay

        If BuildType = BuildTypes.Debug Then Hint("[开发者模式] PCL 正以开发者模式运行，这可能会造成严重的性能下降，请务必立即向开发者反馈此问题！", HintType.Red)
        If ModeDebug Then Hint("[调试模式] PCL 正以调试模式运行，这可能会导致性能下降，若无必要请不要开启！")

        McFolderListLoader.Start(0)

        Logger.Info($"第二阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")
    End Sub
    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ApplicationStartTick = GetTimeMs()
        Handle = New WindowInteropHelper(Me).Handle

        UpdateBackgroundAndTitleBar()
        FontFamilyRefresh()
        FontColorRefresh()
        FontSizeRefresh()

        If Settings.Get(Of Integer)("UiUiStyle") = 1 Then ModGlass.GlassRefreshFill(RectLeftBackground)
        PageSetupUI.HiddenRefresh()
        PageSetupUI.BackgroundRefresh(False, True)
        MusicRefreshPlay(False, True)

        VideoBackgroundUpdate()

        BtnExtraDownload.ShowCheck = AddressOf BtnExtraDownload_ShowCheck
        BtnExtraBack.ShowCheck = AddressOf BtnExtraBack_ShowCheck
        BtnExtraApril.ShowCheck = AddressOf BtnExtraApril_ShowCheck
        BtnExtraShutdown.ShowCheck = AddressOf BtnExtraShutdown_ShowCheck
        BtnExtraApril.ShowRefresh()

        Dim Resizer As New MyResizer(Me)
        Resizer.addResizerDown(ResizerB)
        Resizer.addResizerLeft(ResizerL)
        Resizer.addResizerLeftDown(ResizerLB)
        Resizer.addResizerLeftUp(ResizerLT)
        Resizer.addResizerRight(ResizerR)
        Resizer.addResizerRightDown(ResizerRB)
        Resizer.addResizerRightUp(ResizerRT)
        Resizer.addResizerUp(ResizerT)

        If RandomInteger(1, 1000) = 233 Then
            ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString("M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5")
        End If

        ThemeRefreshMain()
        WindowOpacityRefresh(Nothing)
        TitleBarHeightRefresh(Nothing)

        Topmost = Settings.Get(Of Boolean)("UiWindowTopmost")

        Dim WorkArea As Rect = SystemParameters.WorkArea
        Top = Math.Max(WorkArea.Top, Math.Min(WorkArea.Top + (WorkArea.Height - Height) / 2, WorkArea.Bottom - Height))
        Left = Math.Max(WorkArea.Left, Math.Min(WorkArea.Left + (WorkArea.Width - Width) / 2, WorkArea.Right - Width))
        IsSizeSaveable = True
        ShowWindowToTop()
        Dim HwndSource As Interop.HwndSource = PresentationSource.FromVisual(Me)
        HwndSource.AddHook(New Interop.HwndSourceHook(AddressOf WndProc))

        AddHandler ContentRendered, AddressOf FormMain_ContentRendered

        RunInNewThread(
        Sub()
            Thread.Sleep(1200)
            RunInUi(
            Sub()
                If Not IsShowAnimationStarted Then Logger.Warn("启动动画等待 UI 空闲超时，按下限时机开始播放")
                ShowWindowAnimation()
            End Sub)
        End Sub, "启动动画兜底")

        AniStart()
        TimerMainStart()

        If Not IsVisible Then Visibility = Visibility.Visible
        If WindowState = WindowState.Minimized Then WindowState = WindowState.Normal

        RunInNewThread(
        Sub()
            Thread.Sleep(600)
            RunInUi(
            Sub()
                Try

                    If Opacity <= 0.05 Then
                        Opacity = Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4
                        PanBack.RenderTransform = Nothing
                        FinishWindowLoad()
                        Logger.Warn($"启动自检：窗口不透明度为 {Opacity}，已强制恢复")
                    End If
                    Dim Hwnd As IntPtr = New Interop.WindowInteropHelper(Me).Handle
                    If Hwnd <> IntPtr.Zero Then
                        ShowWindow(Hwnd, SW_SHOW)
                        SetForegroundWindow(Hwnd)
                        Logger.Info($"启动自检：已强制显示主窗口（句柄 {Hwnd}）")
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "强制显示主窗口失败")
                End Try
            End Sub)
        End Sub, "强制显示窗口")

        RunInNewThread(
        Sub()
            Thread.Sleep(1200)
            RunInUi(
            Sub()
                Try
                    If Settings.Get(Of Boolean)("UiUiStyleChosen") Then Return
                    Settings.Set("UiUiStyleChosen", True)
                    Dim Result As Integer = MyMsgBox("欢迎使用 PCL PE！" & vbCrLf &
                                                     "先选一个喜欢的界面风格，之后可以在 设置 → 个性化 → 基础 里随时切换。" & vbCrLf & vbCrLf &
                                                     "简洁版：白色卡片，清爽直接。" & vbCrLf &
                                                     "毛玻璃版：半透明磨砂卡片，壁纸会透出来。",
                                                     "选择界面风格", "简洁版", "毛玻璃版")
                    Settings.Set("UiUiStyle", If(Result = 2, 1, 0))
                    RefreshUiStyle()
                Catch ex As Exception
                    Logger.Warn(ex, "显示界面风格引导失败")
                End Try
            End Sub)
        End Sub, "界面风格引导")

        CheckUpdateAtStartup()

        ModOfficialSaveSync.StartAutoCheck()

        ModFirewall.CheckAtStartup()

        If Not String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PCL_DEBUG_PAGE")) Then Settings.Set("SystemEulaVersion", 2)
        Select Case Environment.GetEnvironmentVariable("PCL_DEBUG_PAGE")
            Case "test"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Other, PageSubType.OtherTest))
                               End Sub, "调试跳页")
            Case "setup"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Setup, PageSubType.SetupUI))
                               End Sub, "调试跳页")
            Case "download"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Download, PageSubType.DownloadInstall))
                               End Sub, "调试跳页")
            Case "link"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Link))
                               End Sub, "调试跳页")
            Case "msg"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() MyMsgBox("这是一条用于验证毛玻璃弹窗的提示内容。" & vbCrLf & "弹窗会模糊背后的界面内容。", "毛玻璃测试", "确定"))
                               End Sub, "调试跳页")
            Case "about"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Other, PageSubType.OtherAbout))
                               End Sub, "调试跳页")
            Case "mod"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Download, PageSubType.DownloadMod))
                               End Sub, "调试跳页")
            Case "resource"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Download, PageSubType.DownloadResourcePack))
                               End Sub, "调试跳页")
            Case "shader"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Download, PageSubType.DownloadShader))
                               End Sub, "调试跳页")
            Case "feedback"
                RunInNewThread(Sub()
                                   Thread.Sleep(1500)
                                   RunInUi(Sub() PageChange(PageType.Other, PageSubType.OtherFeedback))
                               End Sub, "调试跳页")
        End Select

        RunInNewThread(
        Sub()

            Settings.Set("SystemEulaVersion", 2)

            Try
                JavaInit()
                Thread.Sleep(100)
                DlClientListMojangLoader.Start(1)
                RunCountSub()
                ServerLoader.Start()
                RunInNewThread(AddressOf TryClearTaskTemp, "TryClearTaskTemp", ThreadPriority.BelowNormal)
            Catch ex As Exception
                Logger.Error(ex, "初始化加载池运行失败")
            End Try

            Try
                FileUtils.Delete(Paths.Base & "PCL\Plain Craft Launcher Pro.exe")
            Catch ex As Exception
                Logger.Warn(ex, "清理自动更新文件失败")
            End Try

            Telemetry("启动")
        End Sub, "初始化", ThreadPriority.Lowest)

        Logger.Info($"第三阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")
    End Sub

    Private Sub RunCountSub()
        Settings.Set("SystemCount", Settings.Get(Of Integer)("SystemCount") + 1)
        If BuildType = BuildTypes.Snapshot Then
            Select Case Settings.Get(Of Integer)("SystemCount")
                Case 1
                    MyMsgBox("欢迎使用 PCL 快照版！" & vbCrLf &
                             "快照版包含尚未在正式版发布的测试性功能，仅用于赞助者本人尝鲜。所以请不要发给其他人或者用于制作整合包哦！" & vbCrLf &
                             "如果你并非通过赞助或赞助者本人邀请进群获得的本程序，那么可能是有人在违规传播，记得提醒他一下啦。", "快照版使用说明")
            End Select
            If Settings.Get(Of Integer)("SystemCount") >= 99 Then
                If ThemeUnlock(6, False) Then
                    MyMsgBox("你已经使用了 99 次 PCL 啦，感谢你长期以来的支持！" & vbCrLf &
                             "隐藏主题 铁杆粉 已解锁！", "提示")
                End If
            End If
        End If
    End Sub

    Private Sub UpgradeSub(LastVersionCode As Integer)
        Logger.Info($"版本号从 {LastVersionCode} 升高到 {VersionCode}")
        Settings.Set("SystemLastVersionReg", VersionCode)

        Dim HighestVersionCode As Integer
        Const SettingKey = If(BuildType = BuildTypes.Snapshot, "SystemHighestAlphaVersionReg", "SystemHighestBetaVersionReg")
        HighestVersionCode = Settings.Get(Of Integer)(SettingKey)
        If HighestVersionCode < VersionCode Then
            Settings.Set(SettingKey, VersionCode)
            Logger.Info($"最高版本号从 {HighestVersionCode} 升高到 {VersionCode}")
        End If

        If Settings.Get(Of Integer)("LaunchArgumentWindowType") = 5 Then Settings.Set("LaunchArgumentWindowType", 1)

        If HighestVersionCode <= 207 Then
            Dim UnlockedTheme As New List(Of String) From {"2"}
            UnlockedTheme.AddRange(New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide").ToString.Split("|")))
            UnlockedTheme.AddRange(New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|")))
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Distinct.ToList.Join("|"c))
        End If

        If LastVersionCode <= 115 AndAlso Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|").Contains("13") Then
            Dim UnlockedTheme As New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("13")
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Join("|"c))
            MyMsgBox("由于新版 PCL 修改了欧皇彩的解锁方式，你需要重新解锁欧皇彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If

        If LastVersionCode <= 152 AndAlso Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|").Contains("12") Then
            Dim UnlockedTheme As New List(Of String)(Settings.Get(Of String)("UiLauncherThemeHide2").ToString.Split("|"))
            UnlockedTheme.Remove("12")
            Settings.Set("UiLauncherThemeHide2", UnlockedTheme.Join("|"c))
            MyMsgBox("由于新版 PCL 修改了滑稽彩的解锁方式，你需要重新解锁滑稽彩。" & vbCrLf &
                     "多谢各位的理解啦！", "重新解锁提醒")
        End If

        If LastVersionCode <= 161 AndAlso FileUtils.Exists(Paths.Base & "PCL\CustomSkin.png") AndAlso Not FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then
            FileUtils.Copy(Paths.Base & "PCL\CustomSkin.png", Paths.AppDataThenName & "CustomSkin.png")
            Logger.Info("已移动离线自定义皮肤 (162)")
        End If
        If LastVersionCode <= 263 AndAlso FileUtils.Exists(PathTemp & "CustomSkin.png") AndAlso Not FileUtils.Exists(Paths.AppDataThenName & "CustomSkin.png") Then
            FileUtils.Copy(PathTemp & "CustomSkin.png", Paths.AppDataThenName & "CustomSkin.png")
            Logger.Info("已移动离线自定义皮肤 (264)")
        End If

        If LastVersionCode <= 205 Then
            Settings.Set("UiHiddenOtherHelp", False)
            Logger.Info("已解除帮助页面的隐藏")
        End If

        If Not Settings.Get(Of Boolean)("CacheMsV2Migrated") Then
            Settings.Set("CacheMsV2Migrated", True)
            Settings.Set("CacheMsV2OAuthRefresh", Settings.Get(Of String)("CacheMsOAuthRefresh"))
            Settings.Set("CacheMsV2Access", Settings.Get(Of String)("CacheMsAccess"))
            Settings.Set("CacheMsV2ProfileJson", Settings.Get(Of String)("CacheMsProfileJson"))
            Settings.Set("CacheMsV2Uuid", Settings.Get(Of String)("CacheMsUuid"))
            Settings.Set("CacheMsV2Name", Settings.Get(Of String)("CacheMsName"))
            Logger.Info("已从老版本迁移微软登录结果")
        End If

        If Settings.HasSaved("ToolDownloadTranslate") AndAlso Not Settings.HasSaved("ToolDownloadTranslateV2") Then
            Settings.Set("ToolDownloadTranslateV2", Settings.Get(Of Integer)("ToolDownloadTranslate") + 1)
            Logger.Info("已从老版本迁移 Mod 命名设置")
        End If

        If LastVersionCode <= 381 AndAlso Settings.HasSaved("LaunchAdvanceJvm") AndAlso
           Settings.Get(Of String)("LaunchAdvanceJvm").ToString.Replace("-XX:+UseG1GC ", "").Replace("-XX:-UseAdaptiveSizePolicy ", "").Trim = Settings.GetDefault("LaunchAdvanceJvm") Then
            Settings.Reset("LaunchAdvanceJvm")
            Logger.Info("已重置 JVM 参数设置")
        End If

        If LastVersionCode <= 0 Then Return
        If HighestVersionCode >= VersionCode Then Return
        ShowUpdateLog(HighestVersionCode)
    End Sub
    Private Sub DowngradeSub(LastVersionCode As Integer)
        Logger.Info($"版本号从 {LastVersionCode} 降低到 {VersionCode}")
        Settings.Set("SystemLastVersionReg", VersionCode)
    End Sub

#End Region

#Region "自定义窗口"

    Private Sub FormMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        EndProgram(True)
        e.Cancel = True
    End Sub

    Public Sub EndProgram(SendWarning As Boolean)

        If HasDownloadingTask() Then
            If SendWarning AndAlso MyMsgBox("还有下载任务尚未完成，是否确定退出？", "提示", "确定", "取消") = 2 Then Return
            RunInNewThread(
            Sub()
                Logger.Info("正在强行停止任务")
                For Each Task As LoaderBase In LoaderTaskbar.ToList()
                    Task.Cancel()
                Next
            End Sub, "强行停止下载任务")
        End If

        If FrmLinkMain?.TryExit(Not SendWarning, True) Then Return

        RunInUiWait(
        Sub()
            IsHitTestVisible = False
            If PanBack.RenderTransform Is Nothing Then
                Dim TransformPos As New TranslateTransform(0, 0)
                Dim TransformRotate As New RotateTransform(0)
                Dim TransformScale As New ScaleTransform(1, 1)
                PanBack.RenderTransform = New TransformGroup() With {.Children = New TransformCollection({TransformRotate, TransformPos, TransformScale})}
                AniStart({
                    AaOpacity(Me, -Opacity, 140, 40, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(
                    Sub(i)
                        TransformScale.ScaleX += i
                        TransformScale.ScaleY += i
                    End Sub, 0.88 - TransformScale.ScaleX, 180),
                    AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 180, 0, New AniEaseOutFluent(AniEasePower.Weak)),
                    AaDouble(Sub(i) TransformRotate.Angle += i, 0.6 - TransformRotate.Angle, 180, 0, New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaCode(
                    Sub()
                        IsHitTestVisible = False
                        Top = -10000
                        ShowInTaskbar = False
                    End Sub, 210),
                    AaCode(AddressOf EndProgramForce, 230)
                }, "Form Close")
            Else
                EndProgramForce()
            End If
            Logger.Info("收到关闭指令")
        End Sub)
    End Sub
    Private Shared IsLogShown As Boolean = False
    Public Shared Sub EndProgramForce(Optional ReturnCode As ProcessReturnValues = ProcessReturnValues.Success)
        On Error Resume Next
        IsProgramEnding = True
        AniControlEnabled += 1
        If IsUpdateWaitingRestart Then UpdateRestart(False)
        If ReturnCode = ProcessReturnValues.Exception Then
            If Not IsLogShown Then
                FeedbackInfo()
                Logger.Info("请在 https://github.com/Meloong-Git/PCL/issues 提交错误报告，以便于作者解决此问题！")
                IsLogShown = True
                StartProcess(Paths.Base & "PCL\Log1.txt")
            End If
            Thread.Sleep(500)
        End If
        Logger.Info($"程序已退出，返回值：{ReturnCode}")
        ConfigUtils.SaveAll()
        Logger.Instance.Flush()
        If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
        Process.GetCurrentProcess.Kill()
    End Sub
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        EndProgram(True)
    End Sub

    Private Sub FormDragMove(sender As Object, e As MouseButtonEventArgs) Handles PanTitle.MouseLeftButtonDown, PanMsg.MouseLeftButtonDown
        On Error Resume Next
        If sender.IsMouseDirectlyOver Then DragMove()
    End Sub

    Public IsSizeSaveable As Boolean = False
    Private Sub FormMain_SizeChanged() Handles Me.SizeChanged, Me.Loaded
        If IsSizeSaveable Then
            Settings.Set("WindowHeight", Height)
            Settings.Set("WindowWidth", Width)
        End If
        RectForm.Rect = New Rect(0, 0, BorderForm.ActualWidth, BorderForm.ActualHeight)

        Dim Radius As Double = Settings.Get(Of Integer)("UiWindowRadius")
        If RectForm.RadiusX <> Radius Then
            RectForm.RadiusX = Radius
            RectForm.RadiusY = Radius
        End If
        PanForm.Width = BorderForm.ActualWidth + 0.001
        PanForm.Height = BorderForm.ActualHeight + 0.001
        PanMain.Width = PanForm.Width
        PanMain.Height = Math.Max(0, PanForm.Height - PanTitle.ActualHeight)

        ScheduleBackgroundBlurCache()
        ScheduleGlassRealign()
        VideoBackFitLayout()
        If WindowState = WindowState.Maximized Then WindowState = WindowState.Normal
    End Sub

    Public Shared Sub WindowRadiusRefresh(Value As Object)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        Dim Radius As Double = Settings.Get(Of Integer)("UiWindowRadius")
        FrmMain.RectForm.RadiusX = Radius
        FrmMain.RectForm.RadiusY = Radius
        FrmMain.RectForm.Rect = New Rect(0, 0, FrmMain.BorderForm.ActualWidth, FrmMain.BorderForm.ActualHeight)
    End Sub

    Public Shared Sub WindowTopmostRefresh(Value As Object)
        If FrmMain Is Nothing Then Return
        FrmMain.Topmost = CBool(Value)
    End Sub

    Public Shared Sub WindowOpacityRefresh(Value As Object)
        If FrmMain Is Nothing Then Return
        Dim OpacityValue As Integer = Settings.Get(Of Integer)("UiWindowOpacity")
        FrmMain.PanBack.Opacity = If(OpacityValue <= 0 OrElse OpacityValue >= 100, 1, OpacityValue / 100)
    End Sub

    Public Shared Sub TitleBarHeightRefresh(Value As Object)
        If FrmMain Is Nothing Then Return
        Dim HeightValue As Integer = Settings.Get(Of Integer)("UiTitleBarHeight")
        FrmMain.PanTitle.Height = If(HeightValue < 30, 48, HeightValue)
        If FrmMain.IsLoaded Then FrmMain.FormMain_SizeChanged()
    End Sub

    Private Sub BtnTitleMin_Click() Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

    Private _bgSource As ImageSource
    Private _bgCached As ImageSource
    Private _bgBlurRadius As Double = 33
    Private _bgSuit As Integer = -1
    Private _bgCacheTimer As System.Windows.Threading.DispatcherTimer
    Private _bgCaching As Boolean

    Private Sub ApplyBackgroundSuit(Brush As ImageBrush)
        If _bgSource Is Nothing Then Return
        Dim BackgroundType As Integer = Settings.Get(Of Integer)("UiBackgroundSuit")
        If BackgroundType = 0 Then

            BackgroundType = 2
        End If
        Brush.Stretch = Stretch.UniformToFill
        Brush.TileMode = TileMode.None
        Brush.Viewport = New Rect(0, 0, 1, 1)
        Brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox
        Brush.AlignmentX = AlignmentX.Center
        Brush.AlignmentY = AlignmentY.Center
        Select Case BackgroundType
            Case 1
                Brush.Stretch = Stretch.None
            Case 3
                Brush.Stretch = Stretch.Fill
            Case 4
                Brush.Stretch = Stretch.None
                Brush.TileMode = TileMode.Tile
                Brush.Viewport = New Rect(0, 0, _bgSource.Width, _bgSource.Height)
                Brush.ViewportUnits = BrushMappingMode.Absolute

            Case 2
            Case 5
                Brush.AlignmentX = AlignmentX.Left
                Brush.AlignmentY = AlignmentY.Top
            Case 6
                Brush.AlignmentX = AlignmentX.Right
                Brush.AlignmentY = AlignmentY.Top
            Case 7
                Brush.AlignmentX = AlignmentX.Left
                Brush.AlignmentY = AlignmentY.Bottom
            Case 8
                Brush.AlignmentX = AlignmentX.Right
                Brush.AlignmentY = AlignmentY.Bottom
            Case 9
                Brush.AlignmentX = AlignmentX.Left
                Brush.AlignmentY = AlignmentY.Center
            Case 10
                Brush.AlignmentX = AlignmentX.Right
                Brush.AlignmentY = AlignmentY.Center
            Case 11
                Brush.AlignmentX = AlignmentX.Center
                Brush.AlignmentY = AlignmentY.Top
            Case 12
                Brush.AlignmentX = AlignmentX.Center
                Brush.AlignmentY = AlignmentY.Bottom
        End Select
    End Sub

    Private Sub ApplyBackgroundEffect(BlurRadius As Double)
        ImgBack.Margin = New Thickness(0)
        If BlurRadius <= 1 Then
            ImgBack.Effect = Nothing
        Else
            ImgBack.Effect = New Effects.BlurEffect With {.Radius = BlurRadius, .RenderingBias = RenderingBias.Performance}
        End If
    End Sub

    Private Sub ScheduleBackgroundBlurCache()
        If _bgSource Is Nothing OrElse ImgBack.Visibility <> Visibility.Visible Then Return
        If _bgBlurRadius <= 1 Then Return
        If IsWindowLoadFinished AndAlso _bgCached Is Nothing Then

            BuildBackgroundBlurCache()
            Return
        End If
        If _bgCacheTimer Is Nothing Then
            _bgCacheTimer = New System.Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(160)}
            AddHandler _bgCacheTimer.Tick,
                Sub(sender, e)
                    _bgCacheTimer.Stop()
                    BuildBackgroundBlurCache()
                End Sub
        End If

        _bgCacheTimer.Interval = TimeSpan.FromMilliseconds(If(IsWindowLoadFinished, 160, 1000))
        _bgCacheTimer.Stop()
        _bgCacheTimer.Start()
    End Sub

    Private _glassRealignTimer As Windows.Threading.DispatcherTimer = Nothing

    Private Sub ScheduleGlassRealign()
        If Not IsWindowLoadFinished Then Return
        If _glassRealignTimer Is Nothing Then
            _glassRealignTimer = New Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(180)}
            AddHandler _glassRealignTimer.Tick,
                Sub(sender, e)
                    _glassRealignTimer.Stop()
                    Try
                        ModGlass.GlassInvalidate()
                        RefreshUiStyle()
                    Catch ex As Exception
                        Logger.Info($"毛玻璃重新对齐失败：{ex.Message}")
                    End Try
                End Sub
        End If
        _glassRealignTimer.Stop()
        _glassRealignTimer.Start()
    End Sub

    Private _opacityTimer As Windows.Threading.DispatcherTimer = Nothing

    Private Sub ScheduleGlassOpacityRefresh()
        If Not IsWindowLoadFinished Then Return
        If _opacityTimer Is Nothing Then
            _opacityTimer = New Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(150)}
            AddHandler _opacityTimer.Tick,
                Sub(sender, e)
                    _opacityTimer.Stop()
                    Try
                        ModGlass.GlassInvalidate()
                        RefreshUiStyle(True)
                    Catch ex As Exception
                        Logger.Info($"刷新背景透明度失败：{ex.Message}")
                    End Try
                End Sub
        End If
        _opacityTimer.Stop()
        _opacityTimer.Start()
    End Sub

    Private Sub BuildBackgroundBlurCache()
        If _bgCaching OrElse _bgSource Is Nothing Then Return
        If ImgBack.Visibility <> Visibility.Visible Then Return
        If ImgBack.ActualWidth < 64 OrElse ImgBack.ActualHeight < 64 Then Return
        If Not IsWindowLoadFinished Then

            If _bgCacheTimer IsNot Nothing Then
                _bgCacheTimer.Stop()
                _bgCacheTimer.Interval = TimeSpan.FromMilliseconds(250)
                _bgCacheTimer.Start()
            End If
            Return
        End If
        _bgCaching = True
        Try

            Dim Brush As ImageBrush = TryCast(ImgBack.Background, ImageBrush)
            If Brush Is Nothing Then Return

            Brush.ImageSource = _bgSource
            ApplyBackgroundSuit(Brush)

            ImgBack.Effect = Nothing
            ImgBack.Margin = New Thickness(0)
            Dim Width As Double = ImgBack.ActualWidth
            Dim Height As Double = ImgBack.ActualHeight
            Dim DpiScale As Double = (DPI / 96) * Math.Min(1.0, 1024.0 / Math.Max(1.0, Width * DPI / 96))

            Dim Pad As Double = If(_bgBlurRadius > 1, _bgBlurRadius * 1.8, 0)
            Dim CanvasWidth As Integer = Math.Max(1, CInt(Math.Round((Width + Pad * 2) * DpiScale)))
            Dim CanvasHeight As Integer = Math.Max(1, CInt(Math.Round((Height + Pad * 2) * DpiScale)))
            Dim Visual As New DrawingVisual()
            If _bgBlurRadius > 1 Then
                Visual.Effect = New Effects.BlurEffect With {.Radius = _bgBlurRadius * DpiScale, .KernelType = Effects.KernelType.Gaussian, .RenderingBias = RenderingBias.Performance}
            End If

            Visual.Transform = New TranslateTransform(Pad, Pad)

            Using DC As DrawingContext = Visual.RenderOpen()
                DC.DrawRectangle(Brush, Nothing, New Rect(0, 0, Width, Height))
            End Using
            Dim Bitmap As New RenderTargetBitmap(CanvasWidth, CanvasHeight, 96 * DpiScale, 96 * DpiScale, PixelFormats.Pbgra32)
            Bitmap.Render(Visual)

            Dim OffsetX As Integer = Math.Max(0, CInt(Math.Round(Pad * DpiScale)))
            Dim OffsetY As Integer = OffsetX
            Dim CropWidth As Integer = Math.Min(CanvasWidth - OffsetX, Math.Max(1, CInt(Math.Round(Width * DpiScale))))
            Dim CropHeight As Integer = Math.Min(CanvasHeight - OffsetY, Math.Max(1, CInt(Math.Round(Height * DpiScale))))
            Dim Result As New CroppedBitmap(Bitmap, New Int32Rect(OffsetX, OffsetY, CropWidth, CropHeight))
            Result.Freeze()
            _bgCached = Result
            Brush.ImageSource = _bgCached
            Brush.Stretch = Stretch.Fill
            Brush.TileMode = TileMode.None
            Brush.Viewport = New Rect(0, 0, 1, 1)
            Brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox
            Brush.AlignmentX = AlignmentX.Center
            Brush.AlignmentY = AlignmentY.Center
            Logger.Info($"背景模糊已预烘焙：{CanvasWidth}x{CanvasHeight} 像素，裁切 {CropWidth}x{CropHeight}")

            ModGlass.GlassInvalidate()
            RefreshUiStyle()
        Catch ex As Exception
            Logger.Warn(ex, "预烘焙背景模糊失败，回退到实时模糊")
            _bgCached = Nothing
            ApplyBackgroundEffect(_bgBlurRadius)
        Finally
            _bgCaching = False
        End Try
    End Sub

    Public Sub ClearBackgroundCaches()
        Try
            If _bgCacheTimer IsNot Nothing Then _bgCacheTimer.Stop()
            _bgCached = Nothing
            _bgSource = Nothing
            _bgBlurRadius = 0
            ModGlass.GlassInvalidate()
            ModGlass.GlassInvalidateSnapshot()
            RefreshUiStyle(True)
            Logger.Info("背景图片已清除，画笔与毛玻璃底图缓存均已失效")
        Catch ex As Exception
            Logger.Info($"清理背景缓存失败：{ex.Message}")
        End Try
    End Sub

    Public Shared Sub UpdateBackgroundAndTitleBar(Value)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        FrmMain.UpdateBackgroundAndTitleBar()
    End Sub
    Public Sub UpdateBackgroundAndTitleBar()
        Logger.Info("从设置更新背景图片与标题栏样式")

        Dim NewOpacity As Double = Settings.Get(Of Integer)("UiBackgroundOpacity") / 1000
        Dim OpacityChanged As Boolean = Math.Abs(ImgBack.Opacity - NewOpacity) > 0.001
        ImgBack.Opacity = NewOpacity

        Dim BlurRadius As Double = Settings.Get(Of Integer)("UiBackgroundBlur") + 1
        Dim BackgroundType As Integer = Settings.Get(Of Integer)("UiBackgroundSuit")

        Dim ImageChanged As Boolean = False
        Dim BackgroundBrush As ImageBrush = TryCast(ImgBack.Background, ImageBrush)
        If BackgroundBrush IsNot Nothing Then
            Dim Source As ImageSource = BackgroundBrush.ImageSource
            If Source IsNot Nothing AndAlso Source IsNot _bgCached Then
                ImageChanged = Source IsNot _bgSource
                If ImageChanged Then
                    _bgSource = Source
                    _bgCached = Nothing
                End If
            End If
        End If
        Dim NeedRebuild As Boolean = _bgCached Is Nothing OrElse _bgSource Is Nothing OrElse ImageChanged OrElse
                                    Math.Abs(BlurRadius - _bgBlurRadius) > 0.001 OrElse BackgroundType <> _bgSuit
        _bgBlurRadius = BlurRadius
        _bgSuit = BackgroundType
        If NeedRebuild Then

            _bgCached = Nothing

            If BackgroundBrush IsNot Nothing Then

                If _bgSource IsNot Nothing Then BackgroundBrush.ImageSource = _bgSource
                ApplyBackgroundSuit(BackgroundBrush)
            End If

            ApplyBackgroundEffect(_bgBlurRadius)

            ModGlass.GlassInvalidate()
            If _bgBlurRadius <= 1 Then

                RefreshUiStyle(True)
            Else

                ScheduleBackgroundBlurCache()
            End If
        ElseIf OpacityChanged Then

            ScheduleGlassOpacityRefresh()
        End If

        Dim LogoType As Integer = Settings.Get(Of Integer)("UiLogoType")
        If LogoType = 2 Then LogoType = 1
        Select Case LogoType
            Case 0
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Collapsed
                ImageTitleLogo.Visibility = Visibility.Collapsed
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Visible
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed
                End If
            Case 1
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Visible
                ImageTitleLogo.Visibility = Visibility.Collapsed
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed
                End If
            Case 3
                ShapeTitleLogo.Visibility = Visibility.Collapsed
                LabTitleLogo.Visibility = Visibility.Collapsed
                ImageTitleLogo.Visibility = Visibility.Visible
                If Not IsNothing(FrmSetupUI) Then
                    FrmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed
                    FrmSetupUI.PanLogoChange.Visibility = Visibility.Visible
                End If
                Try
                    ImageTitleLogo.Source = Paths.Base & "PCL\Logo.png"
                Catch ex As Exception
                    ImageTitleLogo.Source = Nothing
                    Logger.Error(ex, "显示标题栏图片失败", LogBehavior.Alert)
                End Try
        End Select
        FrmSetupUI?.CardLogo.TriggerForceResize()

        LabTitleLogo.Text = "PCL PE"

        PanTitleMain.ColumnDefinitions(0).Width = New GridLength(If(Settings.Get(Of Boolean)("UiLogoLeft") AndAlso Settings.Get(Of Integer)("UiLogoType") = 0, 0, 1), GridUnitType.Star)
    End Sub

#End Region

#Region "窗体事件"

    Private Sub FormMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.IsRepeat Then Return

        If e.SystemKey = Key.LeftAlt OrElse e.SystemKey = Key.RightAlt Then e.Handled = True

        If PanMsg.Children.Any Then
            If e.Key = Key.Enter Then
                CType(PanMsg.Children(0), Object).Btn1_Click()
            ElseIf e.Key = Key.Escape Then
                Dim Msg As Object = PanMsg.Children(0)
                If TypeOf Msg IsNot MyMsgInput AndAlso TypeOf Msg IsNot MyMsgSelect AndAlso Msg.Btn3.Visibility = Visibility.Visible Then
                    Msg.Btn3_Click()
                ElseIf Msg.Btn2.Visibility = Visibility.Visible Then
                    Msg.Btn2_Click()
                Else
                    Msg.Btn1_Click()
                End If
            End If
            Return
        End If

        If e.Key = Key.Escape Then
            If PageCurrent = FormMain.PageType.Launch AndAlso McLaunchLoader.State = LoadState.Loading Then
                FrmLaunchLeft.BtnCancel_Click()
                Return
            End If
            TriggerPageBack()
        End If

        If e.Key = Key.F11 AndAlso PageCurrent = FormMain.PageType.InstanceSelect Then
            FrmSelectRight.ShowHidden = Not FrmSelectRight.ShowHidden
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Return
        End If

        If e.Key = Key.F12 Then
            PageSetupUI.HiddenForceShow = Not PageSetupUI.HiddenForceShow
            If PageSetupUI.HiddenForceShow Then
                Hint("功能隐藏设置已暂时关闭！", HintType.Green)
            Else
                Hint("功能隐藏设置已重新开启！", HintType.Green)
            End If
            PageSetupUI.HiddenRefresh()
            Return
        End If

        If e.Key = Key.F5 Then
            If TypeOf PageLeft Is IRefreshable Then CType(PageLeft, IRefreshable).Refresh()
            If TypeOf PageRight Is IRefreshable Then CType(PageRight, IRefreshable).Refresh()
            Return
        End If

        If e.Key = Key.Enter AndAlso PageCurrent = FormMain.PageType.Launch Then
            If IsAprilEnabled AndAlso Not IsAprilGiveup Then
                Hint("木大！")
            Else
                FrmLaunchLeft.LaunchButtonClick()
            End If
        End If
    End Sub
    Private Sub FormMain_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseDown

        If FrmMain.PanMsg.Children.Count > 0 OrElse WaitingMyMsgBox.Any Then Return
        If e.ChangedButton = MouseButton.XButton1 OrElse e.ChangedButton = MouseButton.XButton2 Then TriggerPageBack()
    End Sub

    Private Sub TriggerPageBack()
        If PageCurrent = PageType.Download AndAlso PageCurrentSub = PageSubType.DownloadInstall AndAlso FrmDownloadInstall.IsInSelectPage Then
            FrmDownloadInstall.ExitSelectPage()
        Else
            PageBack()
        End If
    End Sub

    Private Sub FormMain_Activated() Handles Me.Activated

        Try
            If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceMod Then

                FrmInstanceMod.ReloadModList()
            ElseIf PageCurrent = PageType.InstanceSetup AndAlso (PageCurrentSub = PageSubType.InstanceSetup OrElse PageCurrentSub = PageSubType.InstanceExport) Then

                PageInstanceLeft.ReloadCurrentJava()
            ElseIf PageCurrent = PageType.InstanceSelect Then

                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
            End If
        Catch ex As Exception
            Logger.Error(ex, "切回窗口时出错")
        End Try

        If PageLinkMain.LinkState <> PageLinkMain.LinkStates.Waiting Then Return
        If PageCurrent = PageType.Link Then Return
        Dim Code = ClipboardGetText() : If Code Is Nothing Then Return
        If Settings.Get(Of String)("LinkLastAutoJoinInviteCode") = Code Then Return
        If PageLinkMain.ValidateCodeFormat(Code) IsNot Nothing Then Return
        Settings.Set("LinkLastAutoJoinInviteCode", Code)
        RunInThread(
        Sub()
            If MyMsgBox("嘿，是否使用复制的邀请码加入房间？", "加入联机房间", "加入", "取消") = 2 Then Return
            RunInUi(
            Sub()
                PageLinkMain.Join(Code)
                ClipboardSet(Nothing, False)
            End Sub)
        End Sub)
    End Sub

    Private Sub HandleDrag(sender As Object, e As DragEventArgs)
        Try
            If e.Handled AndAlso (e.Effects <> DragDropEffects.None) Then Return
            e.Handled = True

            Static PrevData As IDataObject, PrevEffects As DragDropEffects
            If e.Data Is PrevData Then
                e.Effects = PrevEffects
                Return
            End If

            e.Effects = DragDropEffects.None
            If e.Data.GetDataPresent(DataFormats.Text) Then
                Dim Str As String = e.Data.GetData(DataFormats.Text)
                If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then
                    e.Effects = DragDropEffects.Copy
                ElseIf Str.StartsWithF("file:///") Then
                    e.Effects = DragDropEffects.Copy
                End If
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim Files As String() = e.Data.GetData(DataFormats.FileDrop)
                If Files IsNot Nothing AndAlso Files.Length > 0 Then
                    e.Effects = DragDropEffects.Link
                End If
            End If
            PrevData = e.Data
            PrevEffects = e.Effects
            Logger.Info($"设置拖放类型：{e.Effects}")
        Catch ex As Exception
            Logger.Error(ex, "处理拖放时出错")
        End Try
    End Sub
    Private Sub FrmMain_Drop(sender As Object, e As DragEventArgs) Handles Me.Drop
        Try
            ShowWindowToTop()
            If e.Data.GetDataPresent(DataFormats.Text) Then

                Try
                    Dim Str As String = e.Data.GetData(DataFormats.Text)
                    Logger.Info($"接受文本拖拽：{Str}")
                    If Str.StartsWithF("authlib-injector:yggdrasil-server:") Then

                        e.Handled = True
                        e.Effects = DragDropEffects.Copy
                        Dim AuthlibServer As String = StringUtils.FormUrlUnescape(Str.Substring("authlib-injector:yggdrasil-server:".Length))
                        Logger.Info($"Authlib 拖拽：{AuthlibServer}")
                        If Not String.IsNullOrEmpty(New ValidateHttp().Validate(AuthlibServer)) Then
                            Hint($"输入的 Authlib 验证服务器不符合网址格式（{AuthlibServer}）！", HintType.Red)
                            Return
                        End If
                        Dim Target = If(PageCurrent = PageType.InstanceSetup, PageInstanceLeft.Instance, McInstanceSelected)
                        If Target Is Nothing Then
                            Hint("请先下载游戏，再设置第三方登录！", HintType.Red)
                            Return
                        End If
                        If AuthlibServer = "https://littleskin.cn/api/yggdrasil" Then

                            If MyMsgBox($"是否要在版本 {Target.Name} 中开启 LittleSkin 登录？" & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Return
                            End If
                            Settings.Set("VersionServerLogin", 4, Instance:=Target)
                            Settings.Set("VersionServerAuthServer", "https://littleskin.cn/api/yggdrasil", Instance:=Target)
                            Settings.Set("VersionServerAuthRegister", "https://littleskin.cn/auth/register", Instance:=Target)
                            Settings.Set("VersionServerAuthName", "LittleSkin 登录", Instance:=Target)
                        Else

                            If MyMsgBox($"是否要在版本 {Target.Name} 中开启第三方登录？" & vbCrLf &
                                        $"登录服务器：{AuthlibServer}" & vbCrLf & vbCrLf &
                                        "你可以在 版本设置 → 设置 → 服务器选项 中修改登录方式。", "第三方登录开启确认", "确定", "取消") = 2 Then
                                Return
                            End If
                            Settings.Set("VersionServerLogin", 4, Instance:=Target)
                            Settings.Set("VersionServerAuthServer", AuthlibServer, Instance:=Target)
                            Settings.Set("VersionServerAuthRegister", AuthlibServer.Replace("api/yggdrasil", "auth/register"), Instance:=Target)
                            Settings.Set("VersionServerAuthName", "", Instance:=Target)
                        End If
                        If PageCurrent = PageType.InstanceSetup AndAlso PageCurrentSub = PageSubType.InstanceSetup Then

                            FrmInstanceSetup.Reload()
                        ElseIf PageCurrent = PageType.Launch Then

                            FrmLaunchLeft.RefreshPage(True, False)
                        End If
                    ElseIf Str.StartsWithF("file:///") Then

                        Dim FilePath = Net.WebUtility.UrlDecode(Str).Substring("file:///".Length).Replace("/", "\")
                        e.Handled = True
                        e.Effects = DragDropEffects.Copy
                        FileDrag(New List(Of String) From {FilePath})
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "无法接取文本拖拽事件")
                    Return
                End Try
            ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then

                Dim FilePathRaw = e.Data.GetData(DataFormats.FileDrop)
                If FilePathRaw Is Nothing Then
                    Hint("请将文件解压后再拖入！", HintType.Red)
                    Return
                End If
                e.Handled = True
                e.Effects = DragDropEffects.Link
                FileDrag(CType(FilePathRaw, IEnumerable(Of String)))
            End If
        Catch ex As Exception
            Logger.Error(ex, "接取拖拽事件失败")
        End Try
    End Sub
    Private Sub FileDrag(FilePathList As IEnumerable(Of String))
        RunInNewThread(
        Sub()
            Dim FilePath As String = FilePathList.First
            Logger.Warn($"接受文件拖拽：{FilePath}{If(FilePathList.Any, $" 等 {FilePathList.Count} 个文件", "")}")

            If DirectoryUtils.Exists(FilePathList.First) AndAlso Not FileUtils.Exists(FilePathList.First) Then
                Hint("请拖入一个文件，而非文件夹！", HintType.Red)
                Return
            ElseIf Not FileUtils.Exists(FilePathList.First) Then
                Hint("拖入的文件不存在：" & FilePathList.First, HintType.Red)
                Return
            End If

            Dim PathList As List(Of String) = FilePathList.ToList()
            If FilePathList.Count > 1 Then

                For Each File In PathList
                    If Not {"jar", "litemod", "disabled", "old"}.Contains(PathUtils.GetExtension(File)) Then
                        Hint("一次请只拖入一个文件！", HintType.Red)
                        Return
                    End If
                Next
            End If

            Dim Extension As String = PathUtils.GetExtension(FilePath)
            If Extension = "xaml" Then
                Logger.Info("文件后缀为 XAML，作为主页加载")
                If FileUtils.Exists(Paths.Base & "PCL\Custom.xaml") Then
                    If MyMsgBox("已存在一个主页文件，是否要将它覆盖？", "覆盖确认", "覆盖", "取消") = 2 Then
                        Return
                    End If
                End If
                FileUtils.Copy(FilePath, Paths.Base & "PCL\Custom.xaml")
                RunInUi(
                Sub()
                    Settings.Set("UiCustomType", 1)
                    FrmLaunchRight.ForceRefresh()
                    Hint("已加载主页自定义文件！", HintType.Green)
                End Sub)
                Return
            End If

            If PageInstanceMod.InstallMods(PathList) Then Return

            If {"zip", "rar", "mrpack"}.Any(Function(t) t = Extension) Then
                Hint("正在读取压缩包……")
                Try
                    Logger.Info("文件为压缩包，尝试作为整合包安装")
                    ModpackInstall(FilePath)
                    RunInUi(Sub() FrmMain.PageChange(FormMain.PageType.TaskManager))
                    Return
                Catch ex As Exception
                    If RestrictMcDownload AndAlso ex.Message = "为遵守相关法律法规，PCL 不再提供 MC 下载功能，非常抱歉 qwq……" Then
                        Hint(ex.Message, HintType.Red)
                        Return
                    End If
                    If ex.IsCanceled Then Return

                End Try
            End If

            If Extension = "rar" Then
                Hint("PCL 无法处理 rar 格式的压缩包，请在解压后重新压缩为 zip 格式再试！", HintType.Red)
                Return
            End If

            Try
                Logger.Info("尝试进行错误报告分析")
                Dim Analyzer As New CrashAnalyzer
                Analyzer.Import(FilePath)
                If Not Analyzer.Prepare() Then Exit Try
                Analyzer.Analyze()
                Analyzer.Output(True, New List(Of String))
                Return
            Catch ex As Exception
                Logger.Error(ex, "自主错误报告分析失败")
            End Try

            Hint("PCL 无法确定适合的操作……", HintType.Red)
        End Sub, "文件拖拽")
    End Sub

    Public IsSystemTimeChanged As Boolean = False
    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = 30 Then
            Dim NowDate = Date.Now
            If NowDate.Date = ApplicationOpenTime.Date Then
                Logger.Info($"系统时间微调为：{NowDate.ToLongDateString} {NowDate.ToLongTimeString}")
                IsSystemTimeChanged = False
            Else
                Logger.Info($"系统时间修改为：{NowDate.ToLongDateString} {NowDate.ToLongTimeString}")
                IsSystemTimeChanged = True
            End If
        ElseIf msg = 400 * 16 + 2 Then
            Logger.Info($"收到置顶信息：{hwnd.ToInt64}")
            If Not IsWindowLoadFinished Then
                Logger.Info("窗口尚未加载完成，忽略置顶请求")
                Return IntPtr.Zero
            End If
            ShowWindowToTop()
            handled = True
        End If
        Return IntPtr.Zero
    End Function

    Private _Hidden As Boolean = False
    Public Property Hidden As Boolean
        Get
            Return _Hidden
        End Get
        Set(value As Boolean)
            If _Hidden = value Then Return
            _Hidden = value
            If value Then

                Left -= 10000
                ShowInTaskbar = False
                Visibility = Visibility.Hidden
                Logger.Info($"窗口已隐藏，位置：({Left},{Top})")
            Else

                If Left < -2000 Then Left += 10000
                ShowWindowToTop()
            End If
        End Set
    End Property

    Public Sub ShowWindowToTop()
        RunInUi(
        Sub()

            Visibility = Visibility.Visible
            ShowInTaskbar = True
            WindowState = WindowState.Normal
            Hidden = False
            Topmost = True
            Topmost = Settings.Get(Of Boolean)("UiWindowTopmost")
            SetForegroundWindow(Handle)
            Focus()
            Logger.Info($"窗口已置顶，位置：({Left}, {Top}), {Width} x {Height}")
        End Sub)
    End Sub

#End Region

#Region "切换页面"

    Public Enum PageType

        Launch = 0

        Download = 1

        Link = 2

        Setup = 3

        Other = 4

        InstanceSelect = 5

        TaskManager = 6

        InstanceSetup = 7

        ResourceDetail = 8

        HelpDetail = 9
    End Enum

    Public Enum PageSubType
        [Default] = 0
        DownloadInstall = 0
        DownloadMod = 2
        DownloadPack = 3
        DownloadDataPack = 4
        DownloadResourcePack = 5
        DownloadShader = 6
        DownloadMap = 7
        SetupLaunch = 0
        SetupLink = 1
        SetupUI = 2
        SetupSystem = 3
        LinkMain = 0
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
        OtherFeedback = 3
        InstanceOverall = 0
        InstanceSetup = 1
        InstanceModify = 2
        InstanceMod = 3
        InstanceModDisabled = 4
        InstanceExport = 5
    End Enum

    Private Function PageNameGet(Stack As PageStackData) As String
        Select Case Stack.Page
            Case PageType.InstanceSelect
                Return "版本选择"
            Case PageType.TaskManager
                Return "任务管理"
            Case PageType.InstanceSetup
                Return "版本设置 - " & If(PageInstanceLeft.Instance Is Nothing, "未知版本", PageInstanceLeft.Instance.Name)
            Case PageType.ResourceDetail
                Return CType(Stack.Additional(0), ResourceProject).TranslatedName
            Case Else
                Return ""
        End Select
    End Function

    Public Sub PageNameRefresh(Type As PageStackData)
        LabTitleInner.Text = PageNameGet(Type)
    End Sub

    Public Sub PageNameRefresh()
        PageNameRefresh(PageCurrent)
    End Sub

    Public PageCurrent As PageStackData = PageType.Launch

    Public PageLast As PageStackData = PageType.Launch

    Public ReadOnly Property PageCurrentSub As PageSubType
        Get
            Select Case PageCurrent
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    Return FrmDownloadLeft.PageID
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    Return FrmSetupLeft.PageID
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    Return FrmOtherLeft.PageID
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    Return FrmInstanceLeft.PageID
                Case Else
                    Return 0
            End Select
        End Get
    End Property

    Public PageStack As New List(Of PageStackData)
    Public Class PageStackData

        Public Page As PageType
        Public Additional As Object

        Public Overrides Function Equals(other As Object) As Boolean
            If other Is Nothing Then Return False
            If TypeOf other Is PageStackData Then
                Dim PageOther As PageStackData = other
                If Page <> PageOther.Page Then Return False
                If Additional Is Nothing Then
                    Return PageOther.Additional Is Nothing
                Else
                    Return PageOther.Additional IsNot Nothing AndAlso Additional.Equals(PageOther.Additional)
                End If
            ElseIf TypeOf other Is Integer Then
                If Page <> other Then Return False
                Return Additional Is Nothing
            Else
                Return False
            End If
        End Function
        Public Shared Operator =(left As PageStackData, right As PageStackData) As Boolean
            Return EqualityComparer(Of PageStackData).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As PageStackData, right As PageStackData) As Boolean
            Return Not left = right
        End Operator
        Public Shared Widening Operator CType(Value As PageType) As PageStackData
            Return New PageStackData With {.Page = Value}
        End Operator
        Public Shared Widening Operator CType(Value As PageStackData) As PageType
            Return Value.Page
        End Operator
    End Class
    Public PageLeft As MyPageLeft, PageRight As MyPageRight

    Private IsChangingPage As Boolean = False

    Public Sub PageChange(Stack As PageStackData, Optional SubType As PageSubType = PageSubType.Default)
        If PageNameGet(Stack) = "" Then

            PageChangeExit()
            IsChangingPage = True
            CType(PanTitleSelect.Children(Stack), MyRadioButton).SetChecked(True, True, PageNameGet(PageCurrent) = "")
            IsChangingPage = False
            Select Case Stack.Page
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
                    CType(FrmDownloadLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    CType(FrmSetupLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft

                    For Each Child As FrameworkElement In FrmOtherLeft.PanItem.Children
                        If Val(Child.Tag) = SubType Then
                            CType(Child, MyListItem).SetChecked(True, True, Stack = PageCurrent)
                            Exit For
                        End If
                    Next
            End Select
            PageChangeActual(Stack, SubType)
        Else

            Select Case Stack.Page
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    CType(FrmInstanceLeft.PanItem.Children(SubType), MyListItem).SetChecked(True, True, Stack = PageCurrent)
            End Select
            PageChangeActual(Stack, SubType)
        End If
    End Sub

    Private Sub BtnTitleSelect_Click(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check, BtnTitleSelect2.Check, BtnTitleSelect3.Check, BtnTitleSelect4.Check
        If IsChangingPage Then Return
        PageChangeActual(Val(sender.Tag))
    End Sub

    Public Sub PageBack() Handles BtnTitleInner.Click
        If PageStack.Any() Then
            PageChangeActual(PageStack(0))
        Else
            PageChange(PageType.Launch)
        End If
    End Sub

    Private Sub PageChangeActual(Stack As PageStackData, Optional SubType As PageSubType = -1)
        If PageCurrent = Stack AndAlso (PageCurrentSub = SubType OrElse SubType = -1) Then Return
        If Stack.Page = PageType.InstanceSetup AndAlso PageInstanceLeft.Instance IsNot Nothing AndAlso McInstanceLock.IsLocked(PageInstanceLeft.Instance) Then
            Hint("该版本正在被修改，暂时无法进入版本设置！", HintType.Red)
            Return
        End If
        AniControlEnabled += 1
        Try

#Region "子页面处理"
            Dim PageName As String = PageNameGet(Stack)
            If PageName = "" Then

                PageChangeExit()
            Else

                If PageStack.Any Then

                    AniStart({
                        AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                        AaCode(Sub() LabTitleInner.Text = PageName,, True),
                        AaOpacity(LabTitleInner, 1, 150, 30)
                    }, "FrmMain Titlebar SubLayer")
                    If PageStack.Contains(Stack) Then

                        Do While PageStack.Contains(Stack)
                            PageStack.RemoveAt(0)
                        Loop
                    Else

                        PageStack.Insert(0, PageCurrent)
                    End If
                Else

                    PanTitleInner.Visibility = Visibility.Visible
                    PanTitleMain.IsHitTestVisible = False
                    PanTitleInner.IsHitTestVisible = True
                    PageNameRefresh(Stack)
                    AniStart({
                        AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                        AaX(PanTitleMain, 12 - PanTitleMain.Margin.Left, 150,, New AniEaseInFluent(AniEasePower.Weak)),
                        AaOpacity(PanTitleInner, 1 - PanTitleInner.Opacity, 150, 200),
                        AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200, New AniEaseOutBack),
                        AaCode(Sub() PanTitleMain.Visibility = Visibility.Collapsed,, True)
                    }, "FrmMain Titlebar FirstLayer")
                    PageStack.Insert(0, PageCurrent)
                End If
            End If
#End Region

#Region "实际更改页面框架 UI"
            PageLast = PageCurrent
            PageCurrent = Stack
            Select Case Stack.Page
                Case PageType.Launch
                    PageChangeAnim(FrmLaunchLeft, FrmLaunchRight)
                Case PageType.Download
                    If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft

                    PageChangeAnim(FrmDownloadLeft, FrmDownloadLeft.PageGet(SubType))
                Case PageType.Link
                    If FrmLinkMain Is Nothing Then FrmLinkMain = New PageLinkMain
                    PageChangeAnim(New MyPageLeft, FrmLinkMain)
                Case PageType.Setup
                    If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
                    PageChangeAnim(FrmSetupLeft, FrmSetupLeft.PageGet(SubType))
                Case PageType.Other
                    If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
                    PageChangeAnim(FrmOtherLeft, FrmOtherLeft.PageGet(SubType))
                Case PageType.InstanceSelect
                    If FrmSelectLeft Is Nothing Then FrmSelectLeft = New PageSelectLeft
                    If FrmSelectRight Is Nothing Then FrmSelectRight = New PageSelectRight
                    PageChangeAnim(FrmSelectLeft, FrmSelectRight)
                Case PageType.TaskManager
                    If FrmSpeedLeft Is Nothing Then FrmSpeedLeft = New PageSpeedLeft
                    If FrmSpeedRight Is Nothing Then FrmSpeedRight = New PageSpeedRight
                    PageChangeAnim(FrmSpeedLeft, FrmSpeedRight)
                Case PageType.InstanceSetup
                    If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
                    PageChangeAnim(FrmInstanceLeft, FrmInstanceLeft.PageGet(SubType))
                Case PageType.ResourceDetail
                    If FrmDownloadResourceDetail Is Nothing Then FrmDownloadResourceDetail = New PageDownloadResourceDetail
                    PageChangeAnim(New MyPageLeft, FrmDownloadResourceDetail)
                Case PageType.HelpDetail
                    PageChangeAnim(New MyPageLeft, Stack.Additional(1))
            End Select
#End Region

#Region "设置为最新状态"
            BtnExtraDownload.ShowRefresh()
            BtnExtraApril.ShowRefresh()
#End Region

            Logger.Info($"切换主要页面：{Stack.Page}, {SubType}")
        Catch ex As Exception
            Logger.Error(ex, $"切换主要页面失败（ID {PageCurrent.Page}）")
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub

    Private _GlassPaused As Boolean = False

    Private Sub PauseGlassForAnimation()
        If _GlassPaused Then Return
        _GlassPaused = True
        Try
            ModGlass.SuspendRefresh()
        Catch ex As Exception
            Logger.Warn(ex, "挂起毛玻璃失败")
            _GlassPaused = False
            Return
        End Try
        RunInNewThread(
        Sub()
            Thread.Sleep(450)
            RunInUi(
            Sub()
                _GlassPaused = False
                Try
                    ModGlass.ResumeRefresh()
                Catch ex As Exception
                    Logger.Warn(ex, "恢复毛玻璃失败")
                End Try
            End Sub)
        End Sub, "毛玻璃暂停恢复")
    End Sub

    Private Sub PageChangeAnim(TargetLeft As FrameworkElement, TargetRight As FrameworkElement)
        PauseGlassForAnimation()
        AniStop("FrmMain LeftChange")
        AniStop("PageLeft PageChange")
        AniControlEnabled += 1

        If TargetLeft.Parent IsNot Nothing Then TargetLeft.SetValue(ContentPresenter.ContentProperty, Nothing)
        If TargetRight IsNot Nothing AndAlso TargetRight.Parent IsNot Nothing Then TargetRight.SetValue(ContentPresenter.ContentProperty, Nothing)
        PageLeft = TargetLeft
        PageRight = TargetRight

        CType(PanMainLeft.Child, MyPageLeft).TriggerHideAnimation()
        CType(PanMainRight.Child, MyPageRight).PageOnExit()
        AniControlEnabled -= 1

        AniStart({
            AaCode(
            Sub()
                AniControlEnabled += 1

                PanMainLeft.Child = PageLeft
                PageLeft.Opacity = 0
                PanMainLeft.Background = Nothing
                AniControlEnabled -= 1
                RunInUi(Sub() PanMainLeft_Resize(PanMainLeft.ActualWidth), True)
            End Sub, 110),
            AaCode(
            Sub()

                PageLeft.Opacity = 1
                PageLeft.TriggerShowAnimation()
            End Sub, 30, True)
        }, "FrmMain PageChangeLeft")
        AniStart({
            AaCode(
            Sub()
                AniControlEnabled += 1
                CType(PanMainRight.Child, MyPageRight).PageOnForceExit()

                PanMainRight.Child = PageRight
                PageRight.Opacity = 0
                PanMainRight.Background = Nothing
                AniControlEnabled -= 1
                RunInUi(Sub() BtnExtraBack.ShowRefresh(), True)
            End Sub, 110),
            AaCode(
            Sub()

                PageRight.Opacity = 1
                PageRight.PageOnEnter()
            End Sub, 30, True)
        }, "FrmMain PageChangeRight")
    End Sub

    Private Sub PageChangeExit()
        If PageStack.Any Then

            PanTitleMain.Visibility = Visibility.Visible
            PanTitleMain.IsHitTestVisible = True
            PanTitleInner.IsHitTestVisible = False
            AniStart({
                AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,, New AniEaseInFluent),
                AaOpacity(PanTitleMain, 1 - PanTitleMain.Opacity, 150, 200),
                AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200, New AniEaseOutBack(AniEasePower.Weak)),
                AaCode(Sub() PanTitleInner.Visibility = Visibility.Collapsed,, True)
            }, "FrmMain Titlebar FirstLayer")
            PageStack.Clear()
        Else

        End If
    End Sub

    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If Not e.WidthChanged Then Return
        PanMainLeft_Resize(e.NewSize.Width)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Dim Delta As Double = NewWidth - RectLeftBackground.Width
        If Math.Abs(Delta) > 0.1 AndAlso AniControlEnabled = 0 Then
            If PanMain.Opacity < 0.1 Then PanMainLeft.IsHitTestVisible = False
            PauseGlassForAnimation()
            If NewWidth > 0 Then

                AniStart({
                    AaWidth(RectLeftBackground, NewWidth - RectLeftBackground.Width, 180,, New AniEaseOutFluent(AniEasePower.ExtraStrong)),
                    AaOpacity(RectLeftShadow, 1 - RectLeftShadow.Opacity, 180),
                    AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 150)
                }, "FrmMain LeftChange", True)
            Else

                AniStart({
                    AaWidth(RectLeftBackground, -RectLeftBackground.Width, 180,, New AniEaseOutFluent),
                    AaOpacity(RectLeftShadow, -RectLeftShadow.Opacity, 180),
                    AaCode(Sub() PanMainLeft.IsHitTestVisible = True, 150)
                }, "FrmMain LeftChange", True)
            End If
        Else
            RectLeftBackground.Width = NewWidth
            PanMainLeft.IsHitTestVisible = True
            AniStop("FrmMain LeftChange")
        End If
    End Sub

#End Region

#Region "控件拖动"

    Public Sub DragTick()
        If DragControl Is Nothing Then Return
        If Not Mouse.LeftButton = MouseButtonState.Pressed Then
            DragStop()
        End If
    End Sub

    Public Sub DragDoing() Handles PanBack.MouseMove
        If DragControl Is Nothing Then Return
        If Mouse.LeftButton = MouseButtonState.Pressed Then
            DragControl.DragDoing()
        Else
            DragStop()
        End If
    End Sub
    Public Sub DragStop()

        RunInUi(Sub()
                    If DragControl Is Nothing Then Return
                    Dim Control = DragControl
                    DragControl = Nothing
                    Control.DragStop()
                End Sub)
    End Sub

#End Region

#Region "附加按钮"

    Private Sub BtnExtraMusic_Click(sender As Object, e As EventArgs) Handles BtnExtraMusic.Click
        MusicControlPause()
    End Sub
    Private Sub BtnExtraMusic_RightClick(sender As Object, e As EventArgs) Handles BtnExtraMusic.RightClick
        MusicControlNext()
    End Sub

    Private Sub BtnExtraDownload_Click(sender As Object, e As EventArgs) Handles BtnExtraDownload.Click
        PageChange(PageType.TaskManager)
    End Sub
    Private Function BtnExtraDownload_ShowCheck() As Boolean
        Return HasDownloadingTask() AndAlso Not PageCurrent = PageType.TaskManager
    End Function

    Public Sub AprilGiveup() Handles BtnExtraApril.Click
        If IsAprilEnabled AndAlso Not IsAprilGiveup Then
            Hint("=D", HintType.Green)
            IsAprilGiveup = True
            FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
            FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
            BtnExtraApril.ShowRefresh()
        End If
    End Sub
    Public Function BtnExtraApril_ShowCheck() As Boolean
        Return IsAprilEnabled AndAlso Not IsAprilGiveup AndAlso PageCurrent = PageType.Launch
    End Function

    Public Sub BtnExtraShutdown_Click() Handles BtnExtraShutdown.Click
        Try
            If McLaunchLoaderReal IsNot Nothing Then McLaunchLoaderReal.Cancel()
            For Each Watcher In McWatcherList
                Watcher.Kill()
            Next
            Hint("已关闭运行中的 Minecraft！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "强制关闭所有 Minecraft 失败")
        End Try
    End Sub
    Public Function BtnExtraShutdown_ShowCheck() As Boolean
        Return HasRunningMinecraft
    End Function

    Public Sub BackToTop() Handles BtnExtraBack.Click
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        If RealScroll IsNot Nothing Then
            RealScroll.PerformVerticalOffsetDelta(-RealScroll.VerticalOffset)
        Else
            Logger.Error("无法返回顶部，未找到合适的 RealScroll", LogBehavior.Toast)
        End If
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim RealScroll As MyScrollViewer = BtnExtraBack_GetRealChild()
        Return RealScroll IsNot Nothing AndAlso RealScroll.Visibility = Visibility.Visible AndAlso RealScroll.VerticalOffset > Height + If(BtnExtraBack.Show, 0, 700)
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        If PanMainRight.Child Is Nothing OrElse TypeOf PanMainRight.Child IsNot MyPageRight Then Return Nothing
        Dim Page As MyPageRight = PanMainRight.Child
        Return Page.FindName(Page.PanScroll)
    End Function

#End Region

    Public lastMouseArg As MouseEventArgs = Nothing
    Private Sub FormMain_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        lastMouseArg = e
    End Sub

#Region "视频背景"

    Public ReadOnly Property VideoBackgroundFolder As String
        Get
            Return Paths.Base & "PCL\VideoBackground\"
        End Get
    End Property

    Private _videoSrcPath As String = ""
    Private _videoReady As Boolean = False
    Private _videoPaused As Boolean = False
    Private _videoFitRatio As Double = 1
    Private _videoImgHideTimer As System.Windows.Threading.DispatcherTimer
    Private _videoConvertTried As Boolean = False

    Public ReadOnly Property IsVideoBackgroundActive As Boolean
        Get
            Try
                Return _videoSrcPath <> "" AndAlso _videoReady AndAlso VideoBack IsNot Nothing AndAlso
                       VideoBack.Visibility = Visibility.Visible AndAlso VideoBack.Opacity > 0.5
            Catch
                Return False
            End Try
        End Get
    End Property

    Public Shared Sub VideoBackgroundRefresh(Value As Object)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        RunInUi(Sub() FrmMain.VideoBackgroundUpdate())
    End Sub

    Public Shared Function VideoBackgroundApply(SourcePath As String) As Boolean
        Try
            If String.IsNullOrEmpty(SourcePath) OrElse Not File.Exists(SourcePath) Then Return False
            DirectoryUtils.Create(FrmMain.VideoBackgroundFolder)
            Dim Target As String = FrmMain.VideoBackgroundFolder & New FileInfo(SourcePath).Name
            If Not String.Equals(SourcePath, Target, StringComparison.OrdinalIgnoreCase) Then
                If File.Exists(Target) Then FileUtils.Delete(Target)
                FileUtils.Copy(SourcePath, Target)
            End If
            Settings.Set("UiVideoBackground", Target)
            FrmMain._videoConvertTried = False
            Return True
        Catch ex As Exception
            Logger.Error(ex, "应用视频背景失败")
            Hint("应用视频背景失败，详情请查看日志。", HintType.Red)
            Return False
        End Try
    End Function

    Public Shared Sub VideoBackgroundReset()
        Settings.Set("UiVideoBackground", "")
    End Sub

    Public Sub VideoBackgroundUpdate()
        Try
            Dim Path As String = Settings.Get(Of String)("UiVideoBackground")
            If Not String.IsNullOrEmpty(Path) AndAlso File.Exists(Path) Then
                If ModMedia.IsDirectVideo(Path) Then
                    VideoBackgroundPlay(Path)
                Else
                    VideoBackgroundPrepare(Path, False)
                End If
            Else
                VideoBackgroundClear()
            End If
        Catch ex As Exception
            Logger.Error(ex, "视频背景加载失败")
            VideoBackgroundClear()
        End Try
    End Sub

    Private Sub VideoBackgroundPrepare(SourcePath As String, ForceConvert As Boolean)
        Logger.Info($"视频背景格式不受直接支持，准备转换：{SourcePath}")
        RunInNewThread(
            Sub()
                Try
                    Dim Ready As String = ModMedia.PrepareVideoForPlayback(SourcePath, ForceConvert)
                    RunInUi(Sub()
                                If Ready Is Nothing Then
                                    Hint("该视频无法播放，转换也没有成功。可换用 mp4（H.264）格式的视频，或在设置中重新选择以再次尝试。", HintType.Red)
                                Else
                                    VideoBackgroundPlay(Ready)
                                End If
                            End Sub)
                Catch ex As Exception
                    Logger.Error(ex, "视频背景转换失败")
                    RunInUi(Sub() Hint("视频背景转换失败，详情请查看日志。", HintType.Red))
                End Try
            End Sub, "VideoBackgroundConvert")
    End Sub

    Private Sub VideoBackgroundPlay(Path As String)
        Try
            If _videoSrcPath = Path AndAlso _videoReady Then
                _videoPaused = False
                VideoBack.Play()

                VideoBack.Visibility = Visibility.Visible
                VideoBack.Opacity = 1
                VideoHideImgBackLater()
                Return
            End If
            _videoSrcPath = Path
            _videoPaused = False
            _videoReady = False
            VideoBack.Stop()
            VideoBack.Opacity = 0
            VideoBack.ClearValue(FrameworkElement.WidthProperty)
            VideoBack.ClearValue(FrameworkElement.HeightProperty)
            VideoBack.RenderTransform = Nothing
            VideoBack.Visibility = Visibility.Visible
            VideoBack.Source = New Uri(Path)
            VideoBack.Position = TimeSpan.Zero
            VideoBack.Play()
        Catch ex As Exception
            Logger.Error(ex, "视频背景播放失败")
            VideoBackgroundClear()
        End Try
    End Sub

    Private Sub VideoBackgroundClear()
        Try
            _videoSrcPath = ""
            _videoReady = False
            _videoPaused = False
            _videoFitRatio = 1
            If _videoImgHideTimer IsNot Nothing Then _videoImgHideTimer.Stop()
            VideoBack.Stop()
            VideoBack.Source = Nothing
            VideoBack.Position = TimeSpan.Zero
            VideoBack.Opacity = 0
            VideoBack.Visibility = Visibility.Collapsed

            VideoBack.ClearValue(FrameworkElement.WidthProperty)
            VideoBack.ClearValue(FrameworkElement.HeightProperty)
            VideoBack.RenderTransform = Nothing
            If ImgBack.Background IsNot Nothing Then ImgBack.Visibility = Visibility.Visible

            ModGlass.GlassInvalidate()
            RefreshUiStyle()
            Logger.Info("视频背景已关闭，恢复静态背景")
        Catch ex As Exception
            Logger.Warn(ex, "关闭视频背景失败")
        End Try
    End Sub

    Private Sub VideoBackFitLayout()
        If VideoBack Is Nothing OrElse _videoSrcPath = "" Then Return
        Dim Host As Grid = TryCast(VideoBack.Parent, Grid)
        If Host Is Nothing OrElse Host.ActualWidth <= 0 OrElse Host.ActualHeight <= 0 Then Return
        VideoBack.ClearValue(FrameworkElement.WidthProperty)
        VideoBack.ClearValue(FrameworkElement.HeightProperty)
        VideoBack.RenderTransform = Nothing
        VideoBack.HorizontalAlignment = HorizontalAlignment.Stretch
        VideoBack.VerticalAlignment = VerticalAlignment.Stretch
        If Math.Abs(_videoFitRatio - 1) > 0.001 Then
            _videoFitRatio = 1
            Logger.Info($"视频背景按内容区铺满渲染（原始 {VideoBack.NaturalVideoWidth}x{VideoBack.NaturalVideoHeight}，容器 {Math.Round(Host.ActualWidth)}x{Math.Round(Host.ActualHeight)} DIP）")
        End If
    End Sub

    Private Sub VideoBack_MediaOpened(sender As Object, e As RoutedEventArgs) Handles VideoBack.MediaOpened
        Try
            If _videoSrcPath = "" Then Return
            _videoReady = True
            VideoBack.Volume = 0
            VideoBack.Visibility = Visibility.Visible
            VideoBack.Opacity = 1
            If Not _videoPaused Then VideoBack.Play()
            Logger.Info($"视频背景已就绪：{VideoBack.NaturalVideoWidth}x{VideoBack.NaturalVideoHeight}")
            VideoBackFitLayout()
            VideoHideImgBackLater()
        Catch ex As Exception
            Logger.Warn(ex, "视频背景显示失败")
        End Try
    End Sub

    Private Sub VideoHideImgBackLater()
        If _videoImgHideTimer Is Nothing Then
            _videoImgHideTimer = New System.Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(220)}
            AddHandler _videoImgHideTimer.Tick,
                Sub(s, e)
                    _videoImgHideTimer.Stop()
                    If _videoReady AndAlso VideoBack.Opacity > 0.99 Then
                        ImgBack.Visibility = Visibility.Collapsed

                        ModGlass.GlassInvalidate()
                        RefreshUiStyle()
                    End If
                End Sub
        End If
        _videoImgHideTimer.Stop()
        _videoImgHideTimer.Start()
    End Sub

    Private Sub VideoBack_MediaEnded(sender As Object, e As RoutedEventArgs) Handles VideoBack.MediaEnded
        Try
            If _videoSrcPath = "" OrElse _videoPaused Then Return
            VideoBack.Position = TimeSpan.Zero
            VideoBack.Play()
        Catch ex As Exception
            Logger.Warn(ex, "视频背景循环播放失败")
        End Try
    End Sub

    Private Sub VideoBack_MediaFailed(sender As Object, e As ExceptionRoutedEventArgs) Handles VideoBack.MediaFailed
        Logger.Warn("视频背景播放失败：" & If(e Is Nothing OrElse e.ErrorException Is Nothing, "未知错误", e.ErrorException.Message))
        Dim SourcePath As String = Settings.Get(Of String)("UiVideoBackground")
        If Not _videoConvertTried AndAlso Not String.IsNullOrEmpty(SourcePath) AndAlso File.Exists(SourcePath) Then
            _videoConvertTried = True
            VideoBackgroundClear()
            VideoBackgroundPrepare(SourcePath, True)
            Return
        End If
        VideoBackgroundClear()
        Settings.Set("UiVideoBackground", "")
        Hint("该视频无法播放，已切回静态壁纸。请换用 mp4（H.264）格式的视频。", HintType.Red)
    End Sub

    Private Sub FormMain_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged
        Try
            If WindowState = WindowState.Minimized Then
                _videoPaused = True
                VideoBack.Pause()
            ElseIf _videoSrcPath <> "" Then
                _videoPaused = False
                If _videoReady Then VideoBack.Play()
            End If
        Catch
        End Try
    End Sub

#End Region

    Public Shared Sub UiStyleRefresh(Optional NewValue As Object = Nothing)
        If FrmMain Is Nothing Then Return
        FrmMain.RefreshUiStyle()

        If FrmSetupUI IsNot Nothing Then FrmSetupUI.RefreshGlassSettings()
    End Sub

    Public Shared Sub GlassStyleRefresh(Optional NewValue As Object = Nothing)
        If FrmMain Is Nothing Then Return
        FrmMain.RefreshUiStyle()
    End Sub

    Private Shared _FontApplied As String = ""
    Private Shared _FontDefault As System.Windows.Media.FontFamily = Nothing

    Public Shared Sub FontFamilyRefresh(Optional NewValue As Object = Nothing)
        Try
            If Application.Current Is Nothing Then Return
            If _FontDefault Is Nothing Then _FontDefault = TryCast(Application.Current.Resources("UiFontFamily"), System.Windows.Media.FontFamily)
            If _FontDefault Is Nothing Then Return
            Dim Name As String = If(Settings.Get(Of String)("UiFontFamily"), "").Trim
            If Name = _FontApplied Then Return

            Dim Value As System.Windows.Media.FontFamily
            If Name = "" Then
                Value = _FontDefault
            ElseIf Name.StartsWith("file:", StringComparison.OrdinalIgnoreCase) Then

                Dim SplitIndex As Integer = Name.IndexOf("#"c)
                Dim LocalPath As String = ""
                Try
                    LocalPath = New Uri(If(SplitIndex >= 0, Name.Substring(0, SplitIndex), Name)).LocalPath
                Catch ex As Exception
                End Try
                If LocalPath = "" OrElse Not File.Exists(LocalPath) Then
                    Logger.Warn($"导入的字体文件不存在，已恢复默认字体：{LocalPath}")
                    Settings.Set("UiFontFamily", "")
                    Return
                End If
                Value = New System.Windows.Media.FontFamily(Name)
            ElseIf _FontDefault.BaseUri IsNot Nothing Then
                Value = New System.Windows.Media.FontFamily(_FontDefault.BaseUri, $"Resources/#PCL English, {Name}, Microsoft YaHei UI")
            Else
                Value = New System.Windows.Media.FontFamily($"{Name}, Microsoft YaHei UI")
            End If
            Application.Current.Resources("UiFontFamily") = Value
            _FontApplied = Name
            Logger.Info($"界面字体已切换：{If(Name = "", "默认", Name)}")
        Catch ex As Exception
            Logger.Warn(ex, "切换界面字体失败")
        End Try
    End Sub
    Private _uiStyleKey As String = ""
    Public Const FontSizeMin As Integer = 10
    Public Const FontSizeMax As Integer = 18

    Public Shared Function GetFontSizeValue() As Integer
        Dim Size As Integer = 13
        Try
            Size = Settings.Get(Of Integer)("UiFontSize")
        Catch
        End Try
        Return Math.Max(FontSizeMin, Math.Min(FontSizeMax, Size))
    End Function

    Public Shared Sub FontColorRefresh(Optional NewValue As Object = Nothing)
        Try
            If Application.Current Is Nothing Then Return
            Dim Value As String = If(Settings.Get(Of String)("UiFontColor"), "").Trim
            Dim TextColor As MyColor = ModSecret.Color1
            If Value <> "" Then TextColor = New MyColor(Value)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(CType(TextColor, Color))
        Catch ex As Exception
            Logger.Warn(ex, "应用自定义字体颜色失败")
        End Try
    End Sub

    Public Shared Sub FontSizeRefresh(Optional NewValue As Object = Nothing)
        Try
            If Application.Current Is Nothing Then Return
            Application.Current.Resources("UiFontSize") = CDbl(GetFontSizeValue())
        Catch ex As Exception
            Logger.Warn(ex, "应用自定义字体大小失败")
        End Try
    End Sub

    Public Sub RefreshUiStyle(Optional Force As Boolean = False)

        Dim WallKey As String = "0"
        Try
            Dim WallBrush As ImageBrush = TryCast(ImgBack.Background, ImageBrush)
            If WallBrush IsNot Nothing AndAlso WallBrush.ImageSource IsNot Nothing Then WallKey = WallBrush.ImageSource.GetHashCode().ToString()
        Catch
        End Try
        Dim Key As String = $"{Settings.Get(Of Integer)("UiUiStyle")}|{ModGlass.GlassCornerRadius}|{ModGlass.GlassCardOpacity}|{ModGlass.GlassUiBlur}|{ModGlass.IsVideoBackdrop}|{ModGlass.SurfaceRevision}|{WallKey}"
        If Not Force AndAlso Key = _uiStyleKey Then Return
        _uiStyleKey = Key

        If Settings.Get(Of Integer)("UiUiStyle") = 1 Then
            ModGlass.GlassRefreshFill(RectLeftBackground)
        Else
            RectLeftBackground.SetResourceReference(Shapes.Rectangle.FillProperty, "ColorBrushBackgroundTransparentSidebar")
        End If

        MyCard.ApplyAllStyles()
        ModGlass.GlassRefreshDialogs()
    End Sub

End Class
