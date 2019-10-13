#load nuget:?package=Cake.Issues.Recipe&prerelease
#load buildData.cake

#tool "nuget:?package=JetBrains.ReSharper.CommandLineTools"
#tool "nuget:?package=MSBuild.Extension.Pack"

var target = Argument("target", "Default");

Setup<BuildData>(setupContext =>
{
	return new BuildData(setupContext);
});

Task("Build")
    .Does<BuildData>((data) =>
{
    var solutionFile =
        data.IssuesData.RepositoryRootDirectory.Combine("src").CombineWithFilePath("ClassLibrary1.sln");

    NuGetRestore(solutionFile);

    var settings =
        new MSBuildSettings()
            .WithTarget("Rebuild");

    // XML File Logger
    settings =
        settings.WithLogger(
            Context.Tools.Resolve("MSBuild.ExtensionPack.Loggers.dll").FullPath,
            "XmlFileLogger",
            string.Format(
                "logfile=\"{0}\";verbosity=Detailed;encoding=UTF-8",
                data.MsBuildLogFilePath));

    EnsureDirectoryExists(IssuesParameters.OutputDirectory);
    MSBuild(solutionFile, settings);

    // Pass path to MsBuild log file to Cake.Issues.Recipe
    IssuesParameters.InputFiles.MsBuildXmlFileLoggerLogFilePath = data.MsBuildLogFilePath;
});

Task("Run-InspectCode")
    .WithCriteria((context) => context.IsRunningOnWindows(), "InspectCode is only supported on Windows.")
    .Does<BuildData>((data) =>
{
    var settings = new InspectCodeSettings() {
        OutputFile = data.InspectCodeLogFilePath
    };

    InspectCode(
        data.IssuesData.RepositoryRootDirectory.Combine("src").CombineWithFilePath("ClassLibrary1.sln"),
        settings);

    // Pass path to InspectCode log file to Cake.Issues.Recipe
    IssuesParameters.InputFiles.InspectCodeLogFilePath = data.InspectCodeLogFilePath;
});

Task("Lint")
    .IsDependentOn("Run-InspectCode");

// Make sure build and linters run before issues task.
IssuesBuildTasks.ReadIssuesTask
    .IsDependentOn("Build")
    .IsDependentOn("Lint");

// Run issues task by default.
Task("Default")
    .IsDependentOn("Issues");
 
RunTarget(target);