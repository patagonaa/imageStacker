FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app/src
COPY ./ .
RUN dotnet restore imageStacker.Cli/imageStacker.Cli.csproj
RUN dotnet publish -c Release imageStacker.Cli/imageStacker.Cli.csproj -o /app/build

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
RUN apt-get update
RUN apt-get install -y libc6-dev libgdiplus libx11-dev
WORKDIR /app
COPY --from=build /app/build/ ./
ENTRYPOINT ["dotnet", "imageStacker.Cli.dll"]