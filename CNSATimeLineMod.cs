/*
 * 文件用途：
 * 本文件是 CNSATimeLine 模组的入口类，负责在太空中心（SpaceCentre）场景加载时初始化模组。
 * 模组启动时会读取 `中国航天大事时间线.txt`，解析事件列表并创建 TimeLineWindow UI 窗口，
 * 玩家可通过 Ctrl+L 打开窗口并选择事件加速到对应时间。
 *
 * 可调参数：
 * - Startup.SpaceCentre：指定本 Mod 在太空中心场景初始化。
 *   若需在其他场景加载，可改为 MainMenu、Flight、EditorAny 等，
 *   但当前设计仅针对太空中心的时间线浏览与加速。
 */

using System.Collections.Generic;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// CNSATimeLine 模组太空中心场景入口类。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class CNSATimeLineMod : MonoBehaviour
    {
        /// <summary>
        /// 对象唤醒时调用，用于初始化模组状态。
        /// </summary>
        private void Awake()
        {
            Debug.Log("[CNSATimeLine] Mod Awake in SpaceCentre.");
        }

        /// <summary>
        /// 第一帧更新前调用，加载数据并创建 UI 窗口。
        /// </summary>
        private void Start()
        {
            Debug.Log("[CNSATimeLine] Mod Start in SpaceCentre.");

            // 加载时间线数据。
            List<TimeLineEvent> events = TimeLineDataLoader.Load();

            // 在当前游戏对象上附加 TimeLineWindow 组件。
            TimeLineWindow window = gameObject.AddComponent<TimeLineWindow>();
            window.Initialize(events);

            Debug.Log("[CNSATimeLine] TimeLineWindow 已创建并初始化。");
        }
    }
}
