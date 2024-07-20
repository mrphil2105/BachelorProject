#!/bin/sh
APACHI_APP_DATABASE_FILE="App.db" dotnet ef migrations add $1
