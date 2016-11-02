#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool "nuget:?package=xunit.runners&version=1.9.2"
#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var versionString = Argument("version", "0.0.0.localbuild");


// Define directories.
// var buildDir = Directory("./**/bin") + Directory(configuration);

// Task("Clean")
//     .Does(() =>
// {
//     CleanDirectory(buildDir);
// });

Task("Version")
    .Does(() => {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            OutputType = GitVersionOutput.BuildServer
        });
        var versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

        Information(versionInfo.NuGetVersion);
        // Update project.json

     foreach( var jsonProject in GetFiles("./**/project.json") ){
        var updatedProjectJson = System.IO.File.ReadAllText(jsonProject.FullPath)
            .Replace("1.0.0-*", versionInfo.NuGetVersion);

        System.IO.File.WriteAllText(jsonProject.FullPath, updatedProjectJson);
     }
    
    });

Task("Pack").Does( () => {

    var settings = new DotNetCorePackSettings
        {
            OutputDirectory = ".",
            NoBuild = true
        };

     foreach( var jsonProject in GetFiles("./**/project.json") ){
        DotNetCorePack(jsonProject.FullPath);
     }
    
} );

Task("Restore").Does(() => {

    var netCoreProjects = GetFiles("./**/project.json");

    if( netCoreProjects.Any() ){
        DotNetCoreRestore();
    }
});
// Build all projects
Task("Build")
    .IsDependentOn("Version")
    .Does(() =>
{
    var netCoreProjects = GetFiles("./**/project.json");

    if( netCoreProjects.Any() ){
        DotNetCoreRestore();

     foreach( var jsonProject in netCoreProjects ){
         DotNetCoreBuild(jsonProject.FullPath); 
     }
    }
    
     foreach( var sln in GetFiles("./**/*.sln") ){
        NuGetRestore(sln.FullPath);
        DotNetBuild(sln.FullPath); 
     }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit("**/" + configuration + "/*.Tests.dll", new XUnitSettings {
        HtmlReport = true,
        OutputDirectory = "./test-results"
    });
});

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

RunTarget(target);
