# Prerequisites

* .NET SDK 8.0: [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download)
* PostgreSQL with database `apachi-log-db` created

# Setup

Clone this repo with submodules:

```
git clone --recurse-submodules https://github.com/mrphil2105/BachelorProject.git
cd BachelorProject
```

Install the Entity Framework Core tools:

```
dotnet tool install --global dotnet-ef
```

Apply the migrations for the PostgreSQL database:

```
cd Shared
APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" dotnet ef database update
```

# Execute

## ProgramCommitee

```
cd ProgramCommitee
APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" APACHI_APP_DATABASE_FILE="App.db" dotnet run
```

## UserApp

```
cd UserApp
APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" APACHI_APP_DATABASE_FILE="App.db" dotnet run
```
