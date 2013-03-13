using System;

namespace SimonWaite.IoC
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple=false, Inherited=true)]
	public class ImportManyAttribute : ImportAttribute
	{
		public ImportManyAttribute() : base()
		{
		
		}
		public ImportManyAttribute (Type type) : base(type)
		{
		}
		
		
		public override string ToString ()
		{
			return string.Format ("[ImportManyAttribute: Interface='{0}']", null == Interface ? "<null>" : Interface.FullName );
		}
	}
}

