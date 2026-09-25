FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Install dotnet tooling used by the project
RUN dotnet tool install --global dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

EXPOSE 8080
CMD ["dotnet", "watch", "run", "--project", "src/MunicipalElections", "--urls", "http://0.0.0.0:8080"]
