using System;
using System.Collections.Generic;
using System.Linq;

namespace Dominium
{
	public abstract class ValueObject : IEquatable<ValueObject>
	{
		protected abstract IEnumerable<object> GetEqualityComponents();

		public bool Equals(ValueObject other)
			=> Equals((object) other);

		public override bool Equals(object obj)
		{
			if(obj == null)
				return false;

			if(GetType() != obj.GetType())
				throw new ArgumentException($"Invalid comparison of Value Objects of different types: {GetType()} and {obj.GetType()}");

			var valueObject = (ValueObject) obj;

			return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
		}

		public override int GetHashCode()
			=> GetEqualityComponents()
				.Aggregate(1, (current, obj) => { return HashCode.Combine(current, obj); });

		public static bool operator ==(ValueObject a, ValueObject b)
		{
			if(ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return true;

			if(ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;

			return a.Equals(b);
		}

		public static bool operator !=(ValueObject a, ValueObject b)
			=> !(a == b);
	}
}

// Source: https://enterprisecraftsmanship.com/2017/08/28/value-object-a-better-implementation/