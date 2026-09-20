FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. 拷贝所有相关的项目配置文件 (.csproj)，维持原本的目录结构
COPY ["src/SIASUN.RCS.DbMigrator/SIASUN.RCS.DbMigrator.csproj", "src/SIASUN.RCS.DbMigrator/"]
COPY ["src/SIASUN.RCS.EntityFrameworkCore/SIASUN.RCS.EntityFrameworkCore.csproj", "src/SIASUN.RCS.EntityFrameworkCore/"]
COPY ["src/SIASUN.RCS.Domain/SIASUN.RCS.Domain.csproj", "src/SIASUN.RCS.Domain/"]
COPY ["src/SIASUN.RCS.Domain.Shared/SIASUN.RCS.Domain.Shared.csproj", "src/SIASUN.RCS.Domain.Shared/"]
COPY ["src/SIASUN.RCS.Application.Contracts/SIASUN.RCS.Application.Contracts.csproj", "src/SIASUN.RCS.Application.Contracts/"]

# 2. 执行还原。只要上述 .csproj 文件不被修改，这一步的耗时在以后就会变成 0 秒 (CACHED)
RUN dotnet restore "src/SIASUN.RCS.DbMigrator/SIASUN.RCS.DbMigrator.csproj"

# 3. 拷贝全部源码并编译发布
COPY . .
WORKDIR "/src/src/SIASUN.RCS.DbMigrator"
RUN dotnet publish "SIASUN.RCS.DbMigrator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SIASUN.RCS.DbMigrator.dll"]