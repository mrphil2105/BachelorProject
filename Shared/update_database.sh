#!/bin/sh
APACHI_LOG_DATABASE_CONNECTION="Host=localhost;Username=postgres;Database=apachi-log-db" dotnet ef database update
