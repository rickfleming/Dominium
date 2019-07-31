using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Utf8Json;

namespace Dominium.Bus.Redis
{
	public class RedisBus : IBus, IDisposable
	{
		private readonly ISubscriber _sub;


		public RedisBus(ISubscriber sub)
			=> _sub = sub;

		public void SubscribeAsync<T>(Func<T, Task> action)
		{
			var channel = typeof(T).Name;
			
			_sub.Subscribe(channel, (r, v) =>
			{
				if(r != channel)
					return;

				var message = JsonSerializer.Deserialize<T>(v.ToString());

				action.Invoke(message).ContinueWith(t =>
				{
					if(!t.IsFaulted)
						return;
					
					throw t.Exception.InnerException;
				});
			});
			
		}

		public void Subscribe<T>(Action<T> action)
		{
			var channel = typeof(T).Name;
			
			_sub.Subscribe(channel, (r, v) =>
			{
				if(r != channel)
					return;

				var message = JsonSerializer.Deserialize<T>(v.ToString());
				action(message);
			});
		}

		public async Task Publish<T>(T message)
		{
			var channel = typeof(T).Name;
			var json = JsonSerializer.ToJsonString(message);

			await _sub.PublishAsync(channel, json, CommandFlags.FireAndForget);
		}

		public async Task PublishAll<T>(IEnumerable<T> messages)
		{
			foreach(var message in messages)
				await Publish(message);
		}

		public void Dispose() => _sub.UnsubscribeAll(CommandFlags.FireAndForget);
	}

}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.