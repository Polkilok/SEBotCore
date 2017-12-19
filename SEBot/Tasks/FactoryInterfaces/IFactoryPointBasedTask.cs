using VRageMath;

namespace SEBot
{
	public sealed partial class Program
	{
		//Интерфейс фабрики, которая будет создавать задачи, основным аргументом которых является точка и необязательное условие
		interface IFactoryPointBasedTask
		{
			Task GetTask(Vector3D targetPoint, bool condition = false);
		}
	}

}