namespace SEBot
{
	public sealed partial class Program
	{
		//интерфейс фабрики, которая будет создавать определенные задачи
		//Важно - не храните такие задачи - фабрики сделаны специально, чтобы создавать задачи по мере необходимости
	    public interface IFactoryTask
		{
			ITask GetTask();
		}
	}

}