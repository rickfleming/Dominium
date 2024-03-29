using System.Collections.Generic;

namespace Dominium
{
	public delegate void OnCommitEvent<in T>(T root, ICollection<IDomainEvent> events);

	public abstract class AggregateRoot : IEntity
	{
		private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

		public event OnCommitEvent<AggregateRoot> OnCommit;

		protected virtual void Commit()
		{
			OnCommit?.Invoke(this, _events);
			_events.Clear();
		}

		protected virtual void Emit(params IDomainEvent[] events)
			=> ((IEntity) this).Emit(events);

		void IEntity.Emit(params IDomainEvent[] events)
			=> _events.AddRange(events);
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.