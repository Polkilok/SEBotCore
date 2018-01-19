using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SEBot
{
	public sealed partial class Program
	{
		private class Logger
		{
			public const string INFO_STR = "INFO";
			private const string ERROR_STR = "ERROR";
			private const string WARNING_STR = "WARNING";
			private readonly Dictionary<string, List<Message>> _buffer;

			private readonly List<string> _enabledSources;

			private readonly IMyTextPanel _logPanel;

			private readonly int _maxLines;

			private int _currentLineCount;

			//инициализация логгера
			public Logger(IMyGridTerminalSystem gridTerminalSystem, List<string> enabledSources, int maxLines)
			{
				if (maxLines <= 0) throw new Exception($"Out of range:{nameof(maxLines)}:{maxLines}");
				_maxLines = maxLines;
				_enabledSources = enabledSources;
				_enabledSources.Add(WARNING_STR);
				_enabledSources.Add(ERROR_STR);
				_enabledSources.Add(INFO_STR);
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

			public void Error(string msg)
			{
				Log(msg, ERROR_STR);
				Flush(ERROR_STR);
			}

			public void Flush()
			{
				if (_logPanel != null)
				{
					var messages = _buffer.SelectMany(pair => pair.Value).OrderBy(message => message.Number);
					var buf = string.Join("\n", messages.Select(m => $"{m.Source}:{m.Information}"));
					_logPanel.WritePublicText("Cleared\n");
					_logPanel.WritePublicText(buf, true);
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
					var messages = src.Value.OrderBy(m => m.Number);
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
					// ReSharper disable once InlineOutVariableDeclaration
					List<Message> messages;
					if (!_buffer.TryGetValue(source, out messages))
						_buffer.Add(source, messages = new List<Message>());
					AddMessage(messages, msg, source);
				}
			}

			public void Warning(string msg)
			{
				Log(msg, WARNING_STR);
				Flush(WARNING_STR);
			}

			private void AddMessage(IList<Message> log, string msg, string src)
			{
				log.Add(new Message { Information = msg, Source = src });
				_currentLineCount++;
				if (_currentLineCount > _maxLines) //TODO это не лучший выбор сообщений для удаления
				{
					log.RemoveAt(0);
					_currentLineCount--;
				}
			}

			private class Message
			{
				private static int _num = 0;

				public Message()
				{
					Number = _num++;
				}

				public string Information { get; set; }
				public int Number { get; }
				public string Source { get; set; }
			}
		}
	}
}