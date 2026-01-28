# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

RUN apt update && apt install -y unixodbc-dev libgssapi-krb5-2 # curl apt-transport-https gnupg

COPY simbaspark_2.9.4.1013-2_amd64.deb /simbaspark.deb
RUN apt install -y /simbaspark.deb

#RUN apt update && apt install -y 

USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TestDatabricksODBC/TestDatabricksODBC.csproj", "TestDatabricksODBC/"]
RUN dotnet restore "./TestDatabricksODBC/TestDatabricksODBC.csproj"
COPY . .
WORKDIR "/src/TestDatabricksODBC"
RUN dotnet build "./TestDatabricksODBC.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TestDatabricksODBC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TestDatabricksODBC.dll"]