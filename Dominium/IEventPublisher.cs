using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dominium
{
	public interface IEventPublisher
	{
		Task Publish<T>(T message);
		Task PublishAll<T>(IEnumerable<T> messages);
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.