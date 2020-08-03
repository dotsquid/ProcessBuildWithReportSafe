using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public abstract class BaseSafeProcessBuildWithReport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    [InitializeOnLoad]
    private static class Helper
    {
        private enum BuildResult
        {
            Failed,
            Cancelled,
            Other,
        }

        private delegate void ProcessBuild(BaseSafeProcessBuildWithReport processor, BuildReport report);

        private const string kErrorBuldingErrorMessage = "Error building";
        private const string kBuildCompletedFailedErrorMessage = "Build completed with a result of 'Failed'";
        private const string kBuildCancelledLogMessage = "Build completed with a result of 'Cancelled'";
        private const int kEstimateCount = 16;

        private static List<BaseSafeProcessBuildWithReport> _registeredProcessors = new List<BaseSafeProcessBuildWithReport>(kEstimateCount);
        private static BuildReport _report;
        private static bool _isProcessing;

        static Helper()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public static void Register(BaseSafeProcessBuildWithReport processor, BuildReport report)
        {
            _report = report;
            _registeredProcessors.Add(processor);
        }

        public static void Unregister(BaseSafeProcessBuildWithReport processor, BuildReport report)
        {
            _report = report;
            _registeredProcessors.Remove(processor);
        }

        private static BuildResult GetBuildResult(string condition, LogType type)
        {
            if ((type == LogType.Error && condition.Contains(kErrorBuldingErrorMessage)) ||
                (type == LogType.Error && condition.Contains(kBuildCompletedFailedErrorMessage)) ||
                (type == LogType.Exception && condition.Contains(nameof(BuildFailedException))))
                return BuildResult.Failed;

            if (type == LogType.Log && condition.Contains(kBuildCancelledLogMessage))
                return BuildResult.Cancelled;

            return BuildResult.Other;
        }

        private static void ProcessFailedBuild(BaseSafeProcessBuildWithReport processor, BuildReport report)
        {
            processor.OnBuildFailed(report);
        }

        private static void ProcessCancelledBuild(BaseSafeProcessBuildWithReport processor, BuildReport report)
        {
            processor.OnBuildCancelled(report);
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Prevent recursion if registered processor throws BuildFailedException
            if (_isProcessing)
                return;

            var buildProcessor = default(ProcessBuild);
            var buildResult = GetBuildResult(condition, type);
            switch (buildResult)
            {
                case BuildResult.Failed:
                    buildProcessor = ProcessFailedBuild;
                    break;

                case BuildResult.Cancelled:
                    buildProcessor = ProcessCancelledBuild;
                    break;
            }

            if (buildProcessor != null)
            {
                _isProcessing = true;
                foreach (var processor in _registeredProcessors)
                {
                    try
                    {
                        buildProcessor(processor, _report);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
                _registeredProcessors.Clear();
                _isProcessing = false;
            }
        }
    }

    public abstract int callbackOrder { get; }

    protected abstract void OnPostprocessBuild(BuildReport report);
    protected abstract void OnPreprocessBuild(BuildReport report);
    protected abstract void OnBuildFailed(BuildReport report);
    protected abstract void OnBuildCancelled(BuildReport report);

    void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
    {
        Helper.Register(this, report);
        OnPreprocessBuild(report);
    }

    void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
    {
        Helper.Unregister(this, report);
        OnPostprocessBuild(report);
    }
}
