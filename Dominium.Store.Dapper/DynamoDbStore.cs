using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dominium.Store.Dapper
{
	public class DynamoDbStore : IStore
	{
		private readonly string _connectionString;
		private readonly DbProviderFactory _dbFactory;
		private readonly ClassBuilder _builder = new ClassBuilder("Dominium.Store.Dapper.Dynamic");

		public DynamoDbStore(string connectionString, DbProviderFactory dbFactory)
			=> (_connectionString, _dbFactory) = (connectionString, dbFactory);

		public async Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot : class
		{
			var keys = DomainKeyAttribute.FromType<TRoot>().ToArray();
			var key = _builder.CreateObject(keys.ToArray(), keyValues);

			using(var db = Db())
				return await db.Active.GetOrDefaultAsync<TRoot>(key);
		}

		public async Task Save<TRoot>(TRoot root) where TRoot: class
		{
			using(var db = Db())
				await db.Active.UpsertAsync(root);
		}

		private UnitOfWork Db() => new UnitOfWork(_connectionString, _dbFactory);

		private class UnitOfWork : IDisposable
		{
			public DbConnection Active { get; }

			public UnitOfWork(string connectionString, DbProviderFactory dbFactory)
			{
				Active = dbFactory.CreateConnection();
				Active.ConnectionString = connectionString;
				Active.Open();

				while (Active.State != ConnectionState.Open)
					Thread.Sleep(100);
			}

			public void Dispose()
			{
				Active?.Close();
				Active?.Dispose();
			}
		}

	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.