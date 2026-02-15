# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# These ARGs allow for swapping out the base used to make the final image when debugging from VS
ARG LAUNCHING_FROM_VS
# This sets the base image for final, but only if LAUNCHING_FROM_VS has been defined
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot AS build
# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev curl binutils-aarch64-linux-gnu
RUN curl -sL https://deb.nodesource.com/setup_23.x | bash
RUN apt-get -y install nodejs
RUN corepack enable
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
ARG REVISION=latest
COPY Directory.Packages.props ./
WORKDIR /samples
COPY ["samples/MinimalHtml.Sample/MinimalHtml.Sample.csproj", "MinimalHtml.Sample/"]
COPY ["samples/MinimalHtml.Sample/package.json", "MinimalHtml.Sample/"]
COPY ["samples/MinimalHtml.Sample/pnpm-lock.yaml", "MinimalHtml.Sample/"]
WORKDIR /src
COPY ["src/MinimalHtml.AspNetCore/MinimalHtml.AspNetCore.csproj", "MinimalHtml.AspNetCore/"]
COPY ["src/MinimalHtml/MinimalHtml.csproj", "MinimalHtml/"]
COPY ["src/MinimalHtml.SourceGenerator/MinimalHtml.SourceGenerator.csproj", "MinimalHtml.SourceGenerator/"]
WORKDIR /experimental
COPY ["experimental/MinimalForms/MinimalForms.csproj", "MinimalForms/"]
COPY ["experimental/MinimalHtml.Vite/MinimalHtml.Vite.csproj", "MinimalHtml.Vite/"]
COPY ["experimental/MinimalForms.ModelGenerator/MinimalForms.ModelGenerator.csproj", "MinimalForms.ModelGenerator/"]
COPY ["experimental/MinimalHtml.CssModules/MinimalHtml.CssModules.csproj", "MinimalHtml.CssModules/"]
WORKDIR "/samples/MinimalHtml.Sample"
RUN dotnet restore "./MinimalHtml.Sample.csproj" -r linux-$TARGETARCH
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
WORKDIR "/"
COPY . .
WORKDIR "/samples/MinimalHtml.Sample"

# This stage is used to publish the service project to be copied to the final stage
RUN pnpm run build
RUN dotnet publish "./MinimalHtml.Sample.csproj" -o /app -r linux-$TARGETARCH /p:SourceRevisionId=$REVISION
RUN rm -f /app/*.dbg /app/*.Development.json /app/package.json /app/tsconfig.json

# This stage is used as the base for the final stage when launching from VS to support debugging in regular mode (Default when not using the Debug configuration)
FROM base AS aotdebug
USER root
# Install GDB to support native debugging
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["./MinimalHtml.Sample"]
