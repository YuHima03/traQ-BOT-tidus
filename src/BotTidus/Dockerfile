# デバッグ コンテナーをカスタマイズする方法と、Visual Studio がこの Dockerfile を使用してより高速なデバッグのためにイメージをビルドする方法については、https://aka.ms/customizecontainer をご覧ください。

# このステージは、VS から高速モードで実行するときに使用されます (デバッグ構成の既定値)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM debian:12-slim AS libs
RUN apt -y update && apt -y upgrade && apt -y install git-all
WORKDIR /libs
RUN git clone https://github.com/YuHima03/dotnet-traq-extensions.git
WORKDIR /libs/dotnet-traq-extensions
RUN git checkout feat/message-extensions

# Build the service project.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY --from=libs /libs libs/
COPY ["src/BotTidus/BotTidus.csproj", "src/BotTidus/"]
RUN dotnet restore "./src/BotTidus/BotTidus.csproj"
COPY . .
WORKDIR "/src/src/BotTidus"
RUN dotnet build "./BotTidus.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the service project to copy to the final stage.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BotTidus.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Provide application binary.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotTidus.dll"]
