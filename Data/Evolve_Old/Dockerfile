FROM mcr.microsoft.com/dotnet/sdk:7.0

# Install required packages
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Install .NET tools
RUN dotnet tool install --global Evolve.Tool

# Add .NET tools to PATH
ENV PATH="/root/.dotnet/tools:${PATH}"

WORKDIR /app 