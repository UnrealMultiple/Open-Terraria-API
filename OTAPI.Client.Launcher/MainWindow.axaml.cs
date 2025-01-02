/*
Copyright (C) 2020 DeathCradle

This file is part of Open Terraria API v3 (OTAPI)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OTAPI.Client.Launcher.Targets;
using OTAPI.Common;
using OTAPI.Patcher.Targets;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OTAPI.Client.Launcher;

public partial class MainWindow : Window
{
    private FileSystemWatcher? _watcher;

    MainWindowViewModel Context { get; set; } = new MainWindowViewModel();

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        IPlatformTarget? target = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            target = new WindowsPlatformTarget();

        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            target = new LinuxPlatformTarget();

        else //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            target = new MacOSPlatformTarget();

        //else throw new NotSupportedException();

        Context.LaunchTarget = target;
        target.OnUILoad(Context);

        DataContext = Context;

        var dir = Path.Combine(Environment.CurrentDirectory, "client");
        if (Directory.Exists(dir))
        {
            _watcher = new FileSystemWatcher(dir, "OTAPI.exe");
            _watcher.Created += OTAPI_Changed;
            _watcher.Changed += OTAPI_Changed;
            _watcher.Deleted += OTAPI_Changed;
            _watcher.Renamed += OTAPI_Changed;
            _watcher.EnableRaisingEvents = true;
        }

        foreach (var plugin in Program.Plugins)
        {
            Context.Plugins.Add(plugin);
        }

        if (Program.ConsoleWriter is not null)
            Program.ConsoleWriter.LineReceived += OnConsoleLineReceived;

        PatchMonoMod();
    }

    private void OnConsoleLineReceived(string line)
    {
        Context.Console.Insert(0, $"[{DateTime.Now:yyyyMMdd HH:mm:ss}] {line}");
        System.Diagnostics.Debug.WriteLine($"[OTAPI.Launcher] {line}");
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _watcher?.Dispose();
        _watcher = null;
    }

    private void OTAPI_Changed(object sender, FileSystemEventArgs e)
    {
        Context.LaunchTarget?.OnUILoad(Context);
    }

    public void OnStartVanilla(object sender, RoutedEventArgs e)
    {
        if (Context.InstallPath?.Path is null) return;

        Program.LaunchID = "VANILLA";
        Program.LaunchFolder = Context.InstallPath.Path;
        this.Close();
    }

    public void OnStartOTAPI(object sender, RoutedEventArgs e)
    {
        if (Context.InstallPath?.Path is null) return;

        Program.LaunchID = "OTAPI";
        Program.LaunchFolder = Context.InstallPath.Path;
        this.Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        try
        {
            var target = ClientHelpers.DetermineClientInstallPath(Program.Targets);
            OnInstallPathChange(target);
        }
        catch (DirectoryNotFoundException) { }
    }

    void OnInstallPathChange(ClientInstallPath<IPlatformTarget> target)
    {
        Context.InstallPath = target;
        Context.InstallPathValid = target?.Target?.IsValidInstallPath(target.Path) == true;

        target?.Target?.OnUILoad(Context);

        // try copy the icons from the vanilla installation
        try
        {
            if (Context.InstallPathValid)
            {
                var is_bundle = Path.GetFileName(Environment.CurrentDirectory).Equals("MacOS", StringComparison.CurrentCultureIgnoreCase);
                if (is_bundle)
                {
                    var icon_src = Path.Combine(Context.InstallPath.Path, "Resources", "Terraria.icns");
                    var icon_dst = Path.Combine(Environment.CurrentDirectory, "..", "Resources", "OTAPI.icns");

                    if (File.Exists(icon_dst)) File.Delete(icon_dst);

                    if (File.Exists(icon_src) && !File.Exists(icon_dst))
                        File.Copy(icon_src, icon_dst);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to copy icons");
            Console.WriteLine(ex);
        }
    }

    public async void OnFindExe(object sender, RoutedEventArgs e)
    {
        Context.InstallStatus = string.Empty;

        var fd = new OpenFolderDialog()
        {
            Directory = Context.InstallPath?.Path
        };
        var directory = await fd.ShowAsync(this);

        if (directory is not null && Directory.Exists(directory))
        {
            foreach (var target in Program.Targets)
            {
                if (target.IsValidInstallPath(directory))
                {
                    Context.InstallPath = new ClientInstallPath<IPlatformTarget>()
                    {
                        Path = directory,
                        Target = target,
                    };
                    OnInstallPathChange(Context.InstallPath);
                    return;
                }
            }
        }

        Context.InstallStatus = "Install path is not supported";
    }

    public void OnOpenWorkspace(object sender, RoutedEventArgs e) => OpenFolder(Environment.CurrentDirectory);
    public void OnOpenCSharp(object sender, RoutedEventArgs e) => OpenFolder(Path.Combine(Environment.CurrentDirectory, "csharp", "plugins"));
    public void OnOpenJavascript(object sender, RoutedEventArgs e) => OpenFolder(Path.Combine(Environment.CurrentDirectory, "clearscript"));
    public void OnOpenLua(object sender, RoutedEventArgs e) => OpenFolder(Path.Combine(Environment.CurrentDirectory, "lua"));

    public void OpenFolder(string folder)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = folder;
        process.Start();
    }

    System.Threading.CancellationTokenSource CancellationTokenSource { get; set; } = new System.Threading.CancellationTokenSource();

    public void OnInstall(object sender, RoutedEventArgs e)
    {
        if (Context.IsInstalling || Context.InstallPath?.Path is null || Context.LaunchTarget is null) return;
        Context.IsInstalling = true;

        Task.Run(async () =>
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource = new();
            try
            {
                var target = new PCClientTarget();
                target.InstallPath = Context.InstallPath.Path;

                target.StatusUpdate += (sender, e) => Context.InstallStatus = e.Text;
                target.Patch();
                Context.InstallStatus = "Patching completed, installing to existing installation...";

                Context.InstallPath.Target.StatusUpdate += (sender, e) => Context.InstallStatus = e.Text;
                await Context.InstallPath.Target.InstallAsync(Context.InstallPath.Path, CancellationTokenSource.Token);

                Context.InstallStatus = "Install completed";

                Context.IsInstalling = false;
                Context.LaunchTarget.OnUILoad(Context);

                Console.WriteLine("Patching completed");
            }
            catch (System.Exception ex)
            {
                Context.InstallStatus = "Err: " + ex.ToString();
                OnConsoleLineReceived(ex.ToString());
            }
        });
    }

    /// <summary>
    /// Current MonoMod is outdated, and the new reorg is not ready yet, however we need v25 RD for NET9, yet Patcher v22 is the latest, and is not compatible with v25.
    /// Ultimately the problem is OTAPI Client using both at once, unlike the server setup which doesnt.
    /// For now, the intention is to replace the entire both with "return new string[0];" to prevent the GAC IL from being used (which it isn't anyway)
    /// </summary>
    void PatchMonoMod()
    {
        var bin = File.ReadAllBytes("MonoMod.dll");
        using MemoryStream ms = new(bin);
        var asm = AssemblyDefinition.ReadAssembly(ms);
        var modder = asm.MainModule.Types.Single(x => x.FullName == "MonoMod.MonoModder");
        var gacPaths = modder.Methods.Single(m => m.Name == "get_GACPaths");
        var il = gacPaths.Body.GetILProcessor();
        if (il.Body.Instructions.Count != 3)
        {
            il.Body.Instructions.Clear();
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newarr, asm.MainModule.ImportReference(typeof(string)));
            il.Emit(OpCodes.Ret);

            // clear MonoModder.MatchingConditionals(cap, asmName), with "return false;"
            var mc = modder.Methods.Single(m => m.Name == "MatchingConditionals" && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.Name == "AssemblyNameReference");
            il = mc.Body.GetILProcessor();
            mc.Body.Instructions.Clear();
            mc.Body.Variables.Clear();
            mc.Body.ExceptionHandlers.Clear();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);

            var writerParams = modder.Methods.Single(m => m.Name == "get_WriterParameters");
            il = writerParams.Body.GetILProcessor();
            var get_Current = writerParams.Body.Instructions.Single(x => x.Operand is MethodReference mref && mref.Name == "get_Current");
            // replace get_Current with a number, and remove the bitwise checks
            il.Remove(get_Current.Next);
            il.Remove(get_Current.Next);
            il.Replace(get_Current, Instruction.Create(
                OpCodes.Ldc_I4, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 37 : 0
            ));

            asm.Write("MonoMod.dll");
        }
    }
}
