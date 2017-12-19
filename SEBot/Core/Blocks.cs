using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Класс для шаблонной компоновки однотипных блоков
		/// </summary>
		/// <typeparam name="TBlockType">тип блоков</typeparam>
		class Blocks<TBlockType>
			where TBlockType : class, IMyFunctionalBlock
		{
			private List<TBlockType> _blocks;
			public Blocks(ShipSystems ship, IMyGridTerminalSystem MyGrid)
			{
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}({ship}, {MyGrid})", INIT_SYSTEM);
				if (ship == null)
					throw new Exception($"ShipController is null in {nameof(OrientedBlocks<TBlockType>)} Constructor");
				List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
				MyGrid.GetBlocksOfType<TBlockType>(blocks);
				if (blocks.Count == 0)
					throw new Exception($"We need at least one {nameof(TBlockType)}");
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}.blocks.Count:{blocks.Count}", INIT_SYSTEM);
				//отлично, запилим словарик и рассортируем по направлениям все имеющиеся блоки
				_blocks = blocks.Select(block => blocks as TBlockType).ToList();
				Log.Log($"Blocks.{nameof(Blocks<TBlockType>)}.End", INIT_SYSTEM);
			}

		}
	}

}