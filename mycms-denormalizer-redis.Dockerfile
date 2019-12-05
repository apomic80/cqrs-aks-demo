#step 1: build backend
FROM mcr.microsoft.com/dotnet/core/sdk AS build
RUN mkdir app
WORKDIR /app
COPY ./mycms-denormalizer-redis ./mycms-denormalizer-redis
COPY ./mycms-shared ./mycms-shared
WORKDIR /app/mycms-denormalizer-redis
RUN dotnet restore
RUN dotnet publish -c Release -o out

#step 2: run
FROM mcr.microsoft.com/dotnet/core/aspnet
ENV RABBIT_HOSTNAME='localhost'
ENV RABBIT_USER='user'
ENV RABBIT_PASSWORD='password'
ENV RABBIT_PORT='5672'
ENV FRONTEND_QUEUE_NAME="frontend"
WORKDIR /app
COPY --from=build /app/mycms-denormalizer-redis/out .
ENTRYPOINT dotnet mycms-denormalizer-redis.dll