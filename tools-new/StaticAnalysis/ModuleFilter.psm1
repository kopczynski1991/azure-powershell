function Get-ModulesInV3 {
    [OutputType([string[]])]
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $RootDirectory,
        [Parameter()]
        [string[]]
        $Modules
    )
    $Result = @()
    foreach ($Module in $Modules)
    {
        $ReadmePath = Join-Path -Path "$RootDirectory\$($Module -replace 'Az.', '')" -ChildPath "readme.md"
        if (-not (Test-Path -Path $ReadmePath))
        {
            $Result += $Module
        }
    }

    return $Result
}

function Get-ModulesInV4 {

    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $RootDirectory,
        [Parameter()]
        [string[]]
        $Modules
    )
    
    $Result = @()
    foreach ($Module in $Modules)
    {
        $ReadmePath = Join-Path -Path "$RootDirectory\$($Module -replace 'Az.', '')" -ChildPath "readme.md"
        if (Test-Path -Path $ReadmePath)
        {
            $Result += $Module
        }
    }

    return $Result
}

Export-ModuleMember -Function *
