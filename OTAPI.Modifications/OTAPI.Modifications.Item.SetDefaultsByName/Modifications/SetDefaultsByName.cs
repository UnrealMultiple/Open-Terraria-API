﻿using NDesk.Options;
using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;

namespace OTAPI.Patcher.Modifications.Hooks.Item
{
	public class SetDefaultsByName : ModificationBase
	{
		public override string Description => "Hooking Item.SetDefaults(string)...";
		public override void Run(OptionSet options)
		{
			var vanilla = this.SourceDefinition.Type("Terraria.Item").Methods.Single(
				x => x.Name == "SetDefaults"
				&& x.Parameters.First().ParameterType == this.SourceDefinition.MainModule.TypeSystem.String
			);

			var cbkBegin = this.ModificationDefinition.Type("OTAPI.Core.Callbacks.Terraria.Item").Method("SetDefaultsByNameBegin", parameters: vanilla.Parameters);
			var cbkEnd = this.ModificationDefinition.Type("OTAPI.Core.Callbacks.Terraria.Item").Method("SetDefaultsByNameEnd", parameters: vanilla.Parameters);

			vanilla.Wrap(cbkBegin, cbkEnd, true);
		}
	}
}