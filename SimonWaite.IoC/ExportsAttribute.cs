using System;

namespace SimonWaite.IoC
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
	public class ExportsAttribute : Attribute
	{
		public ExportsAttribute (Type type)
		{
			if (!type.IsInterface)
				throw new NotSupportedException (string.Format ("Type: '{0}' is not an interface.", type.FullName));
			Interface = type;
		}
		
		public Type Interface {
			get;
			set;
		}
		
		
		public override string ToString ()
		{
			return string.Format ("[ExportsAttribute: Interface={0}]", Interface);
		}
	}
}

