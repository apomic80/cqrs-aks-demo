#step 1: build backend
FROM mcr.microsoft.com/dotnet/core/sdk AS build
RUN mkdir app
WORKDIR /app
COPY ./mycms-be ./mycms-be
COPY ./mycms-shared ./mycms-shared
WORKDIR /app/mycms-be
RUN dotnet restore
RUN dotnet publish -c Release -o out

#step 2: run
FROM mcr.microsoft.com/dotnet/core/aspnet
ENV SQLSERVER_CONNECTIONSTRING='Server=localhost;Database=mycms;User=sa;Password={password};MultipleActiveResultSets=true;'
ENV SA_PASSWORD='password'
ENV RABBIT_HOSTNAME='localhost'
ENV RABBIT_USER='user'
ENV RABBIT_PASSWORD='password'
ENV RABBIT_PORT='5672'
WORKDIR /app
COPY --from=build /app/mycms-be/out .
ENTRYPOINT dotnet mycms.dll