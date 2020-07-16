# ProcessBuildWithReportSafe
Extend IPreprocessBuildWithReport, IPostprocessBuildWithReport to handle a failed build case

## Problem
Quite often Unity developers have to cut into Unity build pipeline by implementing both `IPreprocessBuildWithReport` and `IPostprocessBuildWithReport`. For instance, for some given platforms it may be necessary to copy some assets into 'StreamingAssets' directory before the build and clean-up those files after the build. Or apply some settings in 'PlayerSettings' prior to the build and restore original ones after that.  
Unfortunately, `IPreprocessBuildWithReport` and `IPostprocessBuildWithReport` do not provide any callback to handle the situation when the build failed. This results in uncleaned / unrestored state of the project when it's incorrectly filled with temporary setting and unnecessary / undesired files which in turn litters the version control system. Meh.

## Solution
I tried to fix this situation by implementing an abstract class `BaseProcessBuildWithReportSafe` which implements both `IPreprocessBuildWithReport` and `IPostprocessBuildWithReport` and provides an extra abstract method `OnBuildFailed` which is called (as it's seen from its name) in case the build failed.

### Caveat
Despite I tested this solution in a real project, I'm still not sure that it can handle every case of the failed build. The fact is that Unity does not provide any API to catch exceptions globaly. That's why I had to use `Application.logMessageReceived` to intercept the logs and by searching for some footprints determine that the build actually failed.  
It means that this routine may be extended or changed in future to detect build fails more reliably.

## Usage
Just inherit `BaseProcessBuildWithReportSafe` class and override `OnPostprocessBuild`, `OnPreprocessBuild`, `OnBuildFailed` methods.  
`OnBuildFailed` takes a `BuildReport` as a parameter. Depending on when the build actually failed (during `OnPostprocessBuild` stage or after that) it may receive a report passed either to `OnPostprocessBuild` or to `OnPreprocessBuild` (check source code for details).
