#step 1: build backend
FROM mcr.microsoft.com/dotnet/core/sdk AS build
RUN mkdir app
WORKDIR /app
COPY mycms .
RUN dotnet restore
RUN dotnet publish -c Release -o out

#step 2: run
FROM mcr.microsoft.com/dotnet/core/aspnet
ENV SQLSERVER_CONNECTIONSTRING='Server=localhost;Database=mycms;User=sa;Password={password};MultipleActiveResultSets=true;'
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT dotnet mycms.dll