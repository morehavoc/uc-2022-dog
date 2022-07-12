FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LIdarTest/LIdarTest.fsproj", "LIdarTest/"]
RUN dotnet restore "LIdarTest/LIdarTest.fsproj"
COPY . .
WORKDIR "/src/LIdarTest"
RUN dotnet build "LIdarTest.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LIdarTest.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LIdarTest.dll"]
