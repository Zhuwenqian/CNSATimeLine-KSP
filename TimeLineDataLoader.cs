/*
 * 文件用途：
 * 本文件负责读取并解析 `中国航天大事时间线.txt`，将其转换为 TimeLineEvent 列表。
 * 解析时兼容文件中的精度标注行、空行以及装饰性分隔线。
 *
 * 可调参数：
 * - DataFileRelativePath：相对于 KSP 安装根目录的数据文件路径。
 *   默认 "GameData/CNSATimeLine/中国航天大事时间线.txt"。
 *   修改后可指向其他位置的数据文件。
 * - IgnoreParseErrors：是否忽略解析失败的行。
 *   默认 true；改为 false 时遇到无法识别的行会记录错误并跳过。
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CNSATimeLine
{
    /// <summary>
    /// 时间线数据加载器。
    /// </summary>
    public static class TimeLineDataLoader
    {
        /// <summary>
        /// 数据文件相对于 KSP 安装根目录的路径。
        /// 默认位于 GameData/CNSATimeLine/ 下，与模组 DLL 同级。
        /// </summary>
        public const string DataFileRelativePath = "GameData/CNSATimeLine/中国航天大事时间线.txt";

        /// <summary>
        /// 是否忽略解析失败的行。
        /// 使用 static readonly 而非 const，避免编译器将条件判定为不可达代码。
        /// </summary>
        public static readonly bool IgnoreParseErrors = true;

        /// <summary>
        /// 匹配事件主行的正则表达式：YYYY-MM-DD HH:MM:SS | 描述。
        /// </summary>
        private static readonly Regex EventLineRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \| (.*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 匹配精度标注行的正则表达式，例如“【精确到秒】来源：科普中国”。
        /// </summary>
        private static readonly Regex PrecisionLineRegex = new Regex(
            @".*(精确到(?:日|分|秒)).*",
            RegexOptions.Compiled);

        /// <summary>
        /// 加载并解析时间线数据。
        /// </summary>
        /// <returns>按文件顺序排列的事件列表。</returns>
        public static List<TimeLineEvent> Load()
        {
            var events = new List<TimeLineEvent>();
            string filePath = GetDataFilePath();

            if (!File.Exists(filePath))
            {
                Debug.LogError(string.Format("[CNSATimeLine] 未找到时间线数据文件: {0}", filePath));
                return events;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                TimeLineEvent lastEvent = null;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    // 跳过空行与装饰性分隔线。
                    if (string.IsNullOrEmpty(line) || line.StartsWith("=") || line.StartsWith("-"))
                    {
                        continue;
                    }

                    // 优先尝试解析事件主行。
                    Match eventMatch = EventLineRegex.Match(line);
                    if (eventMatch.Success)
                    {
                        string dateTimeStr = eventMatch.Groups[1].Value.Trim();
                        string description = eventMatch.Groups[2].Value.Trim();

                        DateTime eventTime;
                        if (DateTime.TryParseExact(
                            dateTimeStr,
                            "yyyy-MM-dd HH:mm:ss",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out eventTime))
                        {
                            lastEvent = new TimeLineEvent
                            {
                                EventDateTime = eventTime,
                                Description = description,
                                Precision = string.Empty,
                                UtcSeconds = TimeLineConverter.DateTimeToUtcSeconds(eventTime)
                            };
                            events.Add(lastEvent);
                        }
                        else if (!IgnoreParseErrors)
                        {
                            Debug.LogError(string.Format("[CNSATimeLine] 无法解析日期时间: {0}", dateTimeStr));
                        }

                        continue;
                    }

                    // 若上一行是事件，则尝试将当前行作为精度标注。
                    if (lastEvent != null)
                    {
                        Match precisionMatch = PrecisionLineRegex.Match(line);
                        if (precisionMatch.Success)
                        {
                            lastEvent.Precision = precisionMatch.Groups[1].Value.Trim();
                            continue;
                        }
                    }

                    // 无法识别的行。
                    if (!IgnoreParseErrors)
                    {
                        Debug.LogWarning(string.Format("[CNSATimeLine] 忽略无法解析的行: {0}", line));
                    }
                }

                Debug.Log(string.Format("[CNSATimeLine] 成功加载 {0} 条时间线事件。", events.Count));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[CNSATimeLine] 读取时间线数据文件失败: {0}", ex.Message));
            }

            return events;
        }

        /// <summary>
        /// 获取数据文件的完整绝对路径。
        /// 使用 KSPUtil.ApplicationRootPath 作为根目录，兼容 Windows/Linux。
        /// </summary>
        /// <returns>数据文件的完整路径。</returns>
        public static string GetDataFilePath()
        {
            return Path.Combine(KSPUtil.ApplicationRootPath, DataFileRelativePath);
        }
    }
}
