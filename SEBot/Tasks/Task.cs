// ReSharper disable once CheckNamespace

using VRage.Serialization;

namespace SEBot
{
	public sealed partial class Program
	{
		public interface ITask
		{
			bool Execute(Environment env);
		}
	}
}