FROM microsoft/dotnet:latest AS build
WORKDIR /app

RUN apt-get update 
RUN apt-get install zip net-tools -y

ARG AUTOMATICA_VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

COPY --from=node /src/dist /app/automatica/wwwroot

RUN automatica-cli setversion $AUTOMATICA_VERSION -W /src/src/automatica.core.plugin.standalone/
RUN dotnet publish -c Release -o /app/plugin /src/src/automatica.core.plugin.standalone/ -r linux-x64

RUN echo $AUTOMATICA_VERSION
RUN rm -rf /src

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app/

COPY --from=build /app/ ./