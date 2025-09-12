## Обоснование выбора технологического решения (в контексте task-1)

Текущее решение построено на C#/.NET 8 с использованием `Hangfire` (оркестрация фоновых задач), `PostgreSQL` (хранилище и Hangfire Storage), `Serilog` (логирование), `Polly` (ретраи на уровне I/O) и `MailKit` (email). Это видно по файлам:

- Оркестрация/инициализация: `BatchProcessing.Api/Program.cs` (настройка Hangfire PostgreSQL Storage, Serilog, Swagger)
- API и демо‑эндпоинты: `BatchProcessing.Api/Controllers/BatchProcessingController.cs` (ветвление, ретраи, fallback)
- Планирование и workflow: `BatchProcessing.Core/Jobs/BatchProcessingJob.cs` (Hangfire `AutomaticRetry`, `ContinueJobWith`)
- Бизнес‑логика и ретраи: `BatchProcessing.Core/Services/BatchProcessingService.cs` (ветвление heavy/light, `Polly` для CSV/БД)
- Уведомления: `BatchProcessing.Core/Services/EmailService.cs` (MailHog)
- Локальная инфраструктура: `docker-compose.yml` (Postgres, MailHog, pgAdmin)

Почему этот стек уместен:

- **Зрелая экосистема .NET**: официальные SDK/драйверы для BigQuery, Redshift, Kafka, а также интеграции со Spark.
- **Hangfire**: простой и надежный планировщик/оркестратор в рамках .NET‑приложения, поддерживает ретраи, продолжения, очереди, несколько воркеров/серверов.
- **Прозрачный контроль потока**: ветвления/условия кодируются на C#, что хорошо согласуется с доменной логикой.
- **Обсервабилити**: Serilog, централизованные логи; можно расширять до Prometheus/Grafana.

### Интеграции с BigQuery, Redshift, Kafka и Spark (в рамках текущего стека)

- **BigQuery (GCP)**
  - **Как интегрировать:** выполнять чтение/запись в шагах Hangfire‑jobs из `BatchProcessingService` с использованием официального клиента.
  - **NuGet‑пакеты:** `Google.Cloud.BigQuery.V2`
  - **Типовые операции:** запуск SQL‑job, загрузка из GCS, экспорт в GCS, чтение таблиц/партиций.
  - **Оркестрация:** шаги чтения/записи в BigQuery как отдельные Hangfire‑jobs; ожидание завершения через polling API/идемпотентные операции, сохранение статуса в БД.

- **Redshift (AWS)**
  - **Как интегрировать:**
    - через `Npgsql` (Redshift совместим с PostgreSQL) для SQL‑операций;
    - либо через **Redshift Data API** (`AWSSDK.RedshiftDataAPIService`) для без‑JDBC/долгих запросов.
  - **NuGet‑пакеты:** `Npgsql`, `AWSSDK.RedshiftDataAPIService`, при необходимости S3 `AWSSDK.S3` для `COPY/UNLOAD`.
  - **Оркестрация:** подготовка данных в S3 → `COPY` в Redshift; мониторинг выполнения через Data API; шаги оформляются как `BackgroundJob.Enqueue`/`ContinueJobWith`.

- **Kafka**
  - **Как интегрировать (event‑driven):** добавить `IHostedService`/BackgroundService‑консьюмер на `Confluent.Kafka`, который по событию публикует задачи в Hangfire (`BackgroundJob.Enqueue`).
  - **NuGet‑пакеты:** `Confluent.Kafka`
  - **Паттерны:**
    - Триггеры: консьюмер → валидация → enqueue job → мониторинг в Hangfire Dashboard;
    - Продьюсер: отправка событий о результатах обработки.

- **Spark**
  - **Вариант A (кластер Spark):** вызывать Spark‑jobs через Livy REST API или cloud‑орchestrators:
    - GCP Dataproc: `Google.Cloud.Dataproc.V1` (submit job, ждать состояние);
    - AWS EMR: `AWSSDK.EMR` (add steps, ждать завершение).
  - **Вариант B (.NET for Apache Spark):** использовать `Microsoft.Spark` для написания Spark‑задач на .NET и отправлять их на кластер.
  - **Оркестрация:** шаг отправки → сенсор/пулинг статуса → продолжение пайплайна; хранить jobId и статус в БД.

### Ветвления, условия и event‑triggers (реализация в коде task‑1)

- **Ветвления/условия:** реализованы в `BatchProcessingService.ProcessDataBatchAsync()` — выбор heavy/light обработки на основании объема данных.
- **Композиция шагов/продолжения:** `BatchProcessingJob.ScheduleRetryDemoWorkflow()` — `Enqueue` → `ContinueJobWith` (fallback) → `ContinueJobWith` (cleanup).
- **Event‑triggers:** доступны через внешние вызовы API (`BatchProcessingController`) или Kafka‑консьюмер (как расширение): событие → enqueue в Hangfire; также есть поддержка Recurring Jobs (по расписанию).

### Надёжность: fallback‑logic, retry и email (реализация в коде task‑1)

- **Retry:**
  - Уровень I/O: `Polly.WaitAndRetryAsync` в чтении CSV/БД (`BatchProcessingService`).
  - Уровень задач: атрибут `AutomaticRetry` в `BatchProcessingJob` с настраиваемыми задержками.
- **Fallback‑логика:** демонстрационный workflow в `BatchProcessingController.DemoWorkflowWithFallback()` и в `BatchProcessingJob.ExecuteFallbackTaskAsync()` с последующей очисткой.
- **Email‑уведомления:** `EmailService` (MailHog локально). Можно переключить на SES/SendGrid/SMTP в облаке. Хуки на ошибки — через фильтры Hangfire/глобальные обработчики и/или `onFailure`‑коллбеки.

### Развёртывание в облачной среде (для текущего стека)

- **Общая схема:** контейнеризованный .NET API + Hangfire Server (масштабируется количеством экземпляров) + хранилище Hangfire в управляемом PostgreSQL + объектное хранилище логов/метрик.

- **GCP‑варианты:**
  - Compute: Cloud Run (simple), GKE (k8s), GCE (VMs).
  - DB: Cloud SQL for PostgreSQL (Hangfire Storage и бизнес‑данные).
  - Логи/метрики: Serilog sink в Cloud Logging; Prometheus Operator на GKE + Grafana; Uptime checks, Error Reporting.
  - Интеграции: BigQuery (`Google.Cloud.BigQuery.V2`), Dataproc (`Google.Cloud.Dataproc.V1`). Секреты — Secret Manager.

- **AWS‑варианты:**
  - Compute: ECS/Fargate или EKS; альтернативно EC2 Auto Scaling Group.
  - DB: Amazon RDS for PostgreSQL.
  - Логи/метрики: Serilog sink в CloudWatch; Prometheus/Grafana на EKS; AWS X-Ray (опционально).
  - Интеграции: Redshift (через `Npgsql`/Data API), EMR (`AWSSDK.EMR`), S3 (`AWSSDK.S3`). Секреты — Secrets Manager/Parameter Store.

- **Архитектурные детали деплоя:**
  - Несколько экземпляров приложения = несколько Hangfire Servers; очереди: `default`, `critical`, `batch-processing`.
  - Миграции БД и инициализация: запуск при старте контейнера; readiness/liveness probes.
  - Секреты/конфигурация: переменные окружения + Secret Manager/Secrets Manager; connection strings не хранить в образе.
  - CI/CD: сборка Docker образа, прогон тестов, деплой через Helm/ArgoCD/GitHub Actions.
  - Безопасность: ограничение доступа к Hangfire Dashboard (реверс‑прокси + аутентификация), сетевые политики, IAM‑ролей‑на‑под.

### Короткое резюме

- **Решение:** .NET 8 + Hangfire + PostgreSQL + Serilog/Polly/MailKit — согласовано с текущим кодом.
- **Интеграции:** BigQuery/Redshift/Kafka/Spark — через официальные .NET SDK/драйверы; задачи оркестрируются Hangfire.
- **Логика:** ветвления и условия реализуются в коде; event‑triggers через API/Kafka консьюмер; recurring по расписанию.
- **Надёжность:** из коробки ретраи (Polly/Hangfire), fallback‑ветки, email‑уведомления.
- **Облако:** GCP/AWS на Kubernetes/ECS/Cloud Run; управляемые Postgres; централизованные логи/метрики/секреты.


