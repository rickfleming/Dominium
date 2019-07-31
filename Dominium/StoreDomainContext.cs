using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dominium
{
	public abstract class StoreDomainContext : IDomainContext
	{
		private readonly IStore _store;
		private readonly IEventPublisher _publisher;

		protected StoreDomainContext(IStore store, IEventPublisher publisher)
			=> (_store, _publisher) = (store, publisher);

		protected TRoot Init<TRoot>() where TRoot : AggregateRoot
		{
			var root = Activator.CreateInstance<TRoot>();
			root.OnCommit += async (target, events) => await Save(target, events);
			return root;
		}
		
		protected async Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot : AggregateRoot
		{
			var root = await _store.Load<TRoot>(keyValues);
			root.OnCommit += async (target, events) => await Save(target, events);
			return root;
		}

		private async Task Save<TRoot>(TRoot root, ICollection<IDomainEvent> events) where TRoot: AggregateRoot
		{
			await _store.Save(root);
			await _publisher.PublishAll(events);
		}
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.