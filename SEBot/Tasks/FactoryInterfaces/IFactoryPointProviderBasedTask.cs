namespace SEBot
{
	public sealed partial class Program
	{
		/// <summary>
		/// Предоставляет возможность создавать задачи, на основе меняющейся точки
		/// </summary>
		public interface IFactoryPointProviderBasedTask
		{
			ITask GetTask(IPointProvider targetPoint);
		}
	}

}