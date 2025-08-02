# デバッグ コンテナーをカスタマイズする方法と、Visual Studio がこの Dockerfile を使用してより高速なデバッグのためにイメージをビルドする方法については、https://aka.ms/customizecontainer をご覧ください。

# このステージは、VS から高速モードで実行するときに使用されます (デバッグ構成の既定値)
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS base
RUN apk add --no-cache tzdata
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine-aot AS with-libs
RUN apk update
RUN apk add --no-cache git
WORKDIR /libs
RUN git clone https://github.com/YuHima03/dotnet-traq-extensions.git
WORKDIR /libs/dotnet-traq-extensions
RUN git checkout feat/message-extensions
WORKDIR /libs
RUN git clone https://github.com/YuHima03/dotnet-traq.git
WORKDIR /libs/dotnet-traq
RUN git checkout aot

# Build the service project.
FROM with-libs AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BotTidus/BotTidus.csproj", "src/BotTidus/"]
RUN dotnet restore "./src/BotTidus/BotTidus.csproj"
COPY . .
WORKDIR "/src/src/BotTidus"
RUN dotnet build "./BotTidus.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the service project to copy to the final stage.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BotTidus.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
COPY ["appsettings.json", "/app/publish/"]
COPY ["appsettings.*.json", "/app/publish/"]

# Provide application binary.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./BotTidus"]
