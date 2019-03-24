param (
    [Parameter(mandatory=$true)][string]$githubaccesstoken,
    [Parameter(mandatory=$true)][string]$artifactDir
)

$ErrorActionPreference = "Stop"
function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

$githubusername = "CoderNate"
cd $artifactDir
if (Test-Path tempDir) {
	throw "TempDIr already exists"
}
Write-Host "About to clone into $(Join-Path (Resolve-Path .).Path -ChildPath tempDir)"
# Have to clone into a new directory or git will error saying the directory already exists and is not empty
ExecSafe {git clone --no-checkout --branch=gh-pages "https://$($githubusername):$githubaccesstoken@github.com/$githubusername/CSharpToPython.git" tempDir --quiet}
Move-Item tempDir\.git .git -force
git config core.autocrlf false
git config user.email ncdahlquist@yahoo.com
git config user.name $githubusername
git add -A
git commit --amend --no-edit
ExecSafe {git push --force-with-lease --quiet}
