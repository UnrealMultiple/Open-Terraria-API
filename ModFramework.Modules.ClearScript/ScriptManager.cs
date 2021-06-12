﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace ModFramework.Modules.ClearScript
{
    public delegate bool FileFoundHandler(string filepath);

    public class JavascriptConsole
    {
        public static void log(string line)
        {
            Console.WriteLine(line);
        }
    }

    class JSScript : IDisposable
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public V8ScriptEngine Container { get; set; }
        public V8Script Script { get; set; }
        public string Content { get; set; }

        public object LoadResult { get; set; }
        public object LoadError { get; set; }

        public ScriptManager Manager { get; set; }

        public JSScript(ScriptManager manager)
        {
            Manager = manager;
        }

        public void Unload()
        {
            Container?.Execute("if(typeof Dispose != undefined) { Dispose(); }");
        }

        public void Dispose()
        {
            if (Script != null) Unload();
            Script?.Dispose();
            Container?.Dispose();
            Script = null;
            FilePath = null;
            FileName = null;
            Container = null;
            Content = null;
            LoadResult = null;
            LoadError = null;
        }

        public void Load()
        {
            try
            {
                if (Container != null) Unload();
                Container?.Dispose();

                Content = File.ReadAllText(FilePath);
                Container = new V8ScriptEngine();

                Container.AddHostType(typeof(Console));
                Container.AddHostType("console", typeof(JavascriptConsole));

                if (Manager.Modder != null)
                {
                    //Container.AddHostObject("Terraria", new HostTypeCollection("Terraria"));
                    Container.AddHostObject("Modder", Manager.Modder);
                }
                else
                {
                    Container.AddHostObject("OTAPI", new HostTypeCollection("OTAPI"));
                    Container.AddHostObject("OTAPIRuntime", new HostTypeCollection("OTAPI.Runtime"));
                }

                Script = Container.Compile(Content);
                LoadResult = Container.Evaluate(Script);
            }
            catch (Exception ex)
            {
                LoadError = ex;
                Console.WriteLine("[JS] Load failed");
                Console.WriteLine(ex);
            }
        }
    }

    public class ScriptManager : IDisposable
    {
        public string ScriptFolder { get; set; }

        public static event FileFoundHandler FileFound;

        private List<JSScript> _scripts { get; } = new List<JSScript>();
        private FileSystemWatcher _watcher { get; set; }

        public MonoMod.MonoModder Modder { get; set; }

        public ScriptManager(
            string scriptFolder,
            MonoMod.MonoModder modder
        )
        {
            ScriptFolder = scriptFolder;
            Modder = modder;
        }

        JSScript CreateScriptFromFile(string file)
        {
            Console.WriteLine($"[JS] Loading {file}");

            var script = new JSScript(this)
            {
                FilePath = file,
                FileName = Path.GetFileNameWithoutExtension(file),
            };

            _scripts.Add(script);

            script.Load();

            return script;
        }

        public void Initialise()
        {
            var scripts = Directory.GetFiles(ScriptFolder, "*.js");
            foreach (var file in scripts)
            {
                if (FileFound?.Invoke(file) == false)
                    continue; // event was cancelled, they do not wish to use this file. skip to the next.

                CreateScriptFromFile(file);
            }
        }

        public bool WatchForChanges()
        {
            try
            {
                _watcher = new FileSystemWatcher(ScriptFolder);
                _watcher.Created += _watcher_Created;
                _watcher.Changed += _watcher_Changed;
                _watcher.Deleted += _watcher_Deleted;
                _watcher.Renamed += _watcher_Renamed;
                _watcher.Error += _watcher_Error;
                _watcher.EnableRaisingEvents = true;

                return true;
            }
            catch (Exception ex)
            {
                var orig = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("[JS] FILE WATCHERS ARE NOT RUNNING");
                Console.WriteLine("[JS] Try running: export MONO_MANAGED_WATCHER=dummy");
                Console.ForegroundColor = orig;
            }
            return false;
        }

        private void _watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("[JS] Error");
            Console.WriteLine(e.GetException());
        }

        private void _watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (!Path.GetExtension(e.FullPath).Equals(".js", StringComparison.CurrentCultureIgnoreCase)) return;
            Console.WriteLine("[JS] Renamed: " + e.FullPath);
            var src = Path.GetFileNameWithoutExtension(e.OldFullPath);
            var dst = Path.GetFileNameWithoutExtension(e.FullPath);

            foreach (var s in _scripts)
            {
                if (s.FileName.Equals(src))
                {
                    s.FileName = dst;
                    s.FilePath = e.FullPath;
                }
            }
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!Path.GetExtension(e.FullPath).Equals(".js", StringComparison.CurrentCultureIgnoreCase)) return;

            var name = Path.GetFileNameWithoutExtension(e.FullPath);
            var matches = _scripts.Where(x => x.FileName == name).ToArray();
            Console.WriteLine($"[JS] Deleted: {e.FullPath} [m:{matches.Count()}]");
            foreach (var script in matches)
            {
                try
                {
                    _scripts.Remove(script);
                    script.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JS] Unload failed {ex}");
                }
            }
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!Path.GetExtension(e.FullPath).Equals(".js", StringComparison.CurrentCultureIgnoreCase)) return;
            var name = Path.GetFileNameWithoutExtension(e.FullPath);
            var matches = _scripts.Where(x => x.FileName == name).ToArray();
            Console.WriteLine($"[JS] Changed: {e.FullPath} [m:{matches.Count()}]");
            foreach (var script in matches)
            {
                script.Load();
            }
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!Path.GetExtension(e.FullPath).Equals(".js", StringComparison.CurrentCultureIgnoreCase)) return;
            Console.WriteLine("[JS] Created: " + e.FullPath);
            CreateScriptFromFile(e.FullPath);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        public void Cli()
        {
            var exit = false;
            do
            {
                Console.WriteLine("[JS] TEST MENU. Press C to exit");
                exit = (Console.ReadKey(true).Key == ConsoleKey.C);
            } while (!exit);
        }
    }
}
