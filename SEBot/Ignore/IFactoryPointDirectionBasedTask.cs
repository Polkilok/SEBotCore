using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Derpricated
		//Интерфейс фабрики, которая будет создавать задачи, основными аргументами которых является точка + направление
		interface IFactoryPointDirectionBasedTask
		{
			Task GetTask(Vector3D targetPoint, Base6Directions.Direction direction);
		}
	}

}