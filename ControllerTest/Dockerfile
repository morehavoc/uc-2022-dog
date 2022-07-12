FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ControllerTest/ControllerTest.fsproj", "ControllerTest/"]
RUN dotnet restore "ControllerTest/ControllerTest.fsproj"
COPY . .
WORKDIR "/src/ControllerTest"
RUN dotnet build "ControllerTest.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ControllerTest.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ControllerTest.dll"]
