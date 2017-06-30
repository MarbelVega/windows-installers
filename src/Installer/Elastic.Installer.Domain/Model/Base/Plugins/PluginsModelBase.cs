﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Configuration.Plugin;
using FluentValidation;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base.Plugins
{
	public abstract class PluginsModelBase<TModel, TModelValidator> : StepBase<TModel, TModelValidator>
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		public IPluginStateProvider PluginStateProvider { get; }
		protected ReactiveList<Plugin> _plugins = new ReactiveList<Plugin> { ChangeTrackingEnabled = true };
		public const string UnchangedMoniker = "__unchanged__";

		protected bool AlreadyInstalled { get; set; }
		protected string InstallDirectory { get; set; }
		protected string ConfigDirectory { get; set; }

		protected abstract IEnumerable<Plugin> GetPlugins();

		public ReactiveList<Plugin> AvailablePlugins
		{
			get => _plugins;
			set => this.RaiseAndSetIfChanged(ref _plugins, value);
		}

		[StaticArgument(nameof(Plugins))]
		public IEnumerable<string> Plugins
		{
			get { return _plugins.Where(p => p.Selected).Select(p => p.Url); }
			set
			{
				if (value == null) return;
				var plugins = value.ToList();
				if (plugins.Count == 1 && plugins[0] == UnchangedMoniker) return;
				
				foreach (var p in AvailablePlugins) p.Selected = false;
				
				foreach (var p in AvailablePlugins.Where(p => value.Contains(p.Url)))
					p.Selected = true;
			}
		}

		protected PluginsModelBase(IPluginStateProvider pluginStateProvider)
		{
			this.Header = "Plugins";
			this.PluginStateProvider = pluginStateProvider;
		}

		public override void Refresh()
		{
			this.AvailablePlugins.Clear();
			var plugins = this.GetPlugins();
			this.AvailablePlugins.AddRange(plugins);
			var selectedPlugins = !this.AlreadyInstalled
				? this.DefaultPlugins()
				: this.PluginStateProvider.InstalledPlugins(this.InstallDirectory, this.ConfigDirectory).ToList();
			foreach (var plugin in this.AvailablePlugins.Where(p => selectedPlugins.Contains(p.Url)))
				plugin.Selected = true;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(this.GetType().Name);
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(Plugins)} = " + string.Join(", ", Plugins));
			return sb.ToString();
		}

		protected virtual List<string> DefaultPlugins() => new List<string>() {UnchangedMoniker};
	}
}