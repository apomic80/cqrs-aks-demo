#step 2: build backend
FROM mcr.microsoft.com/dotnet/core/sdk AS build
RUN mkdir app
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

#step 3: run
FROM mcr.microsoft.com/dotnet/core/aspnet
ENV SQLSERVER_CONNECTIONSTRING='Server=localhost;Database=mycms;User=sa;Password={password};MultipleActiveResultSets=true;'
ENV SA_PASSWORD='password'
ENV REDIS_CONNECTIONSTRING='{address}:6380,password={password},ssl=True,abortConnect=False'
ENV REDIS_PASSWORD='password'
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT dotnet mycms.dll