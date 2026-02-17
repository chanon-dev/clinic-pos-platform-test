FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClinicPOS.sln .
COPY ClinicPOS.Domain/*.csproj ClinicPOS.Domain/
COPY ClinicPOS.Application/*.csproj ClinicPOS.Application/
COPY ClinicPOS.Infrastructure/*.csproj ClinicPOS.Infrastructure/
COPY ClinicPOS.API/*.csproj ClinicPOS.API/
COPY ClinicPOS.UnitTests/*.csproj ClinicPOS.UnitTests/
RUN dotnet restore

COPY . .
RUN dotnet publish ClinicPOS.API -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "ClinicPOS.API.dll"]
