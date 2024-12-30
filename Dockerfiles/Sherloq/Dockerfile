# Basis-Image
FROM python:3.11-slim

# Arbeitsverzeichnis im Container
WORKDIR /app

# Benötigte Pakete installieren
RUN apt-get update && apt-get install -y \
    git \
    && rm -rf /var/lib/apt/lists/*

# Repository klonen
RUN git clone https://github.com/GuidoBartoli/sherloq.git /app/sherloq

# Arbeitsverzeichnis ändern
WORKDIR /app/sherloq/gui

# Abhängigkeiten installieren
RUN pip install --no-cache-dir -r requirements.txt \
    && pip install --no-cache-dir -r requirements_ai_solutions.txt

# Startbefehl definieren
CMD ["python", "sherloq.py"]
