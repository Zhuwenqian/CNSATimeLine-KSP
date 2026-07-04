/*
 * 文件用途：
 * 本文件实现 CNSATimeLine 模组在太空中心（SpaceCentre）场景的 IMGUI 窗口。
 * 窗口通过 Ctrl+L 切换显示/隐藏，列出所有中国航天大事时间线事件，
 * 每个事件旁提供“加速到此时”按钮，点击后将 KSP 全局时间加速到该事件前 10 小时。
 * 时间加速采用“两段式”：先 WarpTo 到当前 UT 与目标 UT 的中点，自动存档一次，
 * 然后再 WarpTo 到最终目标 UT，降低长时间加速出错的损失。
 * 所有可见文本（窗口标题、按钮、提示、事件描述）均通过 KSP 的 Localizer 读取，
 * 支持根据游戏语言自动切换 zh-cn / en-us / ru 等本地化内容。
 *
 * 可调参数：
 * - ToggleKey：切换窗口可见性的快捷键，默认 KeyCode.L。
 * - ModifierKey：组合修饰键，默认 LeftControl 或 RightControl。
 * - WindowRect：窗口初始位置与大小。
 * - WarpAheadSeconds：加速提前量，默认 36000 秒（10 小时）。
 *   修改后会改变点击按钮后到达的目标时间。
 * - EnableMidWarpSave：是否启用中途自动存档，默认 true。
 * - MidSaveFileName：中途自动存档文件名（不含 .sfs），默认 "CNSATimeLine_AutoSave"。
 * - MinMidSaveIntervalSeconds：总加速间隔小于该秒数时跳过中点存档，避免频繁存档。
 *   默认 60 秒。
 */

using System.Collections;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// 太空中心场景的时间线 UI 窗口。
    /// </summary>
    public class TimeLineWindow : MonoBehaviour
    {
        /// <summary>
        /// 切换窗口可见性的快捷键。
        /// </summary>
        public KeyCode ToggleKey = KeyCode.L;

        /// <summary>
        /// 加速提前量，单位秒。默认 36000 秒 = 10 小时。
        /// </summary>
        public double WarpAheadSeconds = 10.0 * 3600.0;

        /// <summary>
        /// 是否启用中途自动存档。
        /// 启用后，长时间加速会先到达中点并自动存档，再继续加速到目标。
        /// </summary>
        public bool EnableMidWarpSave = true;

        /// <summary>
        /// 中途自动存档文件名（不含 .sfs 扩展名）。
        /// 默认 "CNSATimeLine_AutoSave"，会以覆盖方式写入当前存档目录。
        /// </summary>
        public string MidSaveFileName = "CNSATimeLine_AutoSave";

        /// <summary>
        /// 当总加速间隔小于该秒数时，跳过中点自动存档。
        /// 避免目标很近时仍执行一次无意义的存档。默认 60 秒。
        /// </summary>
        public double MinMidSaveIntervalSeconds = 60.0;

        /// <summary>
        /// 窗口初始矩形区域。
        /// </summary>
        private Rect windowRect = new Rect(100, 100, 700, 550);

        /// <summary>
        /// 窗口是否可见。
        /// </summary>
        private bool isVisible = false;

        /// <summary>
        /// 滚动视图位置。
        /// </summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// 窗口唯一标识。
        /// </summary>
        private const int WindowId = 12345001;

        /// <summary>
        /// UI 文本本地化键：窗口标题。
        /// </summary>
        private const string KeyWindowTitle = "#CNSATimeLine_UI_WindowTitle";

        /// <summary>
        /// UI 文本本地化键：顶部信息标签。
        /// </summary>
        private const string KeyHeader = "#CNSATimeLine_UI_Header";

        /// <summary>
        /// UI 文本本地化键：无事件提示。
        /// </summary>
        private const string KeyNoEvents = "#CNSATimeLine_UI_NoEvents";

        /// <summary>
        /// UI 文本本地化键：关闭窗口按钮。
        /// </summary>
        private const string KeyCloseButton = "#CNSATimeLine_UI_CloseButton";

        /// <summary>
        /// UI 文本本地化键：加速按钮。
        /// </summary>
        private const string KeyWarpButton = "#CNSATimeLine_UI_WarpButton";

        /// <summary>
        /// 当前显示的事件列表。
        /// </summary>
        private List<TimeLineEvent> events = new List<TimeLineEvent>();

        /// <summary>
        /// 是否正在执行分段加速流程。
        /// 用于防止同时触发多次加速，并在流程中禁用按钮。
        /// </summary>
        private bool isWarping = false;

        /// <summary>
        /// 初始化窗口数据。
        /// </summary>
        /// <param name="eventList">已加载的事件列表。</param>
        public void Initialize(List<TimeLineEvent> eventList)
        {
            events = eventList ?? new List<TimeLineEvent>();
            Debug.Log(string.Format("[CNSATimeLine] TimeLineWindow 初始化完成，共 {0} 条事件。", events.Count));
        }

        /// <summary>
        /// 每帧检测 Ctrl+L 热键以切换窗口可见性。
        /// </summary>
        private void Update()
        {
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrlPressed && Input.GetKeyDown(ToggleKey))
            {
                isVisible = !isVisible;
                Debug.Log(string.Format("[CNSATimeLine] 时间线窗口可见性切换为: {0}", isVisible));
            }
        }

        /// <summary>
        /// 绘制 IMGUI 窗口。
        /// </summary>
        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            windowRect = GUILayout.Window(
                WindowId,
                windowRect,
                DrawWindowContent,
                Localizer.Format(KeyWindowTitle),
                GUILayout.Width(windowRect.width),
                GUILayout.Height(windowRect.height));
        }

        /// <summary>
        /// 窗口内部绘制逻辑。
        /// </summary>
        /// <param name="id">窗口 ID。</param>
        private void DrawWindowContent(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label(Localizer.Format(KeyHeader, events.Count), GUILayout.Height(25));
            GUILayout.Space(5);

            if (events.Count == 0)
            {
                GUILayout.Label(Localizer.Format(KeyNoEvents));
            }
            else
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                double currentUT = Planetarium.GetUniversalTime();

                foreach (TimeLineEvent evt in events)
                {
                    DrawEventRow(evt, currentUT);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(5);
            if (GUILayout.Button(Localizer.Format(KeyCloseButton), GUILayout.Height(30)))
            {
                isVisible = false;
            }

            GUILayout.EndVertical();

            // 允许拖动窗口。
            GUI.DragWindow();
        }

        /// <summary>
        /// 绘制单条事件行。
        /// </summary>
        /// <param name="evt">事件对象。</param>
        /// <param name="currentUT">当前 KSP 全局 UT 秒数。</param>
        private void DrawEventRow(TimeLineEvent evt, double currentUT)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);

            // 左侧：事件时间 + 描述。
            GUILayout.BeginVertical(GUILayout.Width(520));
            string localizedDesc = GetLocalizedDescription(evt);
            GUILayout.Label(string.Format("{0:yyyy-MM-dd HH:mm:ss} | {1}", evt.EventDateTime, localizedDesc), GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(evt.Precision))
            {
                string precisionText = Localizer.Format(evt.Precision);
                // 若 Localizer 未找到对应键（返回空或键名本身），直接显示原始内容作为回退。
                if (string.IsNullOrEmpty(precisionText) || precisionText == evt.Precision)
                {
                    precisionText = evt.Precision;
                }
                GUILayout.Label(string.Format("  [{0}]", precisionText));
            }
            GUILayout.EndVertical();

            // 右侧：加速按钮。
            double targetUT = evt.UtcSeconds - WarpAheadSeconds;
            bool canWarp = targetUT > currentUT && !isWarping;

            GUI.enabled = canWarp;
            if (GUILayout.Button(Localizer.Format(KeyWarpButton), GUILayout.Width(100), GUILayout.Height(40)))
            {
                WarpToEvent(evt, targetUT);
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        /// <summary>
        /// 获取事件的本地化描述。
        /// 如果 Localizer 未找到对应键（返回键名本身或空），则回退到 Description 字段。
        /// </summary>
        /// <param name="evt">事件对象。</param>
        /// <returns>本地化后的描述文本。</returns>
        private string GetLocalizedDescription(TimeLineEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.LocalizationKey))
            {
                return evt != null ? evt.Description : string.Empty;
            }

            string result = Localizer.Format(evt.LocalizationKey);

            // Localizer 未找到键时可能返回空字符串或原始键名，此时使用 Description 回退。
            if (string.IsNullOrEmpty(result) || result == evt.LocalizationKey)
            {
                return evt.Description;
            }

            return result;
        }

        /// <summary>
        /// 将时间加速到指定目标 UT。
        /// 如果启用中途存档且间隔足够大，会拆分为两段加速并在中点自动存档。
        /// </summary>
        /// <param name="evt">目标事件。</param>
        /// <param name="targetUT">目标 UT 秒数。</param>
        private void WarpToEvent(TimeLineEvent evt, double targetUT)
        {
            if (TimeWarp.fetch == null)
            {
                Debug.LogError("[CNSATimeLine] TimeWarp.fetch 为空，无法加速。");
                return;
            }

            string localizedDesc = GetLocalizedDescription(evt);
            Debug.Log(string.Format("[CNSATimeLine] 开始加速到事件 '{0}' 前 {1} 秒，目标 UT: {2:F2}", localizedDesc, WarpAheadSeconds, targetUT));

            double currentUT = Planetarium.GetUniversalTime();
            double interval = targetUT - currentUT;

            if (!EnableMidWarpSave || interval < MinMidSaveIntervalSeconds)
            {
                // 不启用中途存档或间隔太小时，直接一次性加速到目标。
                TimeWarp.fetch.WarpTo(targetUT);
                return;
            }

            // 启用中途存档：启动协程完成分段加速。
            StartCoroutine(WarpWithMidSaveCR(targetUT));
        }

        /// <summary>
        /// 分段加速协程：先加速到中点，自动存档，再加速到最终目标。
        /// </summary>
        /// <param name="targetUT">最终目标 UT 秒数。</param>
        private IEnumerator WarpWithMidSaveCR(double targetUT)
        {
            isWarping = true;

            double startUT = Planetarium.GetUniversalTime();
            double midUT = startUT + (targetUT - startUT) * 0.5;

            Debug.Log(string.Format("[CNSATimeLine] 第一段加速：当前 UT {0:F2} -> 中点 UT {1:F2}", startUT, midUT));
            TimeWarp.fetch.WarpTo(midUT);

            // 等待第一段加速完成：到达中点附近且 warp 倍率回到 1x。
            // TimeWarp.CurrentRate 为静态属性，表示当前时间加速倍率，1x 时表示已恢复正常速度。
            yield return new WaitUntil(() =>
                Planetarium.GetUniversalTime() >= midUT - 1.0 && TimeWarp.CurrentRate <= 1f);

            // 执行中途自动存档。
            AutoSaveGame();

            Debug.Log(string.Format("[CNSATimeLine] 第二段加速：中点 UT {0:F2} -> 目标 UT {1:F2}", Planetarium.GetUniversalTime(), targetUT));
            TimeWarp.fetch.WarpTo(targetUT);

            isWarping = false;
        }

        /// <summary>
        /// 调用 KSP 的 GamePersistence.SaveGame 在当前存档目录覆盖保存一个自动存档。
        /// </summary>
        private void AutoSaveGame()
        {
            if (string.IsNullOrEmpty(MidSaveFileName))
            {
                Debug.LogWarning("[CNSATimeLine] 未设置 MidSaveFileName，跳过中途存档。");
                return;
            }

            try
            {
                string saveFolder = HighLogic.SaveFolder;
                string savedFile = GamePersistence.SaveGame(MidSaveFileName, saveFolder, SaveMode.OVERWRITE);

                if (!string.IsNullOrEmpty(savedFile))
                {
                    Debug.Log(string.Format("[CNSATimeLine] 中途自动存档成功：{0}", savedFile));
                }
                else
                {
                    Debug.LogWarning("[CNSATimeLine] 中途自动存档返回空文件名，可能已存在同名文件且 SaveMode.ABORT。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("[CNSATimeLine] 中途自动存档失败：{0}", ex.Message));
            }
        }
    }
}
