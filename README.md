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

Environment variables can either be set system-wide (e.g. in `.bashrc`) or by prefixing them to commands as we do below.

First, apply the migrations for the PostgreSQL database:

```
cd Shared
APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" dotnet ef database update
```

Then, generate a key pair for the Programme Committee:

```
cd ProgramCommitee
dotnet run --generate-keypair
```

Save the base64 for the private and public key. The private key is required in the `ProgramCommitee` application and the
public key is required in the `UserApp` application.

# Execute

## ProgramCommitee

```
cd ProgramCommitee
APACHI_PC_PRIVATE_KEY=<replace-with-generated-private-key-base64> APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" APACHI_APP_DATABASE_FILE="App.db" dotnet run
```

## UserApp

```
cd UserApp
APACHI_PC_PUBLIC_KEY=<replace-with-generated-public-key-base64> APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" APACHI_APP_DATABASE_FILE="App.db" dotnet run
```
