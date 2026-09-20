FROM mcr.microsoft.com/dotnet/aspnet:10.0


WORKDIR /app


COPY publish/ .


EXPOSE 9000

ENTRYPOINT ["dotnet", "SIASUN.RCS.HttpApi.Host.dll"]