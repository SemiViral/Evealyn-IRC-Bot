using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Eve.Types;

namespace Eve {
	[Serializable]
	public class ModuleManager {
		internal List<Module> Modules;

		internal ModuleManager(ref Dictionary<string, string> commandList) {
			LoadModules(ref commandList);
		}

		/// <summary>
		///     Loads all Type assemblies in ./Modules/ into memory
		/// </summary>
		/// <returns>void</returns>
		private void LoadModules(ref Dictionary<string, string> definitions) {
			const string modulesPath = "Modules";

			if (!Directory.Exists(modulesPath)) {
				Utils.Output($"{modulesPath} directory not found. Creating directory.");
				Directory.CreateDirectory(modulesPath);
			}
			
			Modules = new List<Module>();

			try {
				AppDomain domain = AppDomain.CreateDomain("Modules");

				foreach (string file in Directory.EnumerateFiles(modulesPath, "Eve.*.dll", SearchOption.AllDirectories)) {
					Module loader = (Module) domain.CreateInstanceAndUnwrap(
					typeof(Module).Assembly.FullName,
					typeof(Module).FullName);
					loader.LoadAssembly(file, ref definitions);

					if (Modules.Contains(loader)) continue;
					Modules.Add(loader);
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
				Utils.Output("No modules to load.");
			}
		}

		public List<string> GetModules() {
			return Modules.SelectMany(e => e.Types).Select(e => e.Name).ToList();
		}
	}

	internal class Module : MarshalByRefObject {
		private Assembly _assembly;

		public List<AssemblyType> Types = new List<AssemblyType>();

		public override object InitializeLifetimeService() {
			return null;
		}

		public void LoadAssembly(string path, ref Dictionary<string, string> definitions) {
			_assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
			Utils.Output($"Loaded module: {_assembly.FullName}");

			foreach (AssemblyType atype in _assembly.GetTypes().Where(e => e.GetInterface("IModule") != null).Select(type => new AssemblyType {
				Name = type.FullName.Split('.').Last(),
				Type = type
			}).Where(atype => !Types.Contains(atype))) {
				Types.Add(atype);
			}

			if (!Types.Any()) return;

			definitions = definitions.AddFrom(Types.SelectMany(type => ((IModule) Activator.CreateInstance(type.Type)).Def).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
			Utils.Output($"— Loaded submodules: {string.Join(", ", Types.Select(e => e.Name))}");
		}

		/// <summary>
		/// Executes onChannelMessage from module's type
		/// </summary>
		/// <returns></returns>
		public List<ChannelMessage> OnChannelMessageIterate(ChannelMessage c, PassableMutableObject propRef) {
			//MethodInfo method = type.GetMethod("OnChannelMessage", BindingFlags.Public);
			//returns.Add(method?.Invoke(null, parameters));
			//return Types.Select(e => e.Type).Select(type => ((IModule) Activator.CreateInstance(type)).OnChannelMessage(c, propRef)).Cast<object>().ToList();
			var returns = new List<ChannelMessage>();
			returns.AddRange(
				Types.Select(e => e.Type)
					.Select(type => ((IModule) Activator.CreateInstance(type)).OnChannelMessage(c, propRef))
					.Where(instance => !returns.Contains(instance)));

			return returns;
		}
	}

	[Serializable]
	internal class AssemblyType {
		public string Name { get; set; }
		public Type Type { get; set; }
	}
}