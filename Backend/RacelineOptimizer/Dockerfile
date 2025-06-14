# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Create Input and Output folders (in the build stage for completeness)
RUN mkdir -p Input Output

# Publish app
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app

# Copy built app from build stage
COPY --from=build /app/out ./

# Create Input and Output in runtime container (redundant with bind mount, but ensures it exists)
RUN mkdir -p Input Output

# Default command to run app
CMD ["dotnet", "RacelineOptimizer.dll"]
