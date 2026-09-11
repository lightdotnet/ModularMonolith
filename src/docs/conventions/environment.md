# dotnet environment

###
```
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<SECRET_VALUE>" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```
```
dotnet user-secrets list --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```
```
dotnet user-secrets remove "Authentication:Microsoft:ClientSecret" --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```

