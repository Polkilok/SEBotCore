using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//FIXID внешне-локальные внутренне-корабельные координаты
		class MyGyros
		{
			private List<SmartGyro> SmartGyroscopes;
			private ShipSystems Ship;

			public MyGyros(ShipSystems owner, IMyGridTerminalSystem myGrid)
			{
				if (owner == null)
					throw new Exception("ShipController is null in MyGyros Constructor");
				Ship = owner;
				List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();
				myGrid.GetBlocksOfType<IMyGyro>(Gyros);
				if (Gyros.Count == 0)
					throw new Exception("We need at least one gyroscope");
				SmartGyroscopes = new List<SmartGyro>();
				foreach (var gyro in Gyros)
					SmartGyroscopes.Add(new SmartGyro(gyro as IMyGyro, owner.MainController));
			}

			private void ApplyTurnDirectionToAllGyroscopes(Base6Directions.Direction direction, Vector3D targetPointInShipCoordinate)
			{
				foreach (var gyro in SmartGyroscopes)
					gyro.TurnDirectionToPoint(direction, targetPointInShipCoordinate);
			}

			public void SetPower(float power)
			{
				foreach (var gyro in SmartGyroscopes)
					gyro.SetPower(power);
			}

			public void DisableOverride()
			{
				for (var iter = SmartGyroscopes.GetEnumerator(); iter.MoveNext();)
					iter.Current.DisableOverride();
			}

			//разворачивает в заданном направлении
			public void TurnDirectionToPoint(Base6Directions.Direction direction, Vector3D pointInGlobalCoordinates)
			{

				//переходим в координаты корабля
				Base6Directions.Direction directionInMainBlockCoordinates = Ship.MainController.Orientation.TransformDirection(direction);

				Vector3D pointInMainBlockCoordinates = Ship.TravelSystem.ToLocalCoordinate(pointInGlobalCoordinates);
				//Log.Log("TurnDirectionToPoint");
				//Log.Log("Input Direction " + direction.ToString());
				//Log.Log("Use Direction " + directionInMainBlockCoordinates.ToString());
				ApplyTurnDirectionToAllGyroscopes(directionInMainBlockCoordinates, pointInMainBlockCoordinates);

				Vector3D dir = Base6Directions.GetVector(directionInMainBlockCoordinates);
				Vector3D bias = dir - Vector3D.Normalize(pointInMainBlockCoordinates);
				bias.SetDim(dir.AbsMaxComponent(), 0);
				//Log.Log("Bias = " + FloorCoordinate(bias));
				//Override(Vector3.Normalize(bias));
				//return bias.Length() < GYRO_E;
			}
		}
	}

}