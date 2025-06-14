# Use official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /app

# Copy project files
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Create input/output directories
RUN mkdir -p /app/Input && mkdir -p /app/Output

# Set entrypoint
ENTRYPOINT ["dotnet", "RacelineOptimizer.dll"]