﻿using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OTAPI.Patcher
{
    public class Program
    {
        static Engine.Patcher patcher;
        static OptionSet options;

        public static void Main(String[] args)
        {
            string sourceAsm = null;
            string modificationGlob = null;
            string outputPath = null;
            List<string> mergeInputs = new List<string>();
            string mergeOutput = null;

            Console.WriteLine("Open Terraria API v2.0");

            if (args.Length == 0)
            {
#if SLN_CLIENT
				args = new[]
				{
					@"-in=../../../wrap/Terraria/Terraria.exe",
					@"-mod=../../../OTAPI.Modifications/OTAPI.**/bin/Debug/OTAPI.**.dll",
					@"-o=../../../OTAPI.dll"
				};
#else
                args = new[]
                {
                    @"-pre-merge-in=../../../wrap/TerrariaServer/TerrariaServer.exe",
                    @"-pre-merge-in=../../../wrap/TerrariaServer/ReLogic.dll",
                    @"-pre-merge-out=../../../TerrariaServer.dll",
                    @"-in=../../../TerrariaServer.dll",
                    @"-mod=../../../OTAPI.Modifications/OTAPI.**/bin/Debug/OTAPI.**.dll",
                    @"-o=../../../OTAPI.dll"
                };
#endif
            }

            options = new OptionSet();
            options.Add("in=|source=", "specifies the source assembly to patch",
                op => sourceAsm = op);
            options.Add("mod=|modifications=", "Glob specifying the path to modification assemblies that will run against the target assembly.",
                op => modificationGlob = op);
            options.Add("o=|output=", "Specifies the output assembly that has had all modifications applied.",
                op => outputPath = op);
            options.Add("pre-merge-in=", "Specifies an assembly to be combined before any modifications are applied",
                op => mergeInputs.Add(op));
            options.Add("pre-merge-out=", "Specifies the output file of combined assemblies before any modifications are applied",
                op => mergeOutput = op);

            options.Parse(args);

            if (string.IsNullOrEmpty(sourceAsm) == true
                || string.IsNullOrEmpty(modificationGlob) == true)
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var roptions = new ILRepacking.RepackOptions()
            {
                //Get the list of input assemblies for merging, ensuring our source 
                //assembly is first in the list so it can be granted as the target assembly.
                InputAssemblies = mergeInputs.ToArray(),

                OutputFile = mergeOutput,
                TargetKind = ILRepacking.ILRepack.Kind.Dll,

                //Setup where ILRepack can look for assemblies
                SearchDirectories = mergeInputs
                    .Select(x => Path.GetDirectoryName(x))
                    .Concat(new[] { Environment.CurrentDirectory })
                    .Distinct()
                    .ToArray(),
                Parallel = true,
                //Version = this.SourceAssembly., //perhaps check an option for this. if changed it should not allow repatching
                CopyAttributes = true,
                XmlDocumentation = true,
                UnionMerge = true,

#if DEBUG
                DebugInfo = true
#endif
            };

            var repacker = new ILRepacking.ILRepack(roptions);
            repacker.Repack();

            patcher = new Engine.Patcher(sourceAsm, new[] { modificationGlob }, outputPath);
            patcher.Run();
        }
    }
}
