using System;
using System.Collections.Generic;

namespace SimonWaite.IoC
{
	public interface IContainer
	{
		void Register (IEnumerable<Type> types);

		void Register (Type type);

		object Resolve (Type iface);

		T ResolveAs<T> ()  where T : class;

		object[] ResolveMany (Type iface);

		T[] ResolveManyAsArray<T> () where T : class;

		List<T> ResolveManyAsList<T> () where T : class;

		IEnumerable<T> ResolveManyAsIEnumerable<T> () where T : class;

		void ResolveFields (object obj);

        string DumpTypes();
	}
}

