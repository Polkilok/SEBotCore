using System;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//FIXID внешне-локальные внутренне-корабельные координаты
	    public class SmartGyro
		{
			private IMyGyro m_Gyro;
			private string MapRotateByZ = "Roll";
			private string MapRotateByY = "Yaw";
			private string MapRotateByX = "Pitch";
			//private float MaxRotationValue;//TODO реализовать
			private readonly Matrix TransformToLocalMatrix;
			public SmartGyro(IMyGyro Gyro, IMyCubeBlock MainBlock)
			{
				m_Gyro = Gyro;
				if (m_Gyro == null)
					throw new Exception("Can't create SmartGyro: IMyGyro is null");
				Log.Log("Init gyro " + m_Gyro.CustomName, INIT_SYSTEM);
				Log.Log("gyro orientation " + m_Gyro.Orientation.ToString(), INIT_SYSTEM);
				Matrix matrix = new Matrix();
				MainBlock.Orientation.GetMatrix(out matrix);
				Log.Log("MainBlock Matrix " + matrix.ToString(), INIT_SYSTEM);
				TransformToLocalMatrix = new Matrix();
				m_Gyro.Orientation.GetMatrix(out TransformToLocalMatrix);
				Log.Log("Gyro Matrix " + TransformToLocalMatrix.ToString(), INIT_SYSTEM);
				TransformToLocalMatrix = Matrix.Invert(TransformToLocalMatrix);

				TransformToLocalMatrix = Matrix.Multiply(matrix, TransformToLocalMatrix);
				Log.Log("Final transform Matrix " + TransformToLocalMatrix.ToString(), INIT_SYSTEM);
				//MaxRotationValue = m_Gyro.GetMaximum<float>(RotateByZ);
			}

			public void SetPower(float power)
			{
				m_Gyro.GyroPower = power;
			}

			//Жестко задает вращения по осям. !!!никиких преобразований нет
			public void SetOverride(VRageMath.Vector3D settings)
			{
				//Vector3D vec = Vector3D.Transform(settings, TransformToLocalMatrix);

				Vector3D vec = settings;
				//Log.Log("Gyro name " + m_Gyro.CustomName);
				//Log.Log("Rotate input " + FloorCoordinate(settings));
				//Log.Log("Rotate apply " + FloorCoordinate(vec));
				if (!m_Gyro.GyroOverride)
					m_Gyro.ApplyAction("Override");

				//TODO вращать только не 0-ые значения
				m_Gyro.SetValue(MapRotateByZ, (float)vec.Z);
				m_Gyro.SetValue(MapRotateByY, (float)vec.Y);
				m_Gyro.SetValue(MapRotateByX, (float)vec.X);

				//float x = m_Gyro.GetValue<float>(RotateByX);
				//float y = m_Gyro.GetValue<float>(RotateByY);
				//float z = m_Gyro.GetValue<float>(RotateByZ);

				//Log.Log("Gyro name: " + m_Gyro.CustomName);
				//Log.Log("RotateByX " + " real " + x.ToString() + " wait " + settings.X.ToString());
				//Log.Log("RotateByY " + " real " + y.ToString() + " wait " + settings.Y.ToString());
				//Log.Log("RotateByZ " + " real " + z.ToString() + " wait " + settings.Z.ToString());
			}

			public void DisableOverride()
			{
				m_Gyro.SetValue("Override", false);
			}

			public void TurnDirectionToPoint(Base6Directions.Direction directionInShipCoordinates, Vector3D pointInMainBlockCoordinates)
			{
				//Log.Log("Gyroscope name: " + m_Gyro.CustomName);
				//Log.Log("Input direction: " + directionInShipCoordinates.ToString());
				Vector3D pointInBlocCoordinates = Vector3D.Transform(pointInMainBlockCoordinates, TransformToLocalMatrix);
				//Log.Log("Ship target: " + FloorCoordinate(pointInMainBlockCoordinates));
				//Log.Log("Bloc target: " + FloorCoordinate(pointInBlocCoordinates));
				pointInBlocCoordinates = Vector3D.Normalize(pointInBlocCoordinates);
				Base6Directions.Direction directionInBlocCoordinates = m_Gyro.Orientation.TransformDirectionInverse(directionInShipCoordinates);
				//TODO разобраться, как работает этот костыль
				if (directionInBlocCoordinates == Base6Directions.Direction.Down)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Up;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Backward)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Forward;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Right)
				{
					pointInBlocCoordinates.Y = -pointInBlocCoordinates.Y;
				}
				else if (directionInBlocCoordinates == Base6Directions.Direction.Left)
				{
					directionInBlocCoordinates = Base6Directions.Direction.Right;
					pointInBlocCoordinates = Vector3D.Negate(pointInBlocCoordinates);
					pointInBlocCoordinates.Y = -pointInBlocCoordinates.Y;
				}

				//Log.Log("Used direction: " + directionInBlocCoordinates.ToString());
				Vector3D dir = Base6Directions.GetVector(directionInBlocCoordinates);
				Vector3D bias = pointInBlocCoordinates - dir;
				int unusedDimention = dir.AbsMaxComponent();
				if (bias.Length() > 1)//для увеличения скорости
				{
					bias.SetDim(unusedDimention, 0);
					bias = Vector3D.Normalize(bias);
				}
				bias.SetDim(unusedDimention, 0);
				//Log.Log("bias before swap " + FloorCoordinate(bias));
				Swap(ref bias, unusedDimention);
				//bias = Vector3D.Normalize(bias);
				//Log.Log("bias after swap " + FloorCoordinate(bias));
				//bias.X = 0;
				//bias.Y = 0;
				//bias.Z = 0;
				//SetOverride(pointLC);
				SetOverride(bias);
			}

			private static void Swap(ref Vector3D vec, int noSwappingIndex)
			{
				//TODO перебор ифами выглядит ущербно
				double val = 0;
				if (noSwappingIndex == 0)
				{
					val = vec.GetDim(1);
					vec.SetDim(1, vec.GetDim(2));
					vec.SetDim(2, val);
				}
				else if (noSwappingIndex == 1)
				{
					val = vec.GetDim(0);
					vec.SetDim(0, vec.GetDim(2));
					vec.SetDim(2, val);
				}
				else if (noSwappingIndex == 2)
				{
					val = vec.GetDim(0);
					vec.SetDim(0, vec.GetDim(1));
					vec.SetDim(1, val);
				}

			}
		}
	}

}