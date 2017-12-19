using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Класс для шаблонной компоновки ориентированных блоков
		/// </summary>
		/// <typeparam name="TBlockType"></typeparam>
		class OrientedBlocks<TBlockType>
			where TBlockType : class, IMyFunctionalBlock
		{
			private Dictionary<Base6Directions.Direction, List<TBlockType>> _blocks;
			public OrientedBlocks(ShipSystems ship, IMyGridTerminalSystem MyGrid)
			{
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}({ship}, {MyGrid})", INIT_SYSTEM);
				if (ship == null)
					throw new Exception($"ShipController is null in {nameof(OrientedBlocks<TBlockType>)} Constructor");
				List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
				MyGrid.GetBlocksOfType<TBlockType>(blocks);
				if (blocks.Count == 0)
					throw new Exception($"We need at least one {nameof(TBlockType)}");
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}.blocks.Count:{blocks.Count}", INIT_SYSTEM);
				//отлично, запилим словарик и рассортируем по направлениям все имеющиеся блоки
				_blocks = new Dictionary<Base6Directions.Direction, List<TBlockType>>(Base6Directions.EnumDirections.Length);
				//создаем списки блоков
				foreach (var i in Base6Directions.EnumDirections)
					_blocks.Add(i, new List<TBlockType>());
				//заполняем списки
				foreach (var block in blocks)
				{
					_blocks[block.Orientation.Forward].Add(block as TBlockType);
					TerminalBlockExtentions.ApplyAction(block, "OnOff_On");//на всякиц случай включим всё
				}
				//Логгирование количества блоков
				foreach (var i in Base6Directions.EnumDirections)
					Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}._blocks[{i}].Count:{_blocks[i].Count}", INIT_SYSTEM);
				Log.Log($"OrientedBlocks.{nameof(OrientedBlocks<TBlockType>)}.End", INIT_SYSTEM);
			}

		}
	}

}