# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt install dnsutils -y && \
    apt install iputils-ping -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* 

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY .csharpierrc .csharpierrc
COPY .csharpierignore .csharpierignore

RUN dotnet tool restore

COPY BtmsGateway/BtmsGateway.csproj BtmsGateway/BtmsGateway.csproj
COPY tests/BtmsGateway.Test/BtmsGateway.Test.csproj tests/BtmsGateway.Test/BtmsGateway.Test.csproj
COPY tests/Testing/Testing.csproj tests/Testing/Testing.csproj
COPY tests/BtmsGateway.IntegrationTests/BtmsGateway.IntegrationTests.csproj tests/BtmsGateway.IntegrationTests/BtmsGateway.IntegrationTests.csproj
COPY BtmsGateway.sln BtmsGateway.sln

COPY NuGet.config NuGet.config
ARG DEFRA_NUGET_PAT

RUN dotnet restore

COPY BtmsGateway BtmsGateway
COPY tests/BtmsGateway.Test tests/BtmsGateway.Test
COPY tests/Testing tests/Testing
COPY tests/BtmsGateway.IntegrationTests tests/BtmsGateway.IntegrationTests

RUN dotnet csharpier check .

RUN dotnet build BtmsGateway/BtmsGateway.csproj --no-restore -c Release

RUN dotnet test --no-restore --filter "Category!=IntegrationTest"

FROM build AS publish
RUN dotnet publish BtmsGateway -c Release -o /app/publish /p:UseAppHost=false

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=mcr.microsoft.com/dotnet/aspnet:7.0 /etc/ssl/openssl.cnf /etc/ssl/openssl.cnf
EXPOSE 8085
ENTRYPOINT ["dotnet", "BtmsGateway.dll"]
