using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
	    public class MyTravelSystem
		{
			private readonly IMyShipController _shipController;

			public readonly PotentialMethodMoveFactory DefaultTravelFactory;

			public MyTravelSystem(IMyShipController shipController,
				PotentialMethodMoveFactory travelFactory)
			{
				_shipController = shipController;
				DefaultTravelFactory = travelFactory;
			}
			/// <summary>
			/// Позиция основного блока в мировых координатах
			/// </summary>
			/// <returns> позиция корабля</returns>
			public Vector3D GetPosition()
			{
				return _shipController.CubeGrid.GridIntegerToWorld(_shipController.Position);
			}
			/// <summary>
			/// Для извлечения текущей скорости корабля
			/// </summary>
			/// <returns>Вектор скорости корабля в локальных координатах</returns>
			public Vector3D Speed
			{
				get
				{
					Log.Log($"MyTravelSystem.Speed", TRAVEL_SYSTEM_DEBUG_LVL);
					MyShipVelocities speed = Ship.MainController.GetShipVelocities();
					Log.Log($"globalCoordinateSpeed:{FloorCoordinate(speed.LinearVelocity)}", TRAVEL_SYSTEM_DEBUG_LVL);
					Vector3D shipCoordinateSpeed = Ship.TravelSystem.ToLocalCoordinate(Ship.TravelSystem.GetPosition() + speed.LinearVelocity);
					Log.Log($"localCoordinateSpeed:{FloorCoordinate(speed.LinearVelocity)}", TRAVEL_SYSTEM_DEBUG_LVL);
					Log.Log($"MyTravelSystem.End", TRAVEL_SYSTEM_DEBUG_LVL);
					return shipCoordinateSpeed;
				}
			}
			//Преобразует в киновские локальные координаты
			public Vector3D ToLocalCoordinate(Vector3D point)
			{
				var matrix = GetMatrixTransformToLocalCoordinate();
				return Vector3D.Transform(point, matrix);
			}
			//возвращает значение GetMatrixTransformToGlobalCoordinate * point
			//при этом ось X направлена вправо, а Z - назад
			//В соответствии с координатами Кинов
			public Vector3D ToGlobalCoordinate(Vector3D point)
			{
				var matrix = GetMatrixTransformToGlobalCoordinate();
				return Vector3D.Transform(point, matrix);
			}
			/// <summary>
			/// Дает матрицу преобразования в локальные координаты (по Кинам)
			/// </summary>
			/// <returns>Матрица для преобразования в локальные координаты</returns>
			public MatrixD GetMatrixTransformToLocalCoordinate()
			{
				return MatrixD.Invert(_shipController.WorldMatrix);
			}
			/// <summary>
			/// Дает матрицу преобразования в глобальные координаты (по Кинам)
			/// Преобразование в соответствии с поворотом корабля
			/// Позиция в соответствии с основным блоком
			/// </summary>
			/// <returns>матрица преобразования в глобальные координаты</returns>
			public MatrixD GetMatrixTransformToGlobalCoordinate()
			{
				//Matrix Rotation = new Matrix();
				//ShipController.Orientation.GetMatrix(out Rotation);
				//return MatrixD.Multiply(ShipController.WorldMatrix, Rotation);
				return _shipController.WorldMatrix;
			}
		}
	}

}