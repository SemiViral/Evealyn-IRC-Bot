using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Module = Eve.Types.Module;

namespace Eve {
	public class ModuleManager {
		/// <summary>
		///     Loads all Type assemblies in ./modules/ into memory
		/// </summary>
		/// <returns>void</returns>
		public static List<Module> LoadModules() {
			const string modulesPath = "Modules";

			if (!Directory.Exists(modulesPath)) {
				Console.WriteLine($"||| {modulesPath} directory not found. Creating directory.");
				Directory.CreateDirectory(modulesPath);
			}

			var modules = new List<Module>();

			try {
				// Do checks on loading process and discovered types
				//foreach (string f in Directory.EnumerateFiles(modulesPath, "Eve.*.dll", SearchOption.AllDirectories)) {
				//	Console.WriteLine(f);

				//	RecursiveAssemblyLoader r = new RecursiveAssemblyLoader();
				//	Assembly a = r.GetAssembly(f);
				//	foreach (Type t in a.GetTypes()) {
				//		Console.WriteLine(t.Name + (typeof(IModule).IsAssignableFrom(t) ? " : true" : " : false"));
				//	}
				//}

				foreach (Module m in from f in Directory.EnumerateFiles(modulesPath, "*.dll", SearchOption.AllDirectories)
					let r = new RecursiveAssemblyLoader()
					select r.GetAssembly(f)
					into a
					where a != null
					from t in a.GetTypes()
					select CheckTypeAndLoad(t)
					into m
					where m != null &&
						  !modules.Contains(m)
					select m)
					modules.Add(m);
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
			} catch (InvalidOperationException) {
				Console.WriteLine("||| No modules to load.");
			}

			Console.Write(modules.Any() ? $"||| Loaded modules: {string.Join(", ", modules.Select(e => e.Name))}\n" : null);
			return modules;
		}

		/// <summary>
		///     Handles interface checks on the Types and adds them to the module list.
		///     Commands are also added to list.
		/// </summary>
		/// <param name="type">Type to be checked against IModule interface</param>
		private static Module CheckTypeAndLoad(Type type) {
			if (type.GetInterface("IModule") == null ||
				!type.GetInterface("IModule").IsEquivalentTo(typeof(IModule))
				)
				return null;

			Dictionary<string, string> def = ((IModule) Activator.CreateInstance(type)).Def;
			return def == null ? new Module(type.Name, null, null, type) : new Module(type.Name, def.Keys.First(), def.Values.First(), type);
		}
	}

	internal class RecursiveAssemblyLoader : MarshalByRefObject {
		public Assembly GetAssembly(string path) {
			try {
				return Assembly.LoadFrom(path);
			} catch (BadImageFormatException) {
				return null;
			}
		}
	}
}