using R4nd0mApps.TddStud10.Common.Domain;
using R4nd0mApps.TddStud10.Engine.Core;
using R4nd0mApps.TddStud10.Logger;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;

namespace R4nd0mApps.TddStud10.Engine
{
    public interface IEngineHost : IRunExecutorHost
    {
        void RunStarting(RunStartParams rd);

        void RunStepStarting(RunStepStartingEventArg rsea);

        void OnRunStepError(RunStepErrorEventArg rss);

        void RunStepEnded(RunStepEndedEventArg rss);

        void OnRunError(RunFailureInfo rfi);

        void RunEnded(RunStartParams rsp);
    }

    public class EngineLoaderParams
    {
        public EngineConfig EngineConfig { get; set; }
        public FilePath SolutionPath { get; set; }
        public DateTime SessionStartTime { get; set; }
    }

    // NOTE: This entity will continue to be alive till we figure out the final trigger mechanism(s)
    // Till then we will just have to carefully do/undo the pairs of functionality at appropriate places
    public static class EngineLoader
    {
        private static ILogger Logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger;
        private static ITelemetryClient TelemetryClient = R4nd0mApps.TddStud10.Logger.TelemetryClientFactory.telemetryClient;

        private static EngineFileSystemWatcher _efsWatcher;
        private static IEngineHost _host;
        private static IDataStore _dataStore;
        private static TddStud10Runner _runner;
        private static Task _currentRun;
        private static CancellationTokenSource _currentRunCts;

        private static Action<RunState> _runStateChangedHandler;
        private static Action<RunStartParams> _runStartingHandler;
        private static Action<RunStepStartingEventArg> _runStepStartingHandler;
        private static Action<RunStepErrorEventArg> _onRunStepErrorHandler;
        private static Action<RunStepEndedEventArg> _runStepEndedHandler;
        private static Action<RunFailureInfo> _onRunErrorHandler;
        private static Action<RunStartParams> _runEndedHandler;

        public static void Load(IEngineHost host, IDataStore dataStore, EngineLoaderParams loaderParams)
        {
            Logger.LogInfo("Loading Engine with solution {0}", loaderParams.SolutionPath);

            _host = host;
            _dataStore = dataStore;

            _runStateChangedHandler = _host.RunStateChanged;
            _runStartingHandler =
                ea =>
                {
                    _host.RunStarting(ea);
                    _dataStore.UpdateRunStartParams(ea);
                };
            _runStepStartingHandler = _host.RunStepStarting;
            _onRunStepErrorHandler = _host.OnRunStepError;
            _runStepEndedHandler =
                ea =>
                {
                    _host.RunStepEnded(ea);
                    _dataStore.UpdateData(ea.rsr.runData);
                };
            _onRunErrorHandler = _host.OnRunError;
            _runEndedHandler = _host.RunEnded;

            _runner = _runner ?? TddStud10Runner.Create(host, Engine.CreateRunSteps(_dataStore.FindTest));
            _runner.AttachHandlers(
                FuncConvert.ToFSharpFunc(_runStateChangedHandler),
                FuncConvert.ToFSharpFunc(_runStartingHandler),
                FuncConvert.ToFSharpFunc(_runStepStartingHandler),
                FuncConvert.ToFSharpFunc(_onRunStepErrorHandler),
                FuncConvert.ToFSharpFunc(_runStepEndedHandler),
                FuncConvert.ToFSharpFunc(_onRunErrorHandler),
                FuncConvert.ToFSharpFunc(_runEndedHandler));

            _efsWatcher = EngineFileSystemWatcher.Create(loaderParams, RunEngine);
        }

        public static bool IsEngineLoaded()
        {
            return _efsWatcher != null;
        }

        public static bool IsEngineEnabled()
        {
            var enabled = IsEngineLoaded() && _efsWatcher.IsEnabled();
            Logger.LogInfo("Engine is enabled:{0}", enabled);

            return enabled;
        }

        public static void EnableEngine()
        {
            Logger.LogInfo("Enabling Engine...");
            TelemetryClient.TrackEvent("EnableEngine", new Dictionary<string, string>(), new Dictionary<string, double>());
            _efsWatcher.Enable();
        }

        public static void DisableEngine()
        {
            Logger.LogInfo("Disabling Engine...");
            TelemetryClient.TrackEvent("DisableEngine", new Dictionary<string, string>(), new Dictionary<string, double>());
            DataStore.Instance.ResetData();
            _efsWatcher.Disable();
        }


        public static void Unload()
        {
            Logger.LogInfo("Unloading Engine...");

            _efsWatcher.Dispose();
            _efsWatcher = null;

            _runner.DetachHandlers();

            _runEndedHandler = null;
            _onRunErrorHandler = null;
            _runStepEndedHandler = null;
            _onRunStepErrorHandler = null;
            _runStepStartingHandler = null;
            _runStartingHandler = null;
            _runStateChangedHandler = null;
        }

        public static bool IsRunInProgress()
        {
            if (_currentRun == null
                || (_currentRun.Status == TaskStatus.Canceled
                    || _currentRun.Status == TaskStatus.Faulted
                    || _currentRun.Status == TaskStatus.RanToCompletion))
            {
                return false;
            }

            return true;
        }

        private static void RunEngine(EngineLoaderParams loaderParams)
        {
            if (_runner != null)
            {
                InvokeEngine(loaderParams);
            }
            else
            {
                Logger.LogInfo("Engine is not loaded. Ignoring command.");
            }
        }

        private static void InvokeEngine(EngineLoaderParams loaderParams)
        {
            try
            {
                if (!_host.CanContinue())
                {
                    Logger.LogInfo("Cannot start engine. Host has denied request.");
                    return;
                }

                if (IsRunInProgress())
                {
                    Logger.LogInfo("Cannot start engine. A run is already in progress.");
                    return;
                }

                Logger.LogInfo("--------------------------------------------------------------------------------");
                Logger.LogInfo("EngineLoader: Going to trigger a run.");
                // NOTE: Note fix the CT design once we wire up.
                if (_currentRunCts != null)
                {
                    _currentRunCts.Dispose();
                }
                _currentRunCts = new CancellationTokenSource();
                _currentRun = _runner.StartAsync(loaderParams.EngineConfig, loaderParams.SessionStartTime, loaderParams.SolutionPath, _currentRunCts.Token);
            }
            catch (Exception e)
            {
                Logger.LogError("Exception thrown in InvokeEngine: {0}.", e);
            }
        }
    }
}
