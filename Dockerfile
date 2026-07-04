# syntax=docker/dockerfile:1.7
FROM ubuntu:24.04

SHELL ["/bin/bash", "-o", "pipefail", "-c"]

ENV DEBIAN_FRONTEND=noninteractive \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PYTHONPATH=/app/src \
    FL_LOG_LEVEL=INFO

WORKDIR /app

# Copy the source code
COPY verifixer_fault_localization/ /app/verifixer_fault_localization/
COPY repair/ /app/repair/
# and the custom dafny fork
COPY dafny/ /app/dafny/


# Build external dependencies
ARG Z3_VERSION=4.12.1
ARG FAULT_LOC_DAFNY_VERSION=v4.11.0
ARG Z3_REPO=https://github.com/Z3Prover/z3.git
ARG DAFNY_REPO=https://github.com/dafny-lang/dafny.git

# Base packages, installed once
RUN --mount=type=cache,target=/var/cache/apt \
    --mount=type=cache,target=/var/lib/apt/lists \
    apt-get update && apt-get install -y --no-install-recommends \
      ca-certificates curl wget sudo make build-essential unzip zip \
      python3 python3-pip python3-venv libicu-dev tzdata \
      git openssh-client openjdk-17-jdk ant rsync \
    && rm -rf /var/lib/apt/lists/*

# .NET SDK 8
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && \
    ln -sfn /usr/share/dotnet/dotnet /usr/local/bin/dotnet && \
    rm -f /tmp/dotnet-install.sh

# Python dependencies: copy only requirements first
COPY verifixer_fault_localization/src/requirements.txt /app/verifixer_fault_localization/src/requirements.txt
RUN python3 -m venv /opt/venv && \
    /opt/venv/bin/pip install --upgrade pip && \
    /opt/venv/bin/pip install -r /app/verifixer_fault_localization/src/requirements.txt && \
    /opt/venv/bin/pip install pytest pyright
ENV PATH="/opt/venv/bin:${PATH}"

# Z3 – pick the right binary for the build architecture
RUN git clone --depth 1 --branch "z3-${Z3_VERSION}" "${Z3_REPO}" /tmp/z3 && \
    cd /tmp/z3 && \
    python scripts/mk_make.py --dotnet && \
    cd build && \
    make -j"$(nproc)" && \
    make install && \
    cp -a /opt/venv/bin/z3 /usr/local/bin/ && \
    cp -a /opt/venv/lib/libz3.so /usr/local/lib/ && \
    cp -a /opt/venv/include/* /usr/local/include/ 2>/dev/null || true && \
    chmod 755 /usr/local/bin/z3 && \
    ldconfig

ENV LD_LIBRARY_PATH="/usr/local/lib:${LD_LIBRARY_PATH}"

# Make JAVA_HOME architecture‑independent
RUN JAVA_ARCH=$(dpkg --print-architecture) && \
    ln -s "/usr/lib/jvm/java-17-openjdk-${JAVA_ARCH}" /usr/lib/jvm/default-java
ENV JAVA_HOME=/usr/lib/jvm/default-java

# Build Dafny's version used for fault localization from the repo you point at
RUN git clone --depth 1 --branch "${FAULT_LOC_DAFNY_VERSION}" "${DAFNY_REPO}" /tmp/dafny && \
    make -C /tmp/dafny -j"$(nproc)" && \
    mv /tmp/dafny /app/verifixer_fault_localization && \
    ln -sf /app/verifixer_fault_localization/dafny/Binaries/Dafny /usr/local/bin/dafny

# Daikon
ENV DAIKONDIR="/opt/daikon"
RUN git clone --depth 1 --branch master https://github.com/codespecs/daikon.git /tmp/daikon-src && \
    cd /tmp/daikon-src && \
    make -j"$(nproc)" daikon.jar && \
    mkdir -p "${DAIKONDIR}" && \
    cp daikon.jar "${DAIKONDIR}/" && \
    cp -r scripts "${DAIKONDIR}/" && \
    rm -rf /tmp/daikon-src
ENV PATH="${DAIKONDIR}:${PATH}"


# Build our tools
# Keep pytest configuration available in-container so test discovery/path settings match local runs
COPY verifixer_fault_localization/pytest.ini /app/verifixer_fault_localization/pytest.ini
# Small marker file
COPY verifixer_fault_localization/.repo_verifixer_fault_localization_marker /app/verifixer_fault_localization

# Build test generation: DafnyTestGen
COPY verifixer_fault_localization/external/tests_gen/dafny-test-gen/ /app/verifixer_fault_localization/external/dafny-test-gen/
RUN cp /app/verifixer_fault_localization/external/dafny-test-gen/DafnyCBT/DafnyCBT.csproj.template /app/verifixer_fault_localization/external/dafny-test-gen/DafnyCBT/DafnyCBT.csproj
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore /app/verifixer_fault_localization/external/dafny-test-gen/DafnyCBT/DafnyCBT.csproj && \
    dotnet build /app/verifixer_fault_localization/external/dafny-test-gen/DafnyCBT/DafnyCBT.csproj \
      -c Release -o /app/build_output/DafnyTestGen --no-restore

# Build fault localization: strategies
COPY verifixer_fault_localization/strategies/ /app/verifixer_fault_localization/strategies/
RUN --mount=type=cache,target=/root/.nuget/packages \
    set -euo pipefail; \
    for csproj in /app/verifixer_fault_localization/strategies/*/*.csproj; do \
      dir="$(dirname "$csproj")"; \
      out="/app/verifixer_fault_localization/build_output/$(basename "$dir")"; \
      dotnet restore "$csproj"; \
      dotnet build "$csproj" -c Release -o "$out" --no-restore /p:DafnyDir=/app/verifixer_fault_localization/dafny/Binaries; \
    done

# Build fault localization: SNAP
COPY verifixer_fault_localization/external/tools/dafny-autofix/ /app/verifixer_fault_localization/external/tools/dafny-autofix/
RUN --mount=type=cache,target=/root/.nuget/packages \
    mkdir /app/verifixer_fault_localization/external/tools/dafny-autofix/autofix/lib && \
    cp -a /tmp/z3/build/Microsoft.Z3.dll /app/verifixer_fault_localization/external/tools/dafny-autofix/autofix/lib && \
    cp -a /tmp/z3/build/libz3.so /app/verifixer_fault_localization/external/tools/dafny-autofix/autofix/lib && \
    dotnet restore /app/verifixer_fault_localization/external/tools/dafny-autofix/autofix && \
    dotnet build /app/verifixer_fault_localization/external/tools/dafny-autofix/autofix -c Release -o /app/verifixer_fault_localization/build_output/Autofix --no-restore /p:DafnyDir=/app/verifixer_fault_localization/dafny/Binaries

# Build repair: custom dafny fork + z3 binary + plugin
RUN --mount=type=cache,target=/root/.nuget/packages \
    cd /app/dafny && \
    make exe -j"$(nproc)" && \
    cd /app/dafny/Binaries && \
    wget -q https://github.com/dafny-lang/solver-builds/releases/download/snapshot-2023-08-02/z3-4.12.1-x64-ubuntu-20.04-bin.zip && \
    unzip -q z3-4.12.1-x64-ubuntu-20.04-bin.zip && \
    mv z3-4.12.1 z3 && \
    chmod 755 z3 && \
    rm -f z3-4.12.1-x64-ubuntu-20.04-bin.zip && \
    cd /app/repair && \
    dotnet build repair.csproj && \
    chmod -R a+rwX /app/repair

CMD ["bash"]
