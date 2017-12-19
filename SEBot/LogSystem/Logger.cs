using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		private class Logger
		{
			private const string ERROR_STR = "error";
			private const string WARNING_STR = "warning";
			private class Message
			{
				public DateTime Time { get; set; }
				public string Source { get; set; }
				public string Information { get; set; }
			}
			private readonly int _maxLines;
			private int _currentLineCount;
			private readonly IMyTextPanel _logPanel;
			private readonly List<string> _enabledSources;
			private readonly Dictionary<string, List<Message>> _buffer;
			//инициализация логгера
			public Logger(IMyGridTerminalSystem gridTerminalSystem, List<string> enabledSources, int maxLines)
			{
				if (maxLines <= 0) throw new Exception($"Out of range:{nameof(maxLines)}:{maxLines}");
				_maxLines = maxLines;
				_enabledSources = enabledSources;
				_enabledSources.Add(ERROR_STR);
				_enabledSources.Add(WARNING_STR);
				_buffer = new Dictionary<string, List<Message>>();
				_currentLineCount = 0;
				_logPanel = (IMyTextPanel)gridTerminalSystem.GetBlockWithName("Log");
				//if (_logPanel == null)
				//{
				//	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
				//	gridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks, block => block.Name.Contains("Log"));
				//	if (blocks.Count != 0)
				//		_logPanel = (IMyTextPanel)blocks.First();
				//}
			}

			public void Warning(string msg)
			{
				Log(WARNING_STR, msg);
				Flush(WARNING_STR);
			}

			public void Error(string msg)
			{
				Log(ERROR_STR, msg);
				Flush(ERROR_STR);
			}

			public void Flush()
			{
				if (_logPanel != null)
				{
					var messages = _buffer.SelectMany(pair => pair.Value).OrderBy(message => message.Time);
					var buf = string.Join("\n", messages.Select(m => $"{m.Source}:{m.Information}"));
					_logPanel.WritePublicText(buf);
					_currentLineCount = 0;
					_buffer.Clear();
				}
			}

			public void Flush(string source)
			{
				if (string.IsNullOrEmpty(source)) throw new Exception($"ArgumentNullException:{nameof(source)}");
				if (_logPanel != null)
				{
					KeyValuePair<string, List<Message>> src;
					try
					{
						src = _buffer.First(m => m.Key.Equals(source));
					}
					catch (Exception)
					{
						return;
					}
					var messages = src.Value.OrderBy(m => m.Time);
					var buf = string.Join("\n", messages.Select(m => $"{m.Source}:{m.Information}"));
					_logPanel.WritePublicText(buf);
					_currentLineCount -= src.Value.Count;
					src.Value.Clear();
				}
			}

			/// <summary>
			/// Depricated
			/// </summary>
			public void Log(string msg, int lvl = 0)
			{
				Log($"{lvl}", msg);
			}

			public void Log(string msg, string source)
			{
				if (string.IsNullOrEmpty(msg)) throw new Exception($"ArgumentNullException:{nameof(msg)}");
				if (string.IsNullOrEmpty(source)) throw new Exception($"ArgumentNullException:{nameof(source)}");

				if (_logPanel != null && _enabledSources.Exists(r => r.Equals(source)))
				{
					if (!_buffer.TryGetValue(source, out var messages))
						_buffer.Add(source, messages = new List<Message>());
					AddMessage(messages, msg, source);
				}
			}

			private void AddMessage(IList<Message> log, string msg, string src)
			{
				log.Add(new Message{Information = msg, Time = DateTime.UtcNow, Source = src});
				_currentLineCount++;
				if (_currentLineCount > _maxLines) //TODO это не лучший выбор сообщений для удаления 
				{
					log.RemoveAt(0);
					_currentLineCount--;
				}
			}
		}
	}

}