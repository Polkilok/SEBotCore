namespace SEBot
{
	public sealed partial class Program
	{
		interface ICondition
		{
			//проверяет условие
			bool Check();
		}
	}

}