using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//все составляющие корабля должны работать в координатной сетке корабля, а не како-го то блока
		//при этом снаружи эта сетка должна выглядеть координатной сеткой, привязанной к основному блоку
	    public class ShipSystems
		{
			public readonly MyGyros OrientationSystem;
			public readonly MyTrusters MovementSystem;
			public MyTravelSystem TravelSystem;
			public MyCargo CargoSystem;
			public MyEnergySystem EnergySystem;
			public MyShipDrils Drils;
			//TODO реально это не система для стыковки
			public DockSystem DockSystem;
			public int Mass => (int) MainController.CalculateShipMass().TotalMass;
			public Vector3D Gravity => TravelSystem.ToLocalCoordinate(TravelSystem.GetPosition() + MainController.GetTotalGravity());
			public IMyShipController MainController { get; private set; }

			public ShipSystems(IMyGridTerminalSystem gridTerminalSystem, IMyShipController mainShipController)
			{
				Log.Log($"ShipSystems.ShipSystems({gridTerminalSystem},{mainShipController})", INIT_SYSTEM);
				MainController = mainShipController;
				Log.Log($"ShipSystems.ShipSystems.MainController.CustomName:{MainController.CustomName})", INIT_SYSTEM);
				Log.Log($"ShipSystems.ShipSystems.MainController.Orientation:\n{MainController.Orientation.ToString()})", INIT_SYSTEM);
				//Matrix M = new Matrix();
				//MainController.Orientation.GetMatrix(out M);
				//Log.Log($"Matrix {M.ToString()}", INIT_SYSTEM);
				OrientationSystem = new MyGyros(this, gridTerminalSystem);
				MovementSystem = new MyTrusters(this, gridTerminalSystem);
				CargoSystem = new MyCargo();
				EnergySystem = new MyEnergySystem();
				Drils = new MyShipDrils(gridTerminalSystem);
				//ThrusterEnableRule rule = new InertialThrusterEnableRule(ACCURACY_POSITIONING, ACCURACY_SPEED);
				//IFactoryPointDirectionBasedTask FactorySpeedLimit =
				//	new FactoryMoveInDirection(rule);
				//FactoryMoveToPoint travelFactory = new FactoryMoveToPoint(FactorySpeedLimit);
				var travelFactory = new PotentialMethodMoveFactory(DEATH_ZONE_FOR_ROTATION, SAFETY_FLY_HEIGHT);
				TravelSystem = new MyTravelSystem(MainController, travelFactory);

				List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
				gridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connectors);
				if (Connectors.Count == 0)
					throw new Exception("We need at least one Connector");
				DockSystem = new DockSystem(MainController, Connectors[0] as IMyShipConnector, travelFactory);
				Log.Log($"ShipSystems.ShipSystems.End", INIT_SYSTEM);

			}

		}
	}

}