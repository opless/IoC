using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimonWaite.IoC
{
	[Exports(typeof(IContainer))]
	[Singleton]
	public class Container : IDisposable, IContainer 
	{
		/*
		  I might want a service that contains a IFoo, but there might be multiple classes that implement IFoo...
		 */
		// exported interface
		Dictionary<Type,List<Type>> exports = new Dictionary<Type, List<Type>> ();
		Dictionary<Type,object> singletons = new Dictionary<Type, object> ();
		//Dictionary<Type,List<ExportsAttribute>> registeredExports = new Dictionary<Type, List<ExportsAttribute>> ();
		bool disposed = false;
		
		public Container ()
		{
			// register myself
			Register(typeof(Container));
			singletons[this.GetType()] = this;
			//Console.WriteLine ("IContainer = {0} {1}",this.GetHashCode(), singletons [typeof(Container)] ?? "<null>");
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

        // 
		
		protected void Dispose (bool disposing)
		{
			if (!disposed) {
				if (disposing) {
					if (singletons != null) {
						singletons = null;
					}
				}

				disposed = true;
			}
			
		}
		#endregion	
		
		
		#region Register	
		public void Register (IEnumerable<Type> types)
		{
			if (null == types)
				throw new ArgumentNullException ();
				
			if (disposed)
				throw new ObjectDisposedException ("Container");
				
			foreach (Type t in types) {
				Register (t);
			}
		}

        public void RegisterSingleton(object o)
        {
            Type t = o.GetType();
            Register(t);
            if (!singletons.ContainsKey(t))
            {
                Register(t);
                if (singletons [t] == null)
                {
                    singletons [t] = o;
                    return;
                }
            }
            throw new NotSupportedException("Manual Singleton Injection Failed.");
        }
		
		public void Register (Type type)
		{
			//Console.WriteLine ("Registering Type: {0}",type.FullName);
			
			if (null == type)
				throw new ArgumentNullException ();

			//if (!type.IsInterface)
			//	throw new ArgumentException ("Not an Interface: '" + type.FullName + "'");
				
			if (disposed)
				throw new ObjectDisposedException ("Container");
							
			// examine type for an Exports Attribute

			foreach (object exportCandidate in type.GetCustomAttributes(true)) {
				ExportsAttribute export = exportCandidate as ExportsAttribute;
				
				if (null != export) {
				
					if (!exports.ContainsKey (export.Interface)) {
						exports.Add (export.Interface, new List<Type> ());
					}
					exports [export.Interface].Add (type);
				}
				
				SingletonAttribute singleton = exportCandidate as SingletonAttribute;
				if (null != singleton) {
					//Console.WriteLine  ("Candidate is singleton: " + type.FullName);
					singletons.Add (type, null);
				}
			}
		}
#endregion
		
		
		#region Resolve One
		public object Resolve (Type iface)
		{
			List<Type> collection = GetTypesForInterface (iface);
						
			if (collection.Count > 1)
				throw new NotSupportedException ("Multiple Registrations found for interface: " + iface.FullName);
			
			return Instantiate (collection [0]);	
			
			//	throw new NotSupportedException("Interface '"+export.Interface.FullName+"' has multiple candidate")
		}
		
		public T ResolveAs<T> ()  where T : class
		{
			return (T)Resolve (typeof(T));
		}
		
		private List<Type> GetTypesForInterface (Type iface)
		{
			if (null == iface)
				throw new ArgumentNullException ();
				
			if (!iface.IsInterface)
				throw new ArgumentException ("Not an Interface: '" + iface.FullName + "'");
					
			if (disposed)
				throw new ObjectDisposedException ("Container");
				
			if (!exports.ContainsKey (iface))
				throw new NotSupportedException ("Interface '" + iface.FullName + "' not registered."+Environment.NewLine+DumpTypes());
			
			return exports [iface];
		}
		
		#endregion
		
		#region ResolveMany ...
		public object[] ResolveMany (Type iface)
		{
			List<Type> collection = GetTypesForInterface (iface);
			object[] many = new object[collection.Count];
			
			for (int i=0; i < collection.Count; i ++) {
				many [i] = Instantiate (collection [i]);
			}
			return many;
		}
		
		public T[] ResolveManyAsArray<T> () where T : class
		{
			List<Type> collection = GetTypesForInterface (typeof(T));
			T[] many = new T[collection.Count];
			
			for (int i=0; i < collection.Count; i ++) {
				many [i] = (T)Instantiate (collection [i]);
			}
			return many;
		
		}
		
		public List<T> ResolveManyAsList<T> () where T : class
		{
			List<Type> collection = GetTypesForInterface (typeof(T));
			List<T> many = new List<T> (collection.Count); // exact sized container
			for (int i=0; i < collection.Count; i++) {
				many.Add ((T)Instantiate (collection [i]));
			}
			return many;		
		}
		
		public IEnumerable<T> ResolveManyAsIEnumerable<T> () where T : class
		{
			return ResolveManyAsList<T> ();
		}
		#endregion
		
		#region Instantiate
		private object[] Instantiate (IEnumerable<Type> types)
		{
			List<object> objects = new List<object> ();
			foreach (Type type in types) {
				objects.Add (Instantiate (type));
			}
			return objects.ToArray ();
		}

		private object Instantiate (Type type)
		{
			object obj;
			//Console.WriteLine  ("Creating a {0}", type.FullName);
			
			if (singletons.ContainsKey (type)) {
				//Console.WriteLine  ("  Singleton");
				if (null == singletons [type]) {
					obj = InstantiateImpl (type);
					//Console.WriteLine  ("  Created new instance: "+obj.GetHashCode());
					singletons [type] = obj;
					Resolver (obj);
				} else {
					obj = singletons [type];
					//Console.WriteLine  ("  Found instance: " + obj.GetHashCode ());
				}
			} else {
				obj = InstantiateImpl (type);
				//Console.WriteLine  ("  Created new instance: " + obj.GetHashCode ());
				Resolver (obj);
			}
			
			return obj;
		}

		private void Resolver (object obj)
		{
			// but wait, perhaps the service depends on other things?
			// we ought to fill those in too.
			// these will be fields only.
			ResolveFields (obj);
		}

		private object InstantiateImpl (Type type)
		{
			object obj = Activator.CreateInstance (type);
		
			return obj;
		}
		#endregion
		
		public void ResolveFields (object obj)
		{
			//Console.WriteLine  ("Resolving Fields for: {0}({1})", obj.GetType ().FullName, obj.GetHashCode ());
			FieldInfo[] fields = obj.GetType ().GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
			foreach (FieldInfo fi in fields) {
				//Console.WriteLine  ("Scanning {0}({1}).{2}", obj.GetType ().FullName, obj.GetHashCode (), fi.Name);
				// if field is not null, then we've been here before.
				object val = fi.GetValue (obj);
				if (null != val)
					continue;
				foreach (object attr in fi.GetCustomAttributes(true)) {
					ImportManyAttribute many = attr as ImportManyAttribute;
					if (null != many) {
						//Console.WriteLine  ("..SetFieldMany");
						SetFieldMany (obj, fi, many);
						break;
					} 
					ImportAttribute one = attr as ImportAttribute;
					if (null != one) {
						//Console.WriteLine  ("..SetField");
						SetField (obj, fi, one);
						break;
					}
				}
			}
		}
		
		private Type GetInterfaceForSetField (Type type, ImportAttribute one)
		{
			if (type.IsArray)
				throw new ArgumentException ("Type " + type.FullName + " is marked as [Import] rather than [ImportMany] ");
			Type iface;
			if (null == one.Interface) {
				if (!type.IsInterface)
					throw new NotSupportedException (string.Format ("Type: '{0}' is not an interface.", type.FullName));
				iface = type;
			} else {
				iface = one.Interface;
			}
			return iface;
		}
		
		private void SetField (object obj, FieldInfo fi, ImportAttribute one)
		{
			Type iface = GetInterfaceForSetField (fi.FieldType, one);
			fi.SetValue (obj, Resolve (iface));
		}
		
		private object CallResolveManyAs (string suffix, Type t)
		{
			MethodInfo method = typeof(Container).GetMethod ("ResolveManyAs" + suffix);
			MethodInfo generic = method.MakeGenericMethod (t);
			return generic.Invoke (this, null);
		}
		
		private void SetFieldMany (object obj, FieldInfo fi, ImportManyAttribute many)
		{
			object val;
			Type ft = fi.FieldType;
			// check for array[]
			if (ft.IsArray && ft.HasElementType && ft.GetArrayRank () == 1) {
				val = CallResolveManyAs ("Array", ft.GetElementType ());
				fi.SetValue (obj, val);
				return;
			}
			/* appears to no longer work.
			// check for List<IFoo>
			if (ft.IsGenericType && ft.IsGenericTypeDefinition && ft.GetGenericTypeDefinition () == typeof(List<>)) {
				val = CallResolveManyAs ("List", ft.GetGenericArguments () [0]);			
				fi.SetValue (obj, val);
				return;
			}
			
			// check for IEnumerable<IFoo>
			if (ft.IsGenericType && ft.IsGenericTypeDefinition && ft.GetGenericTypeDefinition () == typeof(IEnumerable<>)) {
				val = CallResolveManyAs ("IEnumerable", ft.GetGenericArguments () [0]);
				fi.SetValue (obj, val);
				return;
			}
			*/
			if (ft.IsGenericType && ft.IsGenericType) {
				// check for List<IFoo>
				if (ft.GetGenericTypeDefinition () == typeof(List<>)) {
					val = CallResolveManyAs ("List", ft.GetGenericArguments () [0]);			
					fi.SetValue (obj, val);
					return;
				}
			
				// check for IEnumerable<IFoo>
				if (ft.GetGenericTypeDefinition () == typeof(IEnumerable<>)) {
					val = CallResolveManyAs ("IEnumerable", ft.GetGenericArguments () [0]);
					fi.SetValue (obj, val);
					return;
				}
			}
			throw new NotSupportedException ("Field '" + obj.GetType ().FullName + "::" + fi.Name + "' cannot be populated by this container.");			
			
		}

        public string DumpTypes()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Container has following registered types (#"+exports.Count+"):");
            int i =0;
            foreach (var type in this.exports.Keys)
            {
                i++;
                var bucket = exports[type];
                if(bucket == null)
                    bucket = new List<Type>();
                sb.AppendFormat("{0}: {1} [{2}] #{3}",
                                i,
                                type.FullName,
                                singletons.ContainsKey(type) ? singletons[type].GetHashCode().ToString("X8"):"Instance",
                                bucket.Count);
                sb.AppendLine();
                for(int j = 0; j < bucket.Count; j++)
                {
                    sb.AppendLine("  "+j+": "+bucket[j].FullName);
                }
            }
            return sb.ToString();
        }
	}
}

