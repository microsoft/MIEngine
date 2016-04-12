// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

[true, false].each { isPR ->
    ['Debug', 'Release'].each { configuration ->
        def newJob = job(Utilities.getFullJobName(project, configuration.toLowerCase(), isPR)) {
            steps {
                batchFile("\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:Configuration=${configuration}")
            }
        }
        
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestResults.xml')
        
        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "Windows ${configuration}")
        }
        else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}