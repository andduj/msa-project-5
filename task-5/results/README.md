# Task 5 — Мониторинг, логирование и оповещения

Что сделано:
- Поднят стек: Prometheus, Grafana, OpenSearch, OpenSearch Dashboards, Fluent Bit, batch‑processing app.
- Метрики приложения: Micrometer + Prometheus `/actuator/prometheus`.
- Дашборд Grafana `batch-processing-dashboard` (CPU и базовые показатели процесса/ JVM).
- Алерты Prometheus (`prometheus/alerts.yml`):
  - AppDown — цель приложения недоступна (`up{job="batch-processing"} == 0`)
  - HighErrorRate — пример для HTTP 5xx (при необходимости активировать)
- Логи: JSON stdout от приложения → Fluent Bit tail → OpenSearch → просмотр в Dashboards (Discover, индекс `logstash-*`).

Скриншоты:
- 1.png — Prometheus targets (UP)
- 2.png — Grafana CPU панель
- 3.png — Метрики `/actuator/prometheus`
- 4.png — Логи приложения в OpenSearch Dashboards
- 5.png — Alerts (Firing)
- 6.png, 7.png — дополнительные (по шагам демонстрации)

Как воспроизвести:
```
cd task-5/initial
./gradlew build
docker compose up -d --build
# Открыть: 9090 Prometheus, 3000 Grafana, 5601 Dashboards, 8080 actuator
```

Для срабатывания алерта AppDown:
```
docker compose stop app
# Через ~1 минуту на /alerts будет Firing
```

