using System;

namespace SimonWaite.IoC
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
	public class SingletonAttribute : Attribute
	{
		public SingletonAttribute ()
		{
		}
	}
}

