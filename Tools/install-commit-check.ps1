$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $hookPath = git rev-parse --git-path hooks/pre-commit
    if ($LASTEXITCODE -ne 0) { throw 'Git repository not found.' }
    $hook = "#!/bin/sh`n# RedCliffsDeduction staged-file guard`nexec bash Tools/check-staged-files.sh`n"
    if ((Test-Path -LiteralPath $hookPath) -and ((Get-Content -LiteralPath $hookPath -Raw) -ne $hook)) {
        throw 'An existing pre-commit hook is present. Merge it manually; it will not be overwritten.'
    }
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($hookPath), $hook, (New-Object Text.UTF8Encoding($false)))
    Write-Output 'Staged-file guard installed. Existing Git LFS hooks are unchanged.'
} finally { Pop-Location }
