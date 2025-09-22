### Задание 3 — Distributed Scheduling с k8s CronJob (C#)

Содержимое:
- `exporter/` — .NET 8 консольное приложение для экспорта таблицы PostgreSQL в CSV через `COPY ... TO STDOUT`.
- `k8s/postgres.yaml` — демонстрационный PostgreSQL (Namespace, Secret, PVC, ConfigMap с init SQL, Deployment, Service).
- `k8s/export-cronjob.yaml` — `CronJob` (20:00 ежедневно), `Secret`/`ConfigMap` для параметров и `PVC` для выгрузок.

#### Как запустить в Minikube (локально)
1) Запустить Minikube:
   - `minikube start`
2) Собрать контейнер экспортёра внутри Minikube:
   - `minikube image build -t exporter:latest exporter`
3) Развернуть PostgreSQL и CronJob:
   - `kubectl apply -f k8s/postgres.yaml`
   - Подождать Ready у пода `postgres` в ns `analytics`.
   - `kubectl apply -f k8s/export-cronjob.yaml`
4) Протестировать немедленный запуск (не ждать 20:00):
   - `kubectl -n analytics create job --from=cronjob/shipments-export shipments-export-manual`
   - `kubectl -n analytics logs job/shipments-export-manual -f`
5) Проверить результат:
   - Найти под CronJob, смонтированный PVC `export-pvc` содержит файл `/exports/shipments.csv`.
   - Для проверки можно стартовать временный под:
     - `kubectl -n analytics run debug --rm -it --image=busybox -- /bin/sh`
     - внутри: `ls -l /exports` (предварительно смонтировать PVC при запуске или `kubectl cp` из пода экспортёра).

Конфигурация переменных окружения задаётся через `Secret`/`ConfigMap`. При необходимости смените `TABLE`, `OUTPUT_PATH` и параметры подключения в файлах манифестов.


