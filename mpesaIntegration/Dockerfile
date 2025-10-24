# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0.303 AS build
WORKDIR /src

# Copy the csproj and restore as distinct layers
COPY ["cmag-azcontreg-worker.csproj", "./"]
RUN dotnet restore "cmag-azcontreg-worker.csproj"

# Copy the remaining files and build the project
COPY . .
RUN dotnet build "cmag-azcontreg-worker.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "cmag-azcontreg-worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Copy the published files from the publish stage
COPY --from=publish /app/publish .

# Set the entrypoint to the app
ENTRYPOINT ["dotnet", "cmag-azcontreg-worker.dll"]# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0.303 AS build
WORKDIR /src

# Copy the csproj and restore as distinct layers
COPY ["cmag-azcontreg-worker.csproj", "./"]
RUN dotnet restore "cmag-azcontreg-worker.csproj"

# Copy the remaining files and build the project
COPY . .
RUN dotnet build "cmag-azcontreg-worker.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "cmag-azcontreg-worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Copy the published files from the publish stage
COPY --from=publish /app/publish .

# Set the entrypoint to the app
ENTRYPOINT ["dotnet", "cmag-azcontreg-worker.dll"]