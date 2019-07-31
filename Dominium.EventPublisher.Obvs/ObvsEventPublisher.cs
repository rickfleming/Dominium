using System.Collections.Generic;
using System.Threading.Tasks;
using Obvs;
using Obvs.Types;

namespace Dominium.EventPublisher.Obvs
{
	public class ObvsEventPublisher : IEventPublisher
	{
		private readonly IServiceBus _bus;

		public ObvsEventPublisher(IServiceBus bus)
			=> _bus = bus;
		
		public async Task Publish<T>(T message)
			=> await _bus.PublishAsync((IEvent)message);

		public async Task PublishAll<T>(IEnumerable<T> messages)
		{
			foreach (var message in messages)
				await Publish(message);
		}
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.