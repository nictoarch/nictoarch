FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish Nictoarch.Modelling.App/Nictoarch.Modelling.App.csproj -c Release -o out
RUN dotnet test

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Nictoarch.Modelling.App.dll"]
