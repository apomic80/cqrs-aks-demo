#step 1: build backend
FROM mcr.microsoft.com/dotnet/core/sdk AS build
RUN mkdir app
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

#step 2: run
FROM mcr.microsoft.com/dotnet/core/aspnet
ENV REDIS_CONNECTIONSTRING='{address}:6380,password={password},ssl=True,abortConnect=False'
ENV REDIS_PASSWORD='password'
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT dotnet mycms.dll