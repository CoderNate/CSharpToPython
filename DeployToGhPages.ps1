param (
    [Parameter(mandatory=$true)][string]$githubaccesstoken,
    [Parameter(mandatory=$true)][string]$artifactDir
)

$ErrorActionPreference = "Stop"

$githubusername = "CoderNate"
cd $artifactDir
# Have to clone into a new directory or git will error saying the directory already exists and is not empty
git clone --no-checkout --branch=gh-pages "https://$($githubusername):$githubaccesstoken@github.com/$githubusername/CSharpToPython.git" tempDir
Move-Item tempDir\.git .\.git -force
git config core.autocrlf false
git config user.email ncdahlquist@yahoo.com
git config user.name $githubusername
git add -A
git commit --amend --no-edit
git push --force-with-lease
