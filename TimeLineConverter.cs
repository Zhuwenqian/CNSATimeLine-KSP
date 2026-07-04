/*
 * 文件用途：
 * 本文件负责将真实地球日期时间与 KSP 全局时间（UT，秒数）互相转换。
 * 它通过反射读取 RSSTimeFormatter.dll 中 RealDateTimeFormatter 的私有 epoch 字段，
 * 从而把事件的真实日期时间反算为 KSP 的 UT 秒数。
 *
 * 调用开源项目：
 * - RSSTimeFormatter v1.12.1.0（KSP-RO/RSSTimeFormatter）
 *   该项目将 KSPUtil.dateTimeFormatter 替换为 RealDateTimeFormatter，
 *   以自定义 epoch（默认 1951-01-01）为基准将 UT 秒数映射为真实地球日期时间。
 *
 * 可调参数：
 * - FallbackEpoch：当 RSSTimeFormatter 未加载时使用的默认 epoch。
 *   默认值为 1951-01-01，与 RSSTimeFormatter 的默认值保持一致。
 *   修改后会影响未安装 RSSTimeFormatter 时的事件 UT 映射结果。
 */

using System;
using System.Reflection;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// 真实地球日期时间与 KSP UT 秒数转换器。
    /// </summary>
    public static class TimeLineConverter
    {
        /// <summary>
        /// RSSTimeFormatter 未加载时的默认 epoch。
        /// 默认 1951-01-01，与 RSSTimeFormatter 的默认值保持一致。
        /// </summary>
        private static readonly DateTime FallbackEpoch = new DateTime(1951, 1, 1);

        /// <summary>
        /// 缓存的 epoch，避免每次转换都进行反射。
        /// </summary>
        private static DateTime? cachedEpoch;

        /// <summary>
        /// 获取当前生效的 epoch。
        /// 优先从 RSSTimeFormatter.RealDateTimeFormatter 反射读取；若失败则使用 FallbackEpoch。
        /// </summary>
        public static DateTime GetEpoch()
        {
            if (cachedEpoch.HasValue)
            {
                return cachedEpoch.Value;
            }

            DateTime epoch = FallbackEpoch;
            bool reflected = false;

            try
            {
                // 尝试获取 RSSTimeFormatter 的 RealDateTimeFormatter 类型。
                // 使用 Type.GetType 避免在 RSSTimeFormatter 不存在时产生编译期强依赖。
                Type realFormatterType = Type.GetType("RSSTimeFormatter.RealDateTimeFormatter, RSSTimeFormatter");

                if (realFormatterType != null &&
                    KSPUtil.dateTimeFormatter != null &&
                    realFormatterType.IsInstanceOfType(KSPUtil.dateTimeFormatter))
                {
                    // RealDateTimeFormatter 内部私有 readonly 字段 epoch 存储了基准日期。
                    FieldInfo epochField = realFormatterType.GetField(
                        "epoch",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (epochField != null)
                    {
                        object value = epochField.GetValue(KSPUtil.dateTimeFormatter);
                        if (value is DateTime dt)
                        {
                            epoch = dt;
                            reflected = true;
                            Debug.Log(string.Format("[CNSATimeLine] 从 RSSTimeFormatter 读取到 epoch: {0:yyyy-MM-dd}", epoch));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("[CNSATimeLine] 反射读取 RSSTimeFormatter epoch 失败，将使用默认值: {0}", ex.Message));
            }

            if (!reflected)
            {
                Debug.LogWarning(string.Format("[CNSATimeLine] RSSTimeFormatter 未检测到，使用默认 epoch: {0:yyyy-MM-dd}", epoch));
            }

            cachedEpoch = epoch;
            return epoch;
        }

        /// <summary>
        /// 将真实地球日期时间转换为 KSP UT 秒数。
        /// 计算公式：ut = (dateTime - epoch).TotalSeconds
        /// </summary>
        /// <param name="dateTime">真实地球日期时间。</param>
        /// <returns>对应的 KSP UT 秒数。</returns>
        public static double DateTimeToUtcSeconds(DateTime dateTime)
        {
            DateTime epoch = GetEpoch();
            return (dateTime - epoch).TotalSeconds;
        }

        /// <summary>
        /// 将 KSP UT 秒数转换回真实地球日期时间。
        /// </summary>
        /// <param name="utcSeconds">KSP UT 秒数。</param>
        /// <returns>对应的地球日期时间。</returns>
        public static DateTime UtcSecondsToDateTime(double utcSeconds)
        {
            DateTime epoch = GetEpoch();
            return epoch.AddSeconds(utcSeconds);
        }

        /// <summary>
        /// 清除缓存的 epoch，用于在运行时重新检测（一般不需要手动调用）。
        /// </summary>
        public static void ClearCache()
        {
            cachedEpoch = null;
        }
    }
}
