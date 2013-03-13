using System;

namespace SimonWaite.IoC
{
	[AttributeUsage(AttributeTargets.Field, Inherited=true, AllowMultiple=false)]
	public class ImportAttribute : Attribute
	{
		private Type iface = null;
		
		public ImportAttribute ()
		{
			
		}
		
		public ImportAttribute (Type type)
		{
			
		}
		
		public Type Interface {
			get { return iface;}
			set {
				
				Type type = value;
				if (!type.IsInterface)
					throw new NotSupportedException (string.Format ("Type: '{0}' is not an interface.", type.FullName));
				iface = type;
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[ImportAttribute: Interface='{0}']", null == Interface ? "<null>" : Interface.FullName);
		}
	}
}

