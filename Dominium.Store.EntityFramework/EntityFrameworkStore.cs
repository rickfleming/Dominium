using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Dominium.Store.EntityFramework
{
	public class EntityFrameworkStore : IStore
	{
		private readonly DbContext _ctx;

		public EntityFrameworkStore(DbContext ctx) => _ctx = ctx;
		
		public async Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot : AggregateRoot
			=> await _ctx.FindAsync<TRoot>(keyValues);

		public IQueryable<TRoot> Query<TRoot>() where TRoot : AggregateRoot
			=> _ctx.Query<TRoot>();

		public async Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> filter) where T : AggregateRoot
			=> await _ctx.Query<T>().SingleOrDefaultAsync(filter);

		public void Add<TRoot>(TRoot root) where TRoot : AggregateRoot
		{
			if (_ctx.Set<TRoot>().Local.All(e => e != root))
				_ctx.Add(root);
		}

		public async Task Commit()
			=> await _ctx.SaveChangesAsync();
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.