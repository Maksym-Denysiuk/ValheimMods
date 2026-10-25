// HomeCheat - BepInEx client plugin for Maksym's own Valheim game and world. Architecture: docs/homecheat-architecture.md (repo root).
// This class only wires the services: file inbox -> dispatcher -> registry of commands (built-ins + command DLLs), timers.
// It never calls the console cheat path (Terminal/Console commands), on purpose. No network port.
// C# 5 (csc 4.0): no ?., no $"", no expression bodies.
using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using HomeCheat.Builtins;
using HomeCheat.Services;

namespace HomeCheat
{
    [BepInPlugin("com.homeos.homecheat", "HomeCheat", "0.2.0")]
    public sealed class HomeCheatPlugin : BaseUnityPlugin
    {
        CommandRegistry registry;
        Scheduler scheduler;
        FileInboxChannel inbox;

        void Awake()
        {
            HomeApi.InfoSink = delegate (string m) { Logger.LogInfo(m); };
            HomeApi.ErrorSink = delegate (string m) { Logger.LogError(m); };

            // command DLLs reference this assembly: hand them the one that is already loaded
            AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs e)
            {
                return e.Name.StartsWith("HomeCheat,") ? typeof(HomeCheatPlugin).Assembly : null;
            };

            scheduler = new Scheduler();
            registry = new CommandRegistry(Path.Combine(Paths.BepInExRootPath, "homecheat-commands"));
            CommandDispatcher dispatcher = new CommandDispatcher(registry);
            HomeApi.Timers = scheduler;
            HomeApi.Registry = registry;
            HomeApi.Dispatcher = dispatcher;
            inbox = new FileInboxChannel(Path.Combine(Paths.ConfigPath, "homecheat"), dispatcher.Exec);

            try { new Harmony("com.homeos.homecheat").CreateClassProcessor(typeof(TreeScalePatch)).Patch(); }
            catch (Exception e) { Logger.LogError("Harmony: " + e); }

            registry.RegisterBuiltin(new PingCommand());
            registry.RegisterBuiltin(new StatusCommand());
            registry.RegisterBuiltin(new ReloadCommand(registry));
            registry.RegisterBuiltin(new CommandsCommand(registry));
            registry.RegisterBuiltin(new HelpCommand(registry));
            registry.RegisterBuiltin(new HarmEffectCommand(registry));

            Logger.LogInfo("HomeCheat watching " + inbox.Inbox + " (no network port). Drop <name>.cmd, read outbox/<name>.out");
            try { Logger.LogInfo(registry.Reload()); } catch (Exception e) { Logger.LogError(e); }
        }

        void Update()
        {
            scheduler.Tick();
            inbox.Poll();
        }
    }
}
