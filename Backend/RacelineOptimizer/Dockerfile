FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore
COPY . ./

RUN mkdir -p Input Output

RUN dotnet publish -c Release -o out
FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app
COPY --from=build /app/out ./

RUN mkdir -p Input Output

CMD ["dotnet", "RacelineOptimizer.dll"]
