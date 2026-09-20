services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: rcs-sqlserver
    environment:
      ACCEPT_EULA: "Y"
      # 使用 ${} 读取 .env 中的密码
      MSSQL_SA_PASSWORD: ${DB_PASSWORD}
    ports:
      - "1434:1433"
    # 健康检查：不断去 ping 数据库，直到数据库真正可以接受请求
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD}' -Q 'SELECT 1' -No || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:latest
    container_name: rcs-redis
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 3

  dbmigrator:
    # 不再复用 rcs-backend，而是让 Docker Compose 现场构建专属镜像
    build: 
      context: ./BackEnd   # 指向你后端解决方案的根目录 (根据实际情况修改)
      dockerfile: src/SIASUN.RCS.DbMigrator/Dockerfile # 专属的 Dockerfile 路径
    container_name: rcs-dbmigrator
    depends_on:
      sqlserver:
        condition: service_healthy # [关键] 必须等 sqlserver "健康" (完全启动) 才执行
    environment:
      # 连接字符串单行写死，防止折行符带来空格报错。同样读取环境变量
      - ConnectionStrings__Default=Server=sqlserver,1433;Database=SIASUN.RCS;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True

  api:
    image: rcs-backend
    container_name: rcs-api
    depends_on:
      dbmigrator:
        condition: service_completed_successfully
      redis:
        condition: service_healthy
    ports:
      - "${API_PORT}:9000"
    environment:
      - ASPNETCORE_URLS=http://+:9000
      - ConnectionStrings__Default=Server=sqlserver,1433;Database=SIASUN.RCS;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True
      # 【新增下面这行】：补上必填的 Name 属性
      - HealthChecksUI__HealthChecks__0__Name=RCS_API_Local
      - HealthChecksUI__HealthChecks__0__Uri=http://127.0.0.1:9000/health-status
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost:9000/health-status || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    image: rcs-frontend
    container_name: rcs-web
    depends_on:
      api:
        condition: service_healthy # [关键] 必须等 API "健康" (能够提供接口) 才启动前端
    ports:
      - "${WEB_PORT}:80"