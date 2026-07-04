/*
 * 文件用途：
 * 本文件负责构建中国航天大事时间线事件列表。
 * 事件的真实日期、本地化键和精度信息硬编码在本类中；事件描述文本不再从 txt 文件读取，
 * 而是通过 LocalizationKey 到 KSP 的 Localization 系统（Localization/*.cfg）中按当前游戏语言读取。
 * 由于事件日期按北京时间（UTC+8）记录，而 KSP/RSS-RO 的全局 UT 显示为 UTC+0，
 * 本文件在计算 UtcSeconds 时会自动加上 8 小时，使加速目标对应北京时间。
 *
 * 可调参数：
 * - Events：事件元数据数组。增加或删除事件只需修改此数组，
 *   并同步在 Localization/zh-cn.cfg、en-us.cfg、ru.cfg 中添加对应键值。
 * - DefaultPrecision：当某个事件未指定精度时使用的默认值。
 * - BeijingTimeZoneOffsetHours：北京时间与 UTC+0 的时差，默认 8 小时。
 *   若改为其他时区的事件，可调整此值；UTC+0 时设为 0。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// 时间线数据加载器。
    /// </summary>
    public static class TimeLineDataLoader
    {
        /// <summary>
        /// 当事件元数据未提供精度时使用的默认精度本地化键。
        /// </summary>
        public const string DefaultPrecision = "#CNSATimeLine_Precision_Day";

        /// <summary>
        /// 北京时间与 UTC+0 的时差，单位小时。
        /// 默认 8 小时；计算 UT 时会加上该小时数，让 RSS-RO 的 UTC+0 时间线对齐到北京时间。
        /// 若事件日期本身已是 UTC+0，可改为 0。
        /// </summary>
        public const double BeijingTimeZoneOffsetHours = 8.0;

        /// <summary>
        /// 事件元数据数组，包含：真实日期时间、本地化键、精度本地化键。
        /// 增加事件时，请同时更新 Localization 目录下的 zh-cn.cfg / en-us.cfg / ru.cfg。
        /// </summary>
        private static readonly Tuple<DateTime, string, string>[] Events =
        {
            Tuple.Create(new DateTime(1956, 10, 08, 0, 0, 0), "#CNSATimeLine_Event_19561008", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(1970, 04, 24, 21, 35, 44), "#CNSATimeLine_Event_19700424", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(1975, 11, 26, 0, 0, 0), "#CNSATimeLine_Event_19751126", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(1984, 01, 29, 0, 0, 0), "#CNSATimeLine_Event_19840129", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(1984, 04, 08, 0, 0, 0), "#CNSATimeLine_Event_19840408", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(1999, 11, 20, 6, 30, 7), "#CNSATimeLine_Event_19991120", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2000, 10, 31, 0, 0, 0), "#CNSATimeLine_Event_20001031", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(2003, 05, 25, 0, 0, 0), "#CNSATimeLine_Event_20030525", "#CNSATimeLine_Precision_Day"),
            Tuple.Create(new DateTime(2003, 10, 15, 9, 0, 0), "#CNSATimeLine_Event_20031015", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2005, 10, 12, 9, 0, 0), "#CNSATimeLine_Event_20051012", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2007, 04, 14, 4, 11, 0), "#CNSATimeLine_Event_20070414", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2007, 10, 24, 18, 5, 4), "#CNSATimeLine_Event_20071024", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2008, 09, 25, 21, 10, 4), "#CNSATimeLine_Event_20080925", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2009, 03, 01, 16, 13, 10), "#CNSATimeLine_Event_20090301", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2010, 10, 01, 18, 59, 57), "#CNSATimeLine_Event_20101001", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2011, 09, 29, 21, 16, 3), "#CNSATimeLine_Event_20110929", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2011, 11, 01, 5, 58, 10), "#CNSATimeLine_Event_20111101", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2012, 06, 16, 18, 37, 24), "#CNSATimeLine_Event_20120616", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2013, 06, 11, 17, 38, 2), "#CNSATimeLine_Event_20130611", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2013, 12, 02, 1, 30, 0), "#CNSATimeLine_Event_20131202", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2016, 09, 15, 22, 4, 12), "#CNSATimeLine_Event_20160915", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2016, 10, 17, 7, 30, 31), "#CNSATimeLine_Event_20161017", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2017, 11, 05, 19, 45, 0), "#CNSATimeLine_Event_20171105", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2018, 12, 08, 2, 23, 0), "#CNSATimeLine_Event_20181208", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2019, 01, 03, 10, 26, 0), "#CNSATimeLine_Event_20190103", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2020, 06, 23, 9, 43, 0), "#CNSATimeLine_Event_20200623", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2020, 07, 23, 12, 41, 15), "#CNSATimeLine_Event_20200723", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2020, 11, 24, 4, 30, 0), "#CNSATimeLine_Event_20201124", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2021, 04, 29, 11, 23, 15), "#CNSATimeLine_Event_20210429", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2021, 05, 22, 10, 40, 0), "#CNSATimeLine_Event_20210522", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2021, 06, 17, 9, 22, 31), "#CNSATimeLine_Event_20210617", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2021, 10, 16, 0, 23, 56), "#CNSATimeLine_Event_20211016", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2022, 06, 05, 10, 44, 10), "#CNSATimeLine_Event_20220605", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2022, 11, 29, 23, 8, 17), "#CNSATimeLine_Event_20221129", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2023, 05, 30, 9, 31, 10), "#CNSATimeLine_Event_20230530", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2023, 10, 26, 11, 14, 2), "#CNSATimeLine_Event_20231026", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2024, 04, 25, 20, 59, 0), "#CNSATimeLine_Event_20240425", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2024, 05, 03, 17, 27, 0), "#CNSATimeLine_Event_20240503", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2024, 06, 25, 14, 7, 0), "#CNSATimeLine_Event_20240625", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2024, 10, 30, 4, 27, 34), "#CNSATimeLine_Event_20241030", "#CNSATimeLine_Precision_Second"),
            Tuple.Create(new DateTime(2025, 04, 24, 17, 17, 0), "#CNSATimeLine_Event_20250424", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2025, 10, 31, 23, 44, 0), "#CNSATimeLine_Event_20251031", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2025, 11, 25, 12, 11, 0), "#CNSATimeLine_Event_20251125", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2026, 05, 11, 17, 30, 0), "#CNSATimeLine_Event_20260511", "#CNSATimeLine_Precision_Minute"),
            Tuple.Create(new DateTime(2026, 05, 24, 23, 8, 0), "#CNSATimeLine_Event_20260524", "#CNSATimeLine_Precision_Minute")
        };

        /// <summary>
        /// 加载并构建时间线事件列表。
        /// </summary>
        /// <returns>按时间顺序排列的事件列表。</returns>
        public static List<TimeLineEvent> Load()
        {
            var events = new List<TimeLineEvent>(Events.Length);

            foreach (Tuple<DateTime, string, string> meta in Events)
            {
                DateTime eventTime = meta.Item1;
                string localizationKey = meta.Item2;
                string precision = string.IsNullOrEmpty(meta.Item3) ? DefaultPrecision : meta.Item3;

                // 将北京时间对应的 UT 加上时区偏移，使 RSS-RO 的 UTC+0 显示对齐到北京时间。
                DateTime targetTime = eventTime.AddHours(BeijingTimeZoneOffsetHours);

                events.Add(new TimeLineEvent
                {
                    EventDateTime = eventTime,
                    LocalizationKey = localizationKey,
                    Description = localizationKey,
                    Precision = precision,
                    UtcSeconds = TimeLineConverter.DateTimeToUtcSeconds(targetTime)
                });
            }

            Debug.Log(string.Format("[CNSATimeLine] 已构建 {0} 条时间线事件，使用本地化键读取描述。", events.Count));
            return events;
        }
    }
}
