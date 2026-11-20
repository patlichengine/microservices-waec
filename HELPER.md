## Install command line tools for Microservices app development
## winget install Microsoft.DotNet.SDK.10


# 1. Remove all bin and obj folders across every project
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force

# 2. Restore NuGet packages and build every solution/project cleanly
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    dotnet restore $_.FullName
    dotnet build $_.FullName --no-restore
}


# Reinstall the packages. You can have more packages listed in the array
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    $proj = $_.FullName
    $content = Get-Content $proj -Raw
    foreach ($pkg in @("Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.AspNetCore.Identity.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.SqlServer")) {
        if ($content -match $pkg) {
            dotnet add $proj package $pkg --version 10.0.10
        }
    }
}
