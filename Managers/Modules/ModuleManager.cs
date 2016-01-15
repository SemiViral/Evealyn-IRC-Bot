using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Eve.Managers.Modules
{
    public class ModuleManager
    {
		/// <summary>
		///     Loads all Type assemblies in ./modules/ into memory
		/// </summary>
		/// <returns>void</returns>
		public Dictionary<string, Type> LoadModules(Dictionary<string, string> commands) {
			var modules = new Dictionary<string, Type>();
			string modulesPath = Path.GetFullPath(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName + "\\Modules\\Default\\");

			if (!Directory.Exists(modulesPath)) {
				Console.WriteLine("||| /Modules/Default/ directory not found. Creating directory.");
				Directory.CreateDirectory(modulesPath);
			}

			try {
				foreach (KeyValuePair<string, Type> kvp in Assembly.LoadFrom(Directory.EnumerateFiles(modulesPath, "Eve.Core.dll", SearchOption.AllDirectories).First()).GetTypes()
					.Select(t => CheckTypeAndLoad(t, modules, commands)).Where(kvp => !kvp.Equals(default(KeyValuePair<string, Type>))))
					modules.Add(kvp.Key, kvp.Value);

				foreach (KeyValuePair<string, Type> kvp in from f in Directory.EnumerateFiles(modulesPath, "Eve.*.dll", SearchOption.AllDirectories)
					let r = new RecursiveAssemblyLoader()
					select r.GetAssembly(Path.GetFullPath(f))
					into file
					from t in file.GetTypes()
					select CheckTypeAndLoad(t, modules, commands)
					into kvp
					where !kvp.Equals(default(KeyValuePair<string, Type>))
					select kvp)
					modules.Add(kvp.Key, kvp.Value);
			} catch (ReflectionTypeLoadException ex) {
				StringBuilder sb = new StringBuilder();

				foreach (Exception exSub in ex.LoaderExceptions) {
					sb.AppendLine(exSub.Message);
					FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
					if (!string.IsNullOrEmpty(exFileNotFound?.FusionLog)) {
						sb.AppendLine("Fusion Log:");
						sb.AppendLine(exFileNotFound.FusionLog);
					}

					sb.AppendLine();
				}
				string errorMessage = sb.ToString();
				Console.WriteLine(errorMessage);
			}

			Console.WriteLine($"||| Loaded modules: {string.Join(", ", modules.Keys)}");
			return modules;
		}

	    /// <summary>
	    ///     Handles interface checks on the Types and adds them to the module list.
	    ///     Commands are also added to list.
	    /// </summary>
	    /// <param name="type">Type to be checked against IModule interface</param>
	    /// <param name="checker">Dictionary to be checked against</param>
	    /// <param name="commands">dictionary to pass commands to</param>
	    private KeyValuePair<string, Type> CheckTypeAndLoad(Type type, Dictionary<string, Type> checker, IDictionary<string, string> commands) {   
			if (type.GetInterface("IModule") == null
			|| !type.GetInterface("IModule").IsEquivalentTo(typeof(IModule))
			|| checker.ContainsValue(type))
			return new KeyValuePair<string, Type>();

			Dictionary<string, string> def = ((IModule)Activator.CreateInstance(type)).Def;
			if (def == null) return new KeyValuePair<string, Type>(type.Name.ToLower(), type);

			foreach (KeyValuePair<string, string> kvp in def.Where(kvp => !commands.Contains(kvp)))
			    commands.Add(kvp.Key, kvp.Value);

			return new KeyValuePair<string, Type>(type.Name.ToLower(), type);
		}
	}

	internal class RecursiveAssemblyLoader : MarshalByRefObject {
		public Assembly GetAssembly(string path) {
			return Assembly.LoadFrom(path);
		}
	}
}
