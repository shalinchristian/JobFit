FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ ./src/
RUN dotnet publish src/JobFit.Api/JobFit.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
# SQLite tracker database lives on a volume so it survives container restarts.
RUN mkdir -p /data
ENV ConnectionStrings__JobFit="Data Source=/data/jobfit.db"
EXPOSE 5000
ENTRYPOINT ["dotnet", "JobFit.Api.dll"]
