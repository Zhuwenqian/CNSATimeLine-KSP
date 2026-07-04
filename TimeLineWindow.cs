/*
 * 文件用途：
 * 本文件实现 CNSATimeLine 模组在太空中心（SpaceCentre）场景的 IMGUI 窗口。
 * 窗口通过 Ctrl+L 切换显示/隐藏，列出所有中国航天大事时间线事件，
 * 每个事件旁提供“加速到此时”按钮，点击后将 KSP 全局时间加速到该事件前 10 小时。
 *
 * 可调参数：
 * - ToggleKey：切换窗口可见性的快捷键，默认 KeyCode.L。
 * - ModifierKey：组合修饰键，默认 LeftControl 或 RightControl。
 * - WindowRect：窗口初始位置与大小。
 * - WarpAheadSeconds：加速提前量，默认 36000 秒（10 小时）。
 *   修改后会改变点击按钮后到达的目标时间。
 */

using System.Collections.Generic;
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
        /// 当前显示的事件列表。
        /// </summary>
        private List<TimeLineEvent> events = new List<TimeLineEvent>();

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
                "中国航天大事时间线",
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

            GUILayout.Label(string.Format("共 {0} 个事件 | 按 Ctrl+L 关闭窗口", events.Count), GUILayout.Height(25));
            GUILayout.Space(5);

            if (events.Count == 0)
            {
                GUILayout.Label("未加载到任何事件，请检查 GameData/CNSATimeLine/中国航天大事时间线.txt 是否存在。");
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
            if (GUILayout.Button("关闭窗口", GUILayout.Height(30)))
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
            GUILayout.Label(evt.ToString(), GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(evt.Precision))
            {
                GUILayout.Label(string.Format("  [{0}]", evt.Precision));
            }
            GUILayout.EndVertical();

            // 右侧：加速按钮。
            double targetUT = evt.UtcSeconds - WarpAheadSeconds;
            bool canWarp = targetUT > currentUT;

            GUI.enabled = canWarp;
            if (GUILayout.Button("加速到此时", GUILayout.Width(100), GUILayout.Height(40)))
            {
                WarpToEvent(evt, targetUT);
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        /// <summary>
        /// 将时间加速到指定目标 UT。
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

            Debug.Log(string.Format("[CNSATimeLine] 加速到事件 '{0}' 前 {1} 秒，目标 UT: {2:F2}", evt.Description, WarpAheadSeconds, targetUT));
            TimeWarp.fetch.WarpTo(targetUT);
        }
    }
}
