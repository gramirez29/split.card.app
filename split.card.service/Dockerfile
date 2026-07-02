FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Split.Card.Domain/Split.Card.Domain.csproj", "Split.Card.Domain/"]
COPY ["src/Split.Card.Application/Split.Card.Application.csproj", "Split.Card.Application/"]
COPY ["src/Split.Card.Infrastructure/Split.Card.Infrastructure.csproj", "Split.Card.Infrastructure/"]
COPY ["src/Split.Card.Api/Split.Card.Api.csproj", "Split.Card.Api/"]
RUN dotnet restore "Split.Card.Api/Split.Card.Api.csproj"

COPY src/Split.Card.Domain/ Split.Card.Domain/
COPY src/Split.Card.Application/ Split.Card.Application/
COPY src/Split.Card.Infrastructure/ Split.Card.Infrastructure/
COPY src/Split.Card.Api/ Split.Card.Api/

WORKDIR /src/Split.Card.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Split.Card.Api.dll"]
