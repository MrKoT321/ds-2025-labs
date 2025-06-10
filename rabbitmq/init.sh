#!/bin/bash

rabbitmq-server -detached

echo "Waiting for RabbitMQ to start..."
until rabbitmqctl status &> /dev/null; do
  sleep 2
done

rabbitmqctl delete_user guest || true

rabbitmqctl add_user "$RABBITMQ_USERNAME" "$RABBITMQ_PASSWORD"
rabbitmqctl set_user_tags "$RABBITMQ_USERNAME" administrator
rabbitmqctl set_permissions -p / "$RABBITMQ_USERNAME" ".*" ".*" ".*"

rabbitmqctl stop

exec rabbitmq-server