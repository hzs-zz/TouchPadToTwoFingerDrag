# 🖱️ TouchpadToMiddleClick | Windows Touchpad Productivity Savior

> Save your terrible Windows touchpad experience in 1 minute! Middle-click panning + forced scrolling let your Windows laptop handle CAD and design work too.

## Pain Points & Motivation

People have long suffered from Windows touchpads!  
Every time you open design software like **SolidWorks, AutoCAD, Rhino** on a business trip, in a meeting, or on a high-speed train and try to pan a model with the touchpad – sorry, they don't support modern high-precision touchpad gestures at all. Want to scroll through a list in an old industrial program? Sorry, they only recognize the physical scroll wheel of a traditional mouse.  
For us CAD designers, not having an external mouse is an absolute disaster.

To solve this pain point, this project was born. It not only perfectly simulates **middle‑mouse‑button drag** using two‑finger swipes on the touchpad, but also provides **forced native scroll wheel simulation** for ancient legacy software!

---

## Core Killer Features

### 3D Viewport Intelligent Middle‑Click Panning
- Seamlessly maps two‑finger swipes to “middle mouse button down + movement”.
- Perfectly solves the panning pain point in mainstream 3D software like SolidWorks and Rhino when no mouse is available.

### Forced Scrolling for Legacy Software
- Targets old industrial software (even old versions of File Explorer) that is incompatible with high‑precision touchpads by forcing native `WM_MOUSEWHEEL` physical scroll messages at the low level.
- Ancient software? We’ll make it scroll as smoothly as silk!

### Precise Viewport Whitelist (Target Class Lock)
- Say goodbye to mindless global takeover! Add the class name of the specific viewport (e.g. `AfxWnd140su`, `SysListView32`) to the whitelist.
- Cursor inside the 3D canvas: seamlessly takes over and pans the model; cursor moves to the adjacent feature tree or menu bar: automatically restores the native touchpad state! Precise recognition, zero false activations.


---

##  Quick Start

1. **Download & Run**:  
   Go to the [Releases](#) page, download the latest version, unzip and double-click to run.

2. **Add Configuration**:  
   - Open the settings center, select the “Middle‑Click Panning” or “Scroll Simulation” tab.  
   - Enter the process name of the target software (e.g. `SLDWORKS`, without `.exe`).  
   - Use a window inspection tool like `Spy++` to obtain the window class name of the target software’s **3D canvas area** or **list area** (e.g. `AfxWnd140su`) and add it to the list.

3. **One‑Click Activation**:  
   - Click the toggle switch (the UI turns blue when active!).  
   - You can also use the global hotkey `Alt + Shift + M` to toggle the global middle‑click takeover at any time.  
   - Move the cursor into the target area, place two fingers on the touchpad, and enjoy unprecedented smoothness!

4. **Configuration File & Saving**:  
   - A `TouchpadConfig.xml` file is provided in the release folder. I have included some presets that I use personally for your convenience.

---

## 🛠️ Tech Stack

- **Language / Framework**: C# / .NET 8.0, WPF + Wpf.Ui (Fluent Design)
- **Core Low‑Level Technology**: Windows `WH_MOUSE_LL` low‑level global mouse hook
- **Win32 API Integration**: `GetCursorPos`, `WindowFromPoint`, `GetClassName`, `mouse_event`
- **Data Persistence**: `XmlSerializer`

---

## ☕ About & Support

Honestly, this tool is not technologically advanced. During the process of battling low‑level bugs and refactoring the architecture, a fair amount of the code was brainstormed and written together with AI.  
But the reason I wanted to create this little utility was that I envied the MacBook’s touchpad experience but couldn’t afford one yet. If you find it truly helpful and it solves your pain points, and you’d like to buy me a coffee, I would be very grateful!  

Until then, I will continue to improve the terrible touchpad experience on Windows.

---

## Acknowledgements
Some of the underlying principles for unpacking touchpad messages were referenced from this project: ClementGre’s [ThreeFingerDragOnWindows](https://github.com/ClementGre/ThreeFingerDragOnWindows)

- **Author**: Zisheng (XiaoSheng)
- **GitHub**: [hzs-zz](https://github.com/hzs-zz)

*(If this project has helped you, a ⭐ Star is welcome! PRs and Issues are also welcome to improve it together!)*
# 🖱️ TouchpadToMiddleClick | Windows 触摸板生产力救星





> 1分钟拯救 Windows 稀烂的触摸板体验！中键平移 + 强制滚动，让你的 Windows 笔记本也能画图。

## 痛点与初衷

天下苦 Windows 触摸板久矣！
每次在出差、开会或者高铁上打开 **SolidWorks、AutoCAD、Rhino** 等设计软件，想用触摸板平移一下模型？对不起，它们根本不支持现代触摸板的高精度手势。你想翻一下老旧工业软件的列表？对不起，它们只认传统鼠标的物理滚轮。
对于画图狗来说，没带外接鼠标简直就是一场灾难。

为了解决这个痛点，本项目应运而生。它不仅能让触摸板双指滑动完美模拟**鼠标中键拖拽**，还能为上古老旧软件提供**强制原生滚轮模拟**！

---

## 核心杀手锏功能

### 3D 视口智能中键平移
- 将双指滑动无缝映射为“按下鼠标中键 + 移动”。
- 完美解决 SolidWorks、Rhino 等主流 3D 软件在没有鼠标时的平移痛点。

### 老古董软件强制滚动
- 针对不兼容高精度触摸板的老旧工业软件（甚至是旧版资源管理器），强制在底层发送原生 `WM_MOUSEWHEEL` 物理滚轮信号。
- 老古董？一样让它像德芙一样滚起来！

### 精准视口白名单 (Target Class Lock)
- 告别无脑的全局接管！将具体视口的类名（如 `AfxWnd140su`、`SysListView32`）加入白名单。
- 光标在 3D 画布内：丝滑接管并平移模型；光标移到旁边的特征树或菜单栏：自动恢复为原生触摸板状态！精准识别，绝不误杀。


---

##  快速上手

1. **下载与运行**：
   前往 [Releases](#) 页面下载最新版本的程序压缩包，解压后双击运行即可。

2. **添加配置**：
   - 打开设置中心，选择“中键平移”或“滚动模拟”选项卡。
   - 输入目标软件的进程名（例如：`SLDWORKS`，不带 `.exe`）。
   - 使用 `Spy++` 等抓窗工具获取目标软件 **3D 画布区域**或**列表区域**的窗口类名（例如：`AfxWnd140su`），并添加到名单中。

3. **一键开启**：
   - 点击开启卡片开关（UI 变蓝即为开启！）。
   - 也可使用全局快捷键 `Alt + Shift + M` 随时切换全局中键接管状态。
   - 将光标移入目标区域，双指贴上触摸板，享受前所未有的丝滑吧！
4. **配置文件与保存**:
   - 在发行版的文件夹下配备了"TouchpadConfig.xml"文件，我提供了一些我自用的预设供大家使用。
---

## 🛠️ 技术栈

- **语言 / 框架**：C# / .NET 8.0, WPF + Wpf.Ui (Fluent Design)
- **核心底层技术**：Windows `WH_MOUSE_LL` 低级全局鼠标钩子
- **Win32 API 集成**：`GetCursorPos`, `WindowFromPoint`, `GetClassName`, `mouse_event`
- **数据持久化**：`XmlSerializer`

---

## ☕ 关于与支持

老实说，这个插件的技术力不高，在和这些底层 Bug 死磕、重构架构的过程中，也有不少代码是 AI 帮我一起头脑风暴写出来的。

但我当初想做这个小插件，是因为我羡慕 MacBook 的触摸板体验，但暂时买不起。如果你觉得它确确实实地帮助到你，解决了你的痛点，愿意请我喝杯咖啡，我会非常感激！

在那之前，我会继续优化 Windows 上稀烂的触摸板体验。

---

## 特别鸣谢
一些底层的触摸板报文解包原理参考了这个项目：ClementGre的https://github.com/ClementGre/ThreeFingerDragOnWindows

- **作者**: Zisheng (XiaoSheng)
- **GitHub**: [hzs-zz](https://github.com/hzs-zz)

*(如果这个项目帮到了你，欢迎给个 ⭐ Star！也欢迎提交 PR 与 Issue 共同完善！)*
