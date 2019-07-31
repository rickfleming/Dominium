using System;
using System.Threading.Tasks;

namespace Dominium.Bus.Redis
{
	public interface IBus : IEventPublisher
	{
		void SubscribeAsync<T>(Func<T, Task> action);
		void Subscribe<T>(Action<T> action);
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.