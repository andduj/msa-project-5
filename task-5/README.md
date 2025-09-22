**Подготовка окружения**

1. Docker Desktop с Docker Compose
2. JDK 17 (для локальной сборки Gradle, не обязательно)

**Сборка приложения**

```
cd task-5/initial
./gradlew build
```

**Запуск стека мониторинга/логирования**

```
cd task-5/initial
docker compose up -d --build
```

Авто-инициализация БД выполняется из `db-init/01_schema.sql` и `02_seed.sql`.

Сервисы и порты:
- batch-processing: 8080 (метрики `GET /actuator/prometheus`)
- Prometheus: 9090 (таргет scrape, алерты)
- Grafana: 3000 (готовый дашборд `batch-processing-dashboard`)
- OpenSearch: 9200
- OpenSearch Dashboards: 5601 (централизованные логи)
- Fluent Bit: tail Docker-логов → OpenSearch

Prometheus читает метрики через Micrometer Prometheus Registry, алерты описаны в `prometheus/alerts.yml` (например, `AppDown`).

OpenSearch Dashboards: создайте индекс‑паттерн `logstash-*` и смотрите логи контейнеров; фильтр по приложению: `container.name: "initial-app-1"`.

Полезные команды:
```
# Перезапустить только приложение
docker compose rm -f app && docker compose up -d app

# Смоделировать AppDown (алерт Firing)
docker compose stop app
```

Скриншоты примеров находятся в `task-5/results/screeenshots/`.
