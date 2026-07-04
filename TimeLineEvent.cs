/*
 * 文件用途：
 * 本文件定义 CNSATimeLine 模组中时间线事件的数据结构。
 * 每个事件包含真实地球日期时间、事件描述、原始精度标注以及映射到 KSP 全局时间（UT）的秒数。
 *
 * 可调参数：
 * 本类为纯数据容器，无可调运行参数。
 */

using System;

namespace CNSATimeLine
{
    /// <summary>
    /// 单条中国航天大事时间线事件。
    /// </summary>
    public class TimeLineEvent
    {
        /// <summary>
        /// 事件的真实地球日期时间（北京时间 / UTC+8）。
        /// </summary>
        public DateTime EventDateTime { get; set; }

        /// <summary>
        /// 事件描述文本。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 原始文件中的精度标注，例如“精确到秒”、“精确到分”、“精确到日”。
        /// 仅用于展示，不影响 UT 计算。
        /// </summary>
        public string Precision { get; set; }

        /// <summary>
        /// 事件时间映射到 KSP 全局时间（UT）的秒数。
        /// 由 TimeLineConverter 根据 RSSTimeFormatter 的 epoch 计算得出。
        /// </summary>
        public double UtcSeconds { get; set; }

        /// <summary>
        /// 返回便于 UI 显示的字符串。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0:yyyy-MM-dd HH:mm:ss} | {1}", EventDateTime, Description);
        }
    }
}
