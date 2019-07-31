using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dominium
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DomainKeyAttribute : Attribute
	{
		public IEnumerable<string> Keys { get; }

		public DomainKeyAttribute(params string[] keys)
			=> Keys = keys;

		public static IEnumerable<string> FromType<T>()
		{
			var attribute = typeof(T).GetCustomAttribute<DomainKeyAttribute>();
			return attribute.Keys;
		}
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.
