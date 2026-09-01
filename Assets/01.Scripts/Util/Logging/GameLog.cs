using System;
using System.Collections.Generic;
using _01.Scripts.Manager;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _01.Scripts.Util.Logging
{
    /// <summary>
    /// 로그의 중요도입니다. MinimumLevel보다 낮은 로그는 출력하지 않습니다.
    /// </summary>
    public enum GameLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Exception = 4,
        None = 5
    }

    /// <summary>
    /// 출력 대상으로 전달되는 로그 데이터입니다.
    /// 파일 및 원격 로그 기능을 추가할 때도 같은 데이터를 사용할 수 있습니다.
    /// </summary>
    public readonly struct GameLogEntry
    {
        public GameLogEntry(
            GameLogLevel level,
            string message,
            string category,
            Exception exception,
            UnityEngine.Object context)
        {
            TimestampUtc = DateTime.UtcNow;
            Level = level;
            Message = message;
            Category = category;
            Exception = exception;
            Context = context;
        }

        public DateTime TimestampUtc { get; }
        public GameLogLevel Level { get; }
        public string Message { get; }
        public string Category { get; }
        public Exception Exception { get; }
        public UnityEngine.Object Context { get; }
    }

    /// <summary>
    /// 로그 출력 대상이 구현해야 하는 인터페이스입니다.
    /// </summary>
    public interface IGameLogSink
    {
        void Write(in GameLogEntry entry);
    }

    /// <summary>
    /// 프로젝트 전체에서 사용하는 로그 진입점입니다.
    /// </summary>
    public static class GameLog
    {
        private static readonly List<IGameLogSink> Sinks = new List<IGameLogSink>
        {
            new UnityLogSink()
        };

        private static bool Enabled { get; set; } = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static GameLogLevel MinimumLevel { get; set; } = GameLogLevel.Debug;
#else
        public static GameLogLevel MinimumLevel { get; set; } = GameLogLevel.Info;
#endif

        public static void Debug(string message, string category = null, UnityEngine.Object context = null)
        {
            Write(GameLogLevel.Debug, message, category, null, context);
        }

        public static void Info(string message, string category = null, UnityEngine.Object context = null)
        {
            Write(GameLogLevel.Info, message, category, null, context);
        }

        public static void Warning(string message, string category = null, UnityEngine.Object context = null)
        {
            Write(GameLogLevel.Warning, message, category, null, context);
        }

        public static void Error(string message, string category = null, Object context = null)
        {
            Write(GameLogLevel.Error, message, category, null, context);
        }

        public static void Exception(Exception exception, string category = null, UnityEngine.Object context = null)
        {
            if (exception == null)
            {
                Error("A null exception was passed to GameLog.Exception.", category, context);
                return;
            }

            Write(GameLogLevel.Exception, exception.Message, category, exception, context);
        }

        public static void AddSink(IGameLogSink sink)
        {
            if (sink == null || Sinks.Contains(sink))
                return;

            Sinks.Add(sink);
        }

        public static bool RemoveSink(IGameLogSink sink)
        {
            return sink != null && Sinks.Remove(sink);
        }

        public static void ResetSinks()
        {
            Sinks.Clear();
            Sinks.Add(new UnityLogSink());
        }

        private static void Write(
            GameLogLevel level,
            string message,
            string category,
            Exception exception,
            UnityEngine.Object context)
        {
            if (!Enabled || level < MinimumLevel || MinimumLevel == GameLogLevel.None)
                return;

            var entry = new GameLogEntry(
                level,
                message ?? "<null>",
                category,
                exception,
                context);

            for (var i = 0; i < Sinks.Count; i++)
            {
                try
                {
                    Sinks[i].Write(in entry);
                }
                catch (Exception sinkException)
                {
                    UnityEngine.Debug.LogException(sinkException);
                }
            }
        }
    }

    /// <summary>
    /// Unity Console로 로그를 출력하는 기본 출력 대상입니다.
    /// </summary>
    public sealed class UnityLogSink : IGameLogSink
    {
        public void Write(in GameLogEntry entry)
        {
            var message = FormatMessage(entry);

            switch (entry.Level)
            {
                case GameLogLevel.Debug:
                case GameLogLevel.Info:
                    UnityEngine.Debug.Log(message, entry.Context);
                    break;

                case GameLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message, entry.Context);
                    break;

                case GameLogLevel.Error:
                    UnityEngine.Debug.LogError(message, entry.Context);
                    break;

                case GameLogLevel.Exception:
                    if (entry.Exception != null)
                        UnityEngine.Debug.LogException(entry.Exception, entry.Context);
                    else
                        UnityEngine.Debug.LogError(message, entry.Context);
                    break;
            }
        }

        private static string FormatMessage(in GameLogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Category))
                return entry.Message;

            return $"[{entry.Category}] {entry.Message}";
        }
    }
}
