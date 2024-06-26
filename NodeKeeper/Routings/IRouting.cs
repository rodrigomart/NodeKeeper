using NodeKeeper.Forwarders;

using System.Threading.Tasks;


namespace NodeKeeper.Routings
{
	public interface IRouting
	{
		string Listener { get; }

		string Name { get; }


		Task<IForwarder> GetForwarderAsync();
	};
};