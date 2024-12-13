# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt install dnsutils -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* 

RUN sudo touch etc/hosts && \
    sudo sed -i '$ a 10.62.146.246 t2.secure.services.defra.gsi.gov.uk' etc/hosts

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
WORKDIR "/src"

# unit test and code coverage
RUN dotnet test BtmsGateway.Test

FROM build AS publish
RUN dotnet publish BtmsGateway -c Release -o /app/publish /p:UseAppHost=false


ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "BtmsGateway.dll"]
