/*
 * 文件用途：
 * 本文件是 CNSATimeLine 模组的入口类示例，用于演示如何在 KSP 1.12.5 中创建一个基础 Mod。
 * 类继承自 UnityEngine.MonoBehaviour，并通过 KSPAddon 属性指定在飞行场景（Flight）加载时初始化。
 * 同时展示了如何检测并依赖 RSSTimeFormatter.dll 程序集。
 *
 * 可调参数：
 * - Startup.Flight：Mod 加载时机，可改为 MainMenu、SpaceCentre、EditorAny 等，
 *   调整效果为控制 Mod 在 KSP 哪个生命周期阶段被实例化。
 */

using System;
using System.Reflection;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// CNSATimeLine 模组入口类。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CNSATimeLineMod : MonoBehaviour
    {
        // 是否启用 RSSTimeFormatter 依赖检测，默认 true。
        // 调整为 false 可跳过对 RSSTimeFormatter.dll 的加载检测。
        [SerializeField]
        private bool enableFormatterCheck = true;

        /// <summary>
        /// 对象唤醒时调用，用于初始化模组状态。
        /// </summary>
        private void Awake()
        {
            Debug.Log("[CNSATimeLine] Mod Awake.");

            if (enableFormatterCheck)
            {
                CheckRSSTimeFormatter();
            }
        }

        /// <summary>
        /// 第一帧更新前调用。
        /// </summary>
        private void Start()
        {
            Debug.Log("[CNSATimeLine] Mod Start.");
        }

        /// <summary>
        /// 每帧调用一次，可用于处理持续逻辑。
        /// </summary>
        private void Update()
        {
            // 示例：在此处添加每帧更新的业务逻辑。
        }

        /// <summary>
        /// 检测 RSSTimeFormatter.dll 是否已加载。
        /// 使用反射获取类型，避免在 RSSTimeFormatter API 变更时导致编译失败。
        /// </summary>
        private void CheckRSSTimeFormatter()
        {
            try
            {
                // 尝试从 RSSTimeFormatter 程序集中获取主要类型
                Type formatterType = Type.GetType("RSSTimeFormatter.RSSTimeFormatter, RSSTimeFormatter");

                if (formatterType != null)
                {
                    Debug.Log("[CNSATimeLine] RSSTimeFormatter dependency loaded successfully.");

                    // 示例：若存在 PrintTime 静态方法，可在此处调用。
                    // 具体方法名需根据 RSSTimeFormatter.dll 的实际 API 进行调整。
                    MethodInfo printTimeMethod = formatterType.GetMethod(
                        "PrintTime",
                        BindingFlags.Static | BindingFlags.Public);

                    if (printTimeMethod != null)
                    {
                        double currentTime = Planetarium.GetUniversalTime();
                        object result = printTimeMethod.Invoke(null, new object[] { currentTime });
                        Debug.Log($"[CNSATimeLine] Current universal time formatted: {result}");
                    }
                }
                else
                {
                    Debug.LogWarning("[CNSATimeLine] RSSTimeFormatter dependency not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CNSATimeLine] Failed to check RSSTimeFormatter: {ex.Message}");
            }
        }
    }
}
