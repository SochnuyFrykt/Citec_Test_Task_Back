FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Citec_Test_Task_Back/Citec_Test_Task_Back.csproj", "Citec_Test_Task_Back/"]
RUN dotnet restore "Citec_Test_Task_Back/Citec_Test_Task_Back.csproj"
COPY . .
WORKDIR "/src/Citec_Test_Task_Back"
RUN dotnet build "./Citec_Test_Task_Back.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Citec_Test_Task_Back.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Citec_Test_Task_Back.dll"]
