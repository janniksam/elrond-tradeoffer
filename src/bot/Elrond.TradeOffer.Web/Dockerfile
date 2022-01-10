#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Elrond.TradeOffer.Web/Elrond.TradeOffer.Web.csproj", "Elrond.TradeOffer.Web/"]
RUN dotnet restore "Elrond.TradeOffer.Web/Elrond.TradeOffer.Web.csproj"
COPY . .
WORKDIR "/src/Elrond.TradeOffer.Web"
RUN dotnet build "Elrond.TradeOffer.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elrond.TradeOffer.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elrond.TradeOffer.Web.dll"]