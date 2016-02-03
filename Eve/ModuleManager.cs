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
		///     Loads all Type assemblies in ./Modules/ into memory
		/// </summary>
		/// <returns>void</returns>
		public static List<IModule> LoadModules() {
			const string modulesPath = "Modules";

			if (!Directory.Exists(modulesPath)) {
				Console.WriteLine($"||| {modulesPath} directory not found. Creating directory.");
				Directory.CreateDirectory(modulesPath);
			}

			var modules = new List<IModule>();

			try {
				//modules.AddRange(Directory.EnumerateFiles(modulesPath, "Eve.*.dll", SearchOption.AllDirectories).Select(CheckTypeAndLoad));
				foreach (string f in Directory.EnumerateFiles(modulesPath, "Eve.*.dll", SearchOption.AllDirectories)) {
					modules.AddRange(Loader.LoadPlugins(f));
				}
				
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

			//Console.Write(modules.Any() ? $"||| Loaded modules: {string.Join(", ", modules.Select(e => e.Types.Select(f=> f.Name)))}\n" : null);
			return modules;
		}

		/// <summary>
		///     Handle the interface check on a Type and return the Module object.
		/// </summary>
		/// <param name="filepath">path of DLL to be loadded</param>
		private static Module CheckTypeAndLoad(string filepath) {
			string name = Path.GetFileNameWithoutExtension(filepath);

			AppDomain domain = AppDomain.CreateDomain(name);
			Console.WriteLine(filepath);
			AssemblyName assemblyName = new AssemblyName {
				CodeBase = filepath
			};

			return new Module(name, domain, assemblyName);
		}
	}

	internal class Loader : MarshalByRefObject {
		public static List<IModule> LoadPlugins(string assemblyName) {
			Assembly assembly = Assembly.Load(assemblyName);
			IEnumerable<Type> types = from type in assembly.GetTypes()
				where typeof(IModule).IsAssignableFrom(type)
				select type;

			return types.Select(e => (IModule) Activator.CreateInstance(e)).ToList();
		}
	}
}