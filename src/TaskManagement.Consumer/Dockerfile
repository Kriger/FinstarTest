FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FinstarTest.sln", ""]
COPY ["src/TaskManagement.Consumer/TaskManagement.Consumer.csproj", "src/TaskManagement.Consumer/"]
COPY ["src/TaskManagement.Application/TaskManagement.Application.csproj", "src/TaskManagement.Application/"]
COPY ["src/TaskManagement.Domain/TaskManagement.Domain.csproj", "src/TaskManagement.Domain/"]
COPY ["src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj", "src/TaskManagement.Infrastructure/"]
RUN dotnet restore "./FinstarTest.sln"
COPY . .
WORKDIR "/src/src/TaskManagement.Consumer"
RUN dotnet build "TaskManagement.Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskManagement.Consumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskManagement.Consumer.dll"]
