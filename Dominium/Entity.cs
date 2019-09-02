namespace Dominium
{
	public abstract class Entity : IEntity
	{
		private IEntity _parent;

		protected Entity(IEntity parent)
			=> _parent = parent;

		protected virtual void Emit(params IDomainEvent[] events)
			=> ((IEntity) this).Emit(events);

		void IEntity.Emit(params IDomainEvent[] events)
			=> _parent.Emit(events);
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.