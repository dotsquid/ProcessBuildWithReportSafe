using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public abstract class BaseProcessBuildWithReportSafe : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    [InitializeOnLoad]
    private static class Helper
    {
        private const string kBuildFailedErrorMessage = "Error building";
        private const int kEstimateCount = 16;

        private static List<BaseSafeProcessBuildWithReport> _registeredProcessors = new List<BaseSafeProcessBuildWithReport>(kEstimateCount);
        private static BuildReport _report;
        private static bool _isProcessingFailedBuild;

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

        private static bool IsBuildFailed(string condition, LogType type)
        {

            return (type == LogType.Error && condition.Contains(kBuildFailedErrorMessage))
                || (type == LogType.Exception && condition.Contains(nameof(BuildFailedException)));
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Prevent recursion if registered processor throws BuildFailedException
            if (_isProcessingFailedBuild)
                return;

            if (IsBuildFailed(condition, type))
            {
                _isProcessingFailedBuild = true;
                foreach (var processor in _registeredProcessors)
                {
                    try
                    {
                        processor.OnBuildFailed(_report);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
                _registeredProcessors.Clear();
                _isProcessingFailedBuild = false;
            }
        }
    }

    public abstract int callbackOrder { get; }

    protected abstract void OnPostprocessBuild(BuildReport report);
    protected abstract void OnPreprocessBuild(BuildReport report);
    protected abstract void OnBuildFailed(BuildReport report);

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
