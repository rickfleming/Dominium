using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dominium
{
	public abstract class StoreDomainContext : IDomainContext
	{
		private readonly IStore _store;
		private readonly IEventPublisher _publisher;

		protected StoreDomainContext(IStore store, IEventPublisher publisher)
			=> (_store, _publisher) = (store, publisher);

		protected void Add<T>(T root) where T : AggregateRoot
		{
			_store.Add(root);
			RegisterRoot(root);
		}
		
		protected async Task<T> Load<T>(params object[] keyValues) where T : AggregateRoot
			=> RegisterRoot(await _store.Load<T>(keyValues));

		protected async Task<T> Load<T>(Expression<Func<T, bool>> filter) where T : AggregateRoot
			=> RegisterRoot(await _store.SingleOrDefaultAsync(filter));

		private T RegisterRoot<T>(T root) where T : AggregateRoot
		{
			if(root != null)
				root.OnCommit += async (target, events) => await Commit(events);
			return root;
		}

		private async Task Commit(IEnumerable<IDomainEvent> events)
		{
			await _store.Commit();
			await _publisher.PublishAll(events);
		}
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.