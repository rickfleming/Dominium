using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Dominium.Store.EntityFramework
{
	public class EntityFrameworkStore : IStore
	{
		private readonly DbContext _ctx;

		public EntityFrameworkStore(DbContext ctx) => _ctx = ctx;
		
		public async Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot : class
			=> await _ctx.FindAsync<TRoot>(keyValues);

		public async Task Save<TRoot>(TRoot root) where TRoot : class
			=> await _ctx.SaveChangesAsync();
	}
}