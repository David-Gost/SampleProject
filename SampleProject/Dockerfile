# 構建階段
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 複製專案文件
COPY ["SampleProject.csproj", "SampleProject/"]
RUN dotnet restore "SampleProject/SampleProject.csproj"

WORKDIR "/src/SampleProject"
COPY . .
# 安裝 Entity Framework Core 工具
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# 構建和發布
RUN dotnet build "SampleProject.csproj" -c $BUILD_CONFIGURATION -o /app/build 
RUN dotnet publish "SampleProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最終階段
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 複製 EF Core 工具到最終鏡像
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# 移除暫存
RUN rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "SampleProject.dll"]