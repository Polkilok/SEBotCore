using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		class FactoryMoveInDirection : IFactoryPointDirectionBasedTask
		{
			private readonly ThrusterEnableRule ThrusterEnableSwitch;
			public FactoryMoveInDirection(ThrusterEnableRule rule)
			{
				ThrusterEnableSwitch = rule;
			}

			public ITask GetTask(Vector3D targetPoint, Base6Directions.Direction direction)
			{
				return new MoveInDirection(ThrusterEnableSwitch, direction, targetPoint);
			}
		}
	}

}