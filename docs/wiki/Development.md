# Sviluppo

Il progetto è una WinForms .NET con nullable reference types e warning trattati come errori nella CI.

```powershell
dotnet restore Project/WinHubX/WinHubX.csproj
dotnet build Project/WinHubX/WinHubX.csproj --configuration Release --warnaserror
git diff --check
```

Non committare output IDE, build, profiler o log: sono esclusi da `.gitignore`.
