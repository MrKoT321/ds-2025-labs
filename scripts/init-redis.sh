#!/bin/bash

# Ожидаем доступности Redis
until redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" ping &>/dev/null; do
  echo "Waiting for Redis at $REDIS_HOST:$REDIS_PORT..."
  sleep 1
done

# Инициализация данных в Redis (пример)
redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" SET "app:status" "ready"
echo "Redis initialized successfully!"