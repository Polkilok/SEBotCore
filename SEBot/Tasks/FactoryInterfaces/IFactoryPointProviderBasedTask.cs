namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет возможность создавать задачи, на основе меняющейся точки
		/// </summary>
		interface IFactoryPointProviderBasedTask
		{
			Task GetTask(IPointProvider targetPoint);
		}
	}

}