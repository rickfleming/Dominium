using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dominium
{
	public interface IStore
	{
		void Add<T>(T root) where T : AggregateRoot;
		IQueryable<T> Query<T>() where T : AggregateRoot;
		Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> filter) where T : AggregateRoot;
		Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot : AggregateRoot;
		Task Commit();
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.