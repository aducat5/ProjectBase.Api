## This is a base API project that can be used as a starter for mobile apps, with basic api needs.

### The project is based on dotnet 6 and identity framework with ef. 

### Install Steps
- Restore dependencies 
- Install dotnet ef tools
```
dotnet tool install --global dotnet-ef
```
- Open a terminal and go to the of Data Project (where identity context exists)
- Add migrations
```
 dotnet ef migrations add InitialMigrations -s ..\ProjectBase.Api\ProjectBase.Api.csproj
```
- Apply migrations
```
dotnet ef database update -s ..\ProjectBase.Api\ProjectBase.Api.çsproj
```
- Run the app

