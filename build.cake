#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool "nuget:?package=xunit.runners&version=1.9.2"
#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Default");
string configuration = Argument("configuration", "Release");
string versionString = Argument("version", "0.0.0.localbuild");
string artifacts = Argument("artifacts", "./artifacts");

public IEnumerable<FilePath> JsonProjects() {
    return GetFiles("./**/project.json");
}

Task("Version")
    .Does(() => {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            NoFetch = true,
            OutputType = GitVersionOutput.BuildServer
        });
        var versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

        Information( "GitVersion determined : " + versionInfo.NuGetVersion );
        // Update project.json
    
     foreach( var jsonProject in JsonProjects() ){
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

     foreach( var jsonProject in JsonProjects() ){
        DotNetCorePack(jsonProject.FullPath);
     }
    
} );

Task("Restore").Does(() => {
    var netCoreProjects = JsonProjects();

    if( netCoreProjects.Any() ){
        DotNetCoreRestore();
    }

     foreach( var sln in GetFiles("./**/*.sln") ){
        NuGetRestore(sln.FullPath);
     }
});

// Build all projects
Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Version")
    .Does(() =>
{
    var netCoreProjects = JsonProjects();

    if( netCoreProjects.Any() ){
        DotNetCoreRestore();

     foreach( var jsonProject in netCoreProjects ){
         DotNetCoreBuild(jsonProject.FullPath); 
     }
    }
    
     foreach( var sln in GetFiles("./**/*.sln") ){
        DotNetBuild(sln.FullPath); 
     }
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("**/" + configuration + "/*.Tests.dll", new NUnit3Settings {
    });
});

Task("Package")
    .IsDependentOn("Build").Does(()=> {
        foreach(var jsonProject in JsonProjects()){
            var settings = new DotNetCorePackSettings
            {
                OutputDirectory = artifacts,
                NoBuild = true
            };

            DotNetCorePack(jsonProject.FullPath, settings);
        }       
    });

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);
